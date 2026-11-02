namespace MenuMetrics.Web.Services;
public interface IEtlService
{
    Task<EtlPreview> PreviewAsync(Stream csv, string fileName);
    Task<int> ImportAsync(Stream csv, int businessId, int locationId, Dictionary<string,string> mapping);
}
public record EtlPreview(string DetectedSource, IReadOnlyList<string> Headers, IReadOnlyList<Dictionary<string,string>> SampleRows, Dictionary<string,string> SuggestedMapping);
