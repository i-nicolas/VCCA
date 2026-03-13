# Application Layer

## Projects
- `Application.UseCases/` — MediatR handlers, pipeline behaviors, repository interfaces, Mapster registration
- `Application.DataTransferObjects/` — DTOs only


## Application.UseCases — Folder Structure

```
Application.UseCases/
├── Behaviors/
│   └── TransactionalDocumentBehavior.cs
├── Commands/
│   └── [Feature]/
│       └── [Action]Cmd.cs              # record (ITransactionalRequest or IRequest) + Handler class
├── Queries/
│   └── [Feature]/
│       └── [Action]Qry.cs              # record (IRequest) + Handler class
├── Notifications/
│   └── [Feature]/
│       └── [Action]Notification.cs     # record (INotification) + Handler class — co-located in one file
├── Registers/
│   └── MappingRegistration.cs          # Mapster IRegister — single global mapping profile
├── Repositories/
│   ├── Bases/
│   │   ├── IAppCommandRepository.cs    # Generic write interface (T = DEM entity)
│   │   ├── IAppReadRepository.cs       # Generic read interface (T = DEM entity)
│   │   └── ITransactionalRequest.cs    # Marker interface — commands that need DB transaction
│   ├── Domain/                         # Entity-specific read repository interfaces
│   │   └── [Feature]/
│   │       └── IXxxReadRepository.cs
│   └── Integration/                    # SAP and external service interfaces
│       └── IXxxIntegration.cs
└── AppUseCasesDI.cs                    # DI registration — called from Program.cs
```


## Commands & Queries

Commands and queries follow a co-located file convention — the request record and its handler class live in the same file.

```
Commands/[Feature]/[Action]Cmd.cs
Queries/[Feature]/[Action]Qry.cs
```

- Commands that mutate transactional documents **must** implement `ITransactionalRequest` to participate in the transaction pipeline
- Commands that do not require a transaction implement `IRequest` directly
- Queries always implement `IRequest` — never `ITransactionalRequest`


## Pipeline Behaviors

### `TransactionalDocumentBehavior`
A MediatR pipeline behavior that intercepts all requests implementing `ITransactionalRequest`.

**What it does:**
1. Begins a DB transaction via `IAppCommandRepository`
2. Executes the handler
3. Saves changes and commits on success
4. Rolls back on failure

### `ITransactionalRequest`
Marker interface inheriting `IRequest`. Located in `Repositories/Bases/`.
Any command that needs to run inside a transaction must implement this interface.

> Use `IPublisher` (not `ISender`) inside transactional handlers when publishing domain notifications — this keeps all side-effects within the same transaction scope.


## Notifications

MediatR notifications (`INotification`) live under `Notifications/[Feature]/`.
The notification record and its handler class are **co-located in the same file**:

```csharp
// [Action]Notification.cs
public record OrderCreatedNotification(...) : INotification;

public class OrderCreatedNotificationHandler : INotificationHandler<OrderCreatedNotification>
{
    // ...
}
```

Notifications are published via `IPublisher` — never via `ISender`.


## Repository Interfaces

Two generic repository interfaces are defined here. Both accept a DEM entity as the generic type parameter.

### `IAppCommandRepository<T>`
Write-side: `AddAsync`, `SaveChangesAsync`, `CommitAsync`, `BeginTransactionAsync`, rollback.

### `IAppReadRepository<T>`
Read-side: `FirstOrDefaultAsync`, `ListAsync`, `ExistsAsync`, and other EF Core reads.

Entity-specific `IXxxReadRepository` interfaces coexist for complex queries. Prefer these over generic for anything beyond basic CRUD.


## Handler Dependencies

Handlers directly inject the services and repositories they need — there is no intermediate `IXxxService` abstraction layer. Common injectable dependencies are:

| Dependency | Purpose |
|---|---|
| `IAppReadRepository` | Generic DB reads |
| `IAppCommandRepository` | Generic DB writes and transaction control |
| `IXxxReadRepository` (entity-specific) | Complex or domain-specific DB queries |
| `IXxxIntegration` | Outbound SAP/external API calls |
| `DataCipherService` | Data encryption/decryption |

All dependencies are registered in `AppUseCasesDI.cs`.


## Mapster — Global Profile

A single `MappingRegistration.cs` (`IRegister`) under `Registers/` holds all Mapster type mappings for the application.

- All Domain entity → DTO mappings are defined here
- Applied automatically at startup via Mapster's `IRegister` scan
- Do not define mapping logic in DTOs, handlers, or domain entities


## AppUseCasesDI.cs
DI registration entrypoint for `Application.UseCases`. Called from `Program.cs`. Register new handler dependencies and repository implementations here.


## Application.DataTransferObjects — Folder Structure

```
Application.DataTransferObjects/
├── Administration/       # User, role, and admin management DTOs
├── Commons/              # Base DTO classes (EntityDTO, AuditableDTO)
├── Others/               # Miscellaneous DTOs (MoneyDTO, CustomerDTO, VendorDTO, etc.) — flat, no subfolders
├── System/               # System settings DTOs (routes, module information, etc.)
└── Transactions/
    └── [Feature]/        # Transactional DTOs subfoldered per feature
```


## Application.DataTransferObjects — Rules

- No logic, no Mapster profiles, no mapping attributes — DTOs are plain data contracts only
- Mapping profiles belong exclusively in `Application.UseCases/Registers/MappingRegistration.cs`
- Never return Domain entities beyond the Application boundary — always map to a DTO first
- `Commons/` contains base DTO classes mirroring their Domain counterparts: `EntityDTO` and `AuditableDTO`. `TransactionalDocumentDTO` lives in `Transactions/Commons/`
- VO-derived DTOs live flat under `Others/` — no subfoldering

### Naming Convention
```
[Feature][OptionalSpecificUseCase]DTO
```
Examples:
- `UserDTO` — general user DTO
- `UserDataGridDTO` — user DTO shaped specifically for a data grid
- `MoneyDTO` — VO-derived DTO
- `OrderHeaderDTO` — transactional document header DTO

---

