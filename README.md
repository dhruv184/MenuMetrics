# MenuMetrics

ASP.NET Core MVC 8 analytics platform for restaurants — full implementation.

## Run locally

Requirements: .NET 8 SDK.

```bash
cd src/MenuMetrics.Web
dotnet restore
dotnet run
```

Open https://localhost:5001 (or the URL printed in the console).

Default database is SQLite (`menumetrics.db` is created automatically). The app
seeds a demo business (Bella's Pizza), 60 days of transactions, menu items,
promotions, inventory, and a demo user:

- email: `demo@menumetrics.app`
- password: `Demo!12345`

## Switching to SQL Server

Edit `src/MenuMetrics.Web/appsettings.json`:

```json
"Database": { "Provider": "SqlServer" },
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MenuMetrics;Trusted_Connection=True;TrustServerCertificate=true"
}
```

Then `dotnet ef migrations add Init && dotnet ef database update` (optional —
`EnsureCreated()` runs at startup).

## Optional ML microservice

```bash
cd src/MenuMetrics.ML
pip install -r requirements.txt
uvicorn main:app --port 8001
```

If the service is up, the Forecast page uses it; otherwise the .NET app falls
back to a built-in seasonal-naive model.

## Project layout

```
src/MenuMetrics.Web/    ASP.NET Core MVC 8 app (Controllers, Views, Services, EF Core)
src/MenuMetrics.ML/     Optional Python FastAPI forecasting service
db/schema.sql           Reference SQL Server schema
docs/ARCHITECTURE.md    Architecture & roadmap
```

## Modules

- **ETL / Upload** — CSV column mapper with Square/Toast/Clover auto-detection.
- **Dashboard** — Revenue, orders, avg ticket (last 30 days).
- **Profitability** — BCG matrix (Stars / Cash Cows / Question Marks / Dogs).
- **Demand** — Hour × day heatmap (last 4 weeks).
- **Staffing** — Configurable orders → headcount rules; weekly plan grid.
- **Inventory** — Stock vs. reorder point.
- **Promotions** — Pre/during/post revenue + ROI + rating; recompute button.
- **Forecast** — 14-day forecast with base + seasonality + trend decomposition.
- **Recommendations** — Rule-based engine across menu/staffing/inventory/promo.

## API

- `GET /api/metrics/heatmap` — demand heatmap JSON.
- `GET /api/metrics/forecast?days=14` — forecast JSON.
