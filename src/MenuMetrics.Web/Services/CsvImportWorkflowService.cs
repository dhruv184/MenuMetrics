using System.Data;
using System.Globalization;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using MenuMetrics.Web.Data;
using MenuMetrics.Web.Models.Entities;
using MenuMetrics.Web.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace MenuMetrics.Web.Services;

public class CsvImportWorkflowService : ICsvImportWorkflowService
{
    private const int MaxCsvRows = 100_000;
    private const int MaxSavedIssues = 50;
    private readonly ApplicationDbContext _db;
    private readonly string _stagingDirectory;

    public CsvImportWorkflowService(ApplicationDbContext db, IWebHostEnvironment environment)
    {
        _db = db;
        _stagingDirectory = Path.Combine(environment.ContentRootPath, "App_Data", "ImportStaging");
    }

    public async Task<ImportMappingVm> CreateDraftAsync(
        Stream csv, string originalFileName, int businessId, int locationId, string userId)
    {
        var location = await _db.Locations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == locationId && x.BusinessId == businessId);
        if (location == null) throw new InvalidOperationException("Choose a location belonging to your business.");

        Directory.CreateDirectory(_stagingDirectory);
        var storedPath = Path.Combine(_stagingDirectory, $"{Guid.NewGuid():N}.csv");
        try
        {
            await using (var destination = new FileStream(storedPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                await csv.CopyToAsync(destination);

            var table = await ReadCsvAsync(storedPath);
            if (table.Headers.Count == 0) throw new InvalidDataException("The CSV is empty or has no header row.");
            if (table.Headers.Distinct(StringComparer.OrdinalIgnoreCase).Count() != table.Headers.Count)
                throw new InvalidDataException("Column names must be unique. Rename duplicate CSV headers and upload again.");

            var run = new ImportRun
            {
                BusinessId = businessId,
                LocationId = locationId,
                UserId = userId,
                OriginalFileName = Path.GetFileName(originalFileName),
                StoredFilePath = storedPath,
                DetectedSource = DetectSource(table.Headers),
                Status = "AwaitingMapping"
            };
            _db.ImportRuns.Add(run);
            await _db.SaveChangesAsync();
            return ToMappingVm(run, table);
        }
        catch
        {
            if (File.Exists(storedPath)) File.Delete(storedPath);
            throw;
        }
    }

    public async Task<ImportMappingVm> GetMappingAsync(int runId, int businessId, string userId)
    {
        var run = await FindRunAsync(runId, businessId, userId);
        EnsureStagedFile(run);
        var table = await ReadCsvAsync(run.StoredFilePath);
        return ToMappingVm(run, table);
    }

    public async Task<ImportConfirmationVm> MapAndValidateAsync(
        int runId, int businessId, string userId, List<ColumnMappingInput> mappings)
    {
        var run = await FindRunAsync(runId, businessId, userId);
        if (run.Status == "Completed") throw new InvalidOperationException("This import has already been completed.");
        EnsureStagedFile(run);

        var table = await ReadCsvAsync(run.StoredFilePath);
        ValidateMapping(table.Headers, mappings);
        run.MappingJson = JsonSerializer.Serialize(mappings);

        var scan = await ScanAsync(table, mappings, businessId);
        ApplyScan(run, scan);
        run.Status = scan.ValidRows.Count > 0 ? "AwaitingConfirmation" : "ValidationFailed";
        await _db.SaveChangesAsync();
        return await ToConfirmationVmAsync(run, scan);
    }

    public async Task<ImportConfirmationVm> GetConfirmationAsync(int runId, int businessId, string userId)
    {
        var run = await FindRunAsync(runId, businessId, userId);
        if (run.Status is not ("AwaitingConfirmation" or "ValidationFailed"))
            throw new InvalidOperationException("This import is not ready for confirmation. Map and validate it first.");
        EnsureStagedFile(run);
        var mappings = DeserializeMappings(run.MappingJson);
        var table = await ReadCsvAsync(run.StoredFilePath);
        var scan = await ScanAsync(table, mappings, businessId);
        ApplyScan(run, scan);
        run.Status = scan.ValidRows.Count > 0 ? "AwaitingConfirmation" : "ValidationFailed";
        await _db.SaveChangesAsync();
        return await ToConfirmationVmAsync(run, scan);
    }

    public async Task<ImportSummaryVm> CommitAsync(int runId, int businessId, string userId)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var run = await FindRunAsync(runId, businessId, userId);
        if (run.Status != "AwaitingConfirmation")
            throw new InvalidOperationException("This import is not awaiting confirmation or has already been imported.");
        EnsureStagedFile(run);

        var mappings = DeserializeMappings(run.MappingJson);
        var table = await ReadCsvAsync(run.StoredFilePath);
        ValidateMapping(table.Headers, mappings);
        var scan = await ScanAsync(table, mappings, businessId);
        ApplyScan(run, scan);
        if (scan.ValidRows.Count == 0)
        {
            run.Status = "ValidationFailed";
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            throw new InvalidOperationException("No valid rows remain. Review the validation errors and correct the CSV.");
        }

        // Only valid rows are imported. Invalid rows remain counted in the ImportRun audit record.
        var entities = scan.ValidRows.Select(row => new Transaction
        {
            BusinessId = businessId,
            LocationId = run.LocationId,
            MenuItemId = row.MenuItemId,
            OccurredAt = row.OccurredAt,
            Quantity = row.Quantity,
            LineTotal = row.LineTotal,
            Source = "csv-import",
            ImportRunId = run.Id
        }).ToList();

        _db.Transactions.AddRange(entities);
        run.Status = "Completed";
        run.ImportedRows = entities.Count;
        run.CompletedAtUtc = DateTime.UtcNow;
        var stagedPath = run.StoredFilePath;
        run.StoredFilePath = "";
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        // The imported source file is not retained after successful commit.
        TryDelete(stagedPath);
        return await ToSummaryVmAsync(run);
    }

