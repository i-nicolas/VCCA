# Service Architecture

## Full Request Pipeline
```
Blazor Component (inherits BaseComponent)
  → @inject IXxxHandler (Handlers/Repositories/ — per-feature interface)
    → XxxHandler (Handlers/Implementations/ — calls IMediator.Send())
      → Application.UseCases Handler
        → IXxxService (internal Application-layer contracts)
          → IAppCommandRepository / IAppReadRepository (Application.UseCases/Repositories/Bases/)
            → AppCommandRepository / AppReadRepository (Database.MsSql implementation)
              → AppDbContext → MSSQL
```

## What Belongs Where

| Concern | Location |
|---|---|
| UI markup and component logic | `Web.BlazorServer/Components/Pages/` |
| Per-feature handler interfaces (injected into components) | `Web.BlazorServer/Handlers/Repositories/` |
| Per-feature handler implementations (`IMediator.Send()` calls) | `Web.BlazorServer/Handlers/Implementations/` |
| MediatR commands, queries, handlers | `Application.UseCases/Commands/` or `Queries/` |
| Internal Application service contracts | `Application.UseCases/Services/` |
| DTOs | `Application.DataTransferObjects/` |
| Mapster profiles | `Application.UseCases/` |
| Domain entities, aggregates, events | `Domain.Entities/` |
| EF Core configurations | `Database.MsSql/` |
| Migrations | `Database.MsSql/` |
| Generic repository interfaces (`IAppCommandRepository`, `IAppReadRepository`) | `Application.UseCases/Repositories/Bases/` |
| Entity-specific repository interfaces (`IXxxReadRepository`) | `Application.UseCases/Repositories/Domain/` |
| Repository implementations | `Database.MsSql/` |
| SAP HTTP calls | `Integration.SAP/` |
| Helpers and extensions | `Shared.Libraries/` |
