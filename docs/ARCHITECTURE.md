# MenuMetrics — Architecture

See repository README for run instructions. High-level:

- Web tier: ASP.NET Core MVC 8, Razor views, ASP.NET Identity.
- Data tier: EF Core 8. SQLite by default for zero-config local dev; switch to SQL Server by setting `Database:Provider=SqlServer` in appsettings.
- ML tier: optional Python FastAPI microservice (src/MenuMetrics.ML) for forecasting. The Web tier falls back to an in-process seasonal-naive model if unavailable.
- Modules: ETL (CSV mapper), Profitability (BCG matrix), Demand (heatmap), Staffing (rule engine), Promotions (pre/during/post + ROI), Forecasting (explainable decomposition), Recommendations (rule + heuristic engine), Inventory.

## Roadmap
1. MVP — ETL + Dashboard + Profitability + Demand.
2. Analytics — Promotions ROI + Inventory.
3. Forecasting — wire FastAPI Prophet/ARIMA.
4. AI Recommendations — expand rule + ML hybrid.
5. Enterprise — multi-tenant, RBAC, audit, white-label.