    public async Task<ImportSummaryVm?> GetSummaryAsync(int runId, int businessId, string userId)
    {
        var run = await _db.ImportRuns.AsNoTracking()
            .Include(x => x.Location)
            .SingleOrDefaultAsync(x => x.Id == runId && x.BusinessId == businessId && x.UserId == userId);
        return run == null ? null : ToSummaryVm(run);
    }

    public async Task<List<ImportSummaryVm>> GetHistoryAsync(int businessId, string userId)
    {
        var runs = await _db.ImportRuns.AsNoTracking()
            .Include(x => x.Location)
            .Where(x => x.BusinessId == businessId && x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(100)
            .ToListAsync();
        return runs.Select(ToSummaryVm).ToList();
    }

    public async Task<ClearSalesDataResult> ClearBusinessSalesAsync(int businessId)
    {
        // Keep database deletion atomic. Transactions are deleted before import runs,
        // so this does not depend on the Transaction.ImportRunId delete behavior.
        await using var dbTransaction =
            await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        // Collect staged paths before deleting the import-run records.
        var storedPaths = await _db.ImportRuns
            .AsNoTracking()
            .Where(run => run.BusinessId == businessId && run.StoredFilePath != "")
            .Select(run => run.StoredFilePath)
            .ToListAsync();

        var transactionsDeleted = await _db.Transactions
            .Where(sale => sale.BusinessId == businessId)
            .ExecuteDeleteAsync();

        var importRunsDeleted = await _db.ImportRuns
            .Where(run => run.BusinessId == businessId)
            .ExecuteDeleteAsync();

        await dbTransaction.CommitAsync();

        // Delete only CSV files inside this service's staging directory.
        var pathComparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
        var safePaths = new HashSet<string>(pathComparer);
        var stagedFilesNotRemoved = 0;

        foreach (var storedPath in storedPaths)
        {
            var safePath = GetSafeStagedPath(storedPath);
            if (safePath == null)
            {
                stagedFilesNotRemoved++;
                continue;
            }

            safePaths.Add(safePath);
        }

        var stagedFilesRemoved = 0;
        foreach (var path in safePaths)
        {
            if (!File.Exists(path))
                continue;

            try
            {
                File.Delete(path);
                stagedFilesRemoved++;
            }
            catch (IOException)
            {
                stagedFilesNotRemoved++;
            }
            catch (UnauthorizedAccessException)
            {
                stagedFilesNotRemoved++;
            }
        }

        return new ClearSalesDataResult(
            transactionsDeleted,
            importRunsDeleted,
            stagedFilesRemoved,
            stagedFilesNotRemoved);
    }

    private string? GetSafeStagedPath(string storedPath)
    {
        try
        {
            var root = Path.GetFullPath(_stagingDirectory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            var candidate = Path.GetFullPath(storedPath);
            var comparison = OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            if (!candidate.StartsWith(root, comparison)) return null;
            if (!string.Equals(Path.GetExtension(candidate), ".csv", StringComparison.OrdinalIgnoreCase))
                return null;

            return candidate;
        }
        catch (ArgumentException) { return null; }
        catch (NotSupportedException) { return null; }
        catch (PathTooLongException) { return null; }
    }

    private async Task<ImportRun> FindRunAsync(int runId, int businessId, string userId)
    {
        var run = await _db.ImportRuns
            .Include(x => x.Location)
            .SingleOrDefaultAsync(x => x.Id == runId && x.BusinessId == businessId && x.UserId == userId);
        return run ?? throw new KeyNotFoundException("Import run not found.");
    }

    private static void EnsureStagedFile(ImportRun run)
    {
        if (string.IsNullOrWhiteSpace(run.StoredFilePath) || !File.Exists(run.StoredFilePath))
            throw new InvalidOperationException("The staged CSV has expired. Upload the file again.");
    }

    private async Task<CsvTable> ReadCsvAsync(string path)
    {
        var headers = new List<string>();
        var rows = new List<CsvDataRow>();
        using var reader = new StreamReader(path, detectEncodingFromByteOrderMarks: true);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
            MissingFieldFound = null,
            HeaderValidated = null
        };
        using var parser = new CsvParser(reader, config);
        if (!await parser.ReadAsync()) return new CsvTable(headers, rows);

        headers = (parser.Record ?? Array.Empty<string>())
            .Select((h, i) => string.IsNullOrWhiteSpace(h) ? $"Column {i + 1}" : h.Trim())
            .ToList();

        var rowNumber = 1;
        while (await parser.ReadAsync())
        {
            rowNumber++;
            if (rows.Count >= MaxCsvRows)
                throw new InvalidDataException($"The CSV exceeds the {MaxCsvRows:N0}-row limit.");
            rows.Add(new CsvDataRow(rowNumber, parser.Record ?? Array.Empty<string>()));
        }
        return new CsvTable(headers, rows);
    }

    private async Task<ScanResult> ScanAsync(CsvTable table, List<ColumnMappingInput> mappings, int businessId)
    {
        var menuItems = await _db.MenuItems.AsNoTracking()
            .Where(x => x.BusinessId == businessId)
            .ToListAsync();
        var menuByName = menuItems
            .GroupBy(x => x.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var columnIndexes = mappings
            .Where(x => !string.Equals(x.TargetField, "Ignore", StringComparison.Ordinal))
            .ToDictionary(
                x => x.TargetField,
                x => table.Headers.FindIndex(h =>
                    string.Equals(h, x.SourceColumn, StringComparison.Ordinal)),
                StringComparer.Ordinal);

        var valid = new List<ValidatedSale>();
        var issues = new List<string>();
        var total = 0;

        foreach (var row in table.Rows)
        {
            total++;
            var rowErrors = new List<string>();
            string Cell(string target)
            {
                var index = columnIndexes[target];
                return index >= 0 && index < row.Cells.Length ? row.Cells[index].Trim() : "";
            }

            var dateText = Cell("Date");
            var timeText = columnIndexes.TryGetValue("Time", out var timeIndex) && timeIndex >= 0 && timeIndex < row.Cells.Length
                ? row.Cells[timeIndex].Trim() : "";
            var dateValue = string.IsNullOrWhiteSpace(timeText) ? dateText : $"{dateText} {timeText}";
            if (!TryParseDate(dateValue, out var occurredAt)) rowErrors.Add("invalid or missing date/time");

            var menuName = Cell("MenuItem");
            if (string.IsNullOrWhiteSpace(menuName)) rowErrors.Add("missing menu item");
            else if (!menuByName.TryGetValue(menuName, out _)) rowErrors.Add($"menu item '{menuName}' does not exist in this business");

            if (!int.TryParse(Cell("Quantity"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var quantity) || quantity <= 0)
                rowErrors.Add("quantity must be a positive whole number");

            if (!TryParseMoney(Cell("LineTotal"), out var lineTotal) || lineTotal < 0)
                rowErrors.Add("line total must be a valid non-negative amount");

            if (rowErrors.Count == 0)
            {
                var item = menuByName[menuName];
                valid.Add(new ValidatedSale(occurredAt, item.Id, quantity, lineTotal));
            }
            else if (issues.Count < MaxSavedIssues)
            {
                issues.Add($"CSV row {row.RowNumber}: {string.Join("; ", rowErrors)}.");
            }
        }
        return new ScanResult(total, valid, issues);
    }

    private static void ValidateMapping(List<string> headers, List<ColumnMappingInput> mappings)
    {
        if (mappings == null || mappings.Count == 0) throw new InvalidOperationException("No column mapping was submitted.");
        if (mappings.Any(m => !headers.Contains(m.SourceColumn, StringComparer.Ordinal)))
            throw new InvalidOperationException("The mapping contains a column that is not in the uploaded CSV.");
        if (mappings.Any(m => !ImportTargetFields.All.Contains(m.TargetField, StringComparer.Ordinal)))
            throw new InvalidOperationException("The mapping contains an unsupported target field.");

        var targets = mappings.Where(m => m.TargetField != "Ignore").Select(m => m.TargetField).ToList();
        if (targets.Count != targets.Distinct(StringComparer.Ordinal).Count())
            throw new InvalidOperationException("Map each target field to at most one source column.");
        foreach (var required in new[] { "Date", "MenuItem", "Quantity", "LineTotal" })
            if (!targets.Contains(required, StringComparer.Ordinal))
                throw new InvalidOperationException($"Map a CSV column to {required}.");
    }

    private static bool TryParseDate(string value, out DateTime result) =>
        DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out result) ||
        DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out result);

    private static bool TryParseMoney(string value, out decimal result) =>
        decimal.TryParse(value, NumberStyles.Currency, CultureInfo.InvariantCulture, out result) ||
        decimal.TryParse(value, NumberStyles.Currency, CultureInfo.CurrentCulture, out result);

    private static void ApplyScan(ImportRun run, ScanResult scan)
    {
        run.TotalRows = scan.TotalRows;
        run.ValidRows = scan.ValidRows.Count;
        run.RejectedRows = scan.TotalRows - scan.ValidRows.Count;
        run.ValidationIssuesJson = JsonSerializer.Serialize(scan.Issues);
    }

    private async Task<ImportConfirmationVm> ToConfirmationVmAsync(ImportRun run, ScanResult scan)
    {
        var locationName = run.Location?.Name ?? await _db.Locations
            .Where(x => x.Id == run.LocationId && x.BusinessId == run.BusinessId)
            .Select(x => x.Name).SingleAsync();
        return new ImportConfirmationVm
        {
            RunId = run.Id,
            OriginalFileName = run.OriginalFileName,
            LocationName = locationName,
            DetectedSource = run.DetectedSource,
            TotalRows = scan.TotalRows,
            ValidRows = scan.ValidRows.Count,
            RejectedRows = scan.TotalRows - scan.ValidRows.Count,
            Issues = scan.Issues
        };
    }

    private static ImportMappingVm ToMappingVm(ImportRun run, CsvTable table)
    {
        var mappings = table.Headers.Select(header => new ColumnMappingInput
        {
            SourceColumn = header,
            TargetField = SuggestTarget(header)
        }).ToList();
        return new ImportMappingVm
        {
            RunId = run.Id,
            OriginalFileName = run.OriginalFileName,
            DetectedSource = run.DetectedSource,
            Headers = table.Headers,
            SampleRows = table.Rows.Take(5).Select(r => r.Cells.ToList()).ToList(),
            Mappings = mappings
        };
    }

    private static string SuggestTarget(string header)
    {
        var h = header.ToLowerInvariant();
        if (h.Contains("date")) return "Date";
        if (h == "time" || h.Contains("time ")) return "Time";
        if (h.Contains("menu") || h.Contains("item name") || h == "item") return "MenuItem";
        if (h.Contains("quantity") || h == "qty" || h.Contains("item qty")) return "Quantity";
        if (h.Contains("total") || h.Contains("gross") || h.Contains("amount")) return "LineTotal";
        return "Ignore";
    }

    private static string DetectSource(List<string> headers)
    {
        var joined = string.Join("|", headers).ToLowerInvariant();
        if (joined.Contains("transaction id") && joined.Contains("gross sales")) return "Square (suggested)";
        if (joined.Contains("order id") && joined.Contains("item gross price")) return "Toast (suggested)";
        if (joined.Contains("payment id") && joined.Contains("created time")) return "Clover (suggested)";
        return "Manual CSV";
    }

    private static List<ColumnMappingInput> DeserializeMappings(string json) =>
        JsonSerializer.Deserialize<List<ColumnMappingInput>>(json) ?? new List<ColumnMappingInput>();

    private async Task<ImportSummaryVm> ToSummaryVmAsync(ImportRun run)
    {
        var location = run.Location ?? await _db.Locations.AsNoTracking()
            .SingleAsync(x => x.Id == run.LocationId && x.BusinessId == run.BusinessId);
        return ToSummaryVm(run, location.Name);
    }

    private static ImportSummaryVm ToSummaryVm(ImportRun run) => ToSummaryVm(run, run.Location?.Name ?? "");

    private static ImportSummaryVm ToSummaryVm(ImportRun run, string locationName)
    {
        List<string> issues;
        try { issues = JsonSerializer.Deserialize<List<string>>(run.ValidationIssuesJson) ?? new(); }
        catch (JsonException) { issues = new(); }
        return new ImportSummaryVm
        {
            Id = run.Id,
            OriginalFileName = run.OriginalFileName,
            LocationName = locationName,
            DetectedSource = run.DetectedSource,
            Status = run.Status,
            TotalRows = run.TotalRows,
            ValidRows = run.ValidRows,
            RejectedRows = run.RejectedRows,
            ImportedRows = run.ImportedRows,
            CreatedAtUtc = run.CreatedAtUtc,
            CompletedAtUtc = run.CompletedAtUtc,
            Issues = issues
        };
    }

    private static void TryDelete(string path)
    {
        try { if (!string.IsNullOrWhiteSpace(path) && File.Exists(path)) File.Delete(path); }
        catch (IOException) { /* A cleanup job can remove abandoned staging files later. */ }
        catch (UnauthorizedAccessException) { /* Do not undo a committed import over file cleanup. */ }
    }

    private sealed record CsvTable(List<string> Headers, List<CsvDataRow> Rows);
    private sealed record CsvDataRow(int RowNumber, string[] Cells);
    private sealed record ValidatedSale(DateTime OccurredAt, int MenuItemId, int Quantity, decimal LineTotal);
    private sealed record ScanResult(int TotalRows, List<ValidatedSale> ValidRows, List<string> Issues);
}