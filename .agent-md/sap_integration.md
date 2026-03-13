# SAP Integration

## Location
`Integration.SAP/`

## Pattern
- Outbound HTTP client only — wraps SAP API calls; never called by Domain or Application directly
- SQL scripts for SAP data queries live in `Integration.SAP/SQLScripts/`
- All SAP HTTP logic stays in `Integration.SAP` — never call it from Blazor components or Application handlers directly

## Adding a New SAP Endpoint
1. Add the HTTP method in `Integration.SAP`
2. Add a MediatR command/query + handler in `Application.UseCases`
3. Expose via `IXxxIntegration` interface — components go through `IXxxHandler` → MediatR as normal
