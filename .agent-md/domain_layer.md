# Domain Layer

## Project
`Domain.Entities/`

## Purpose
Single source of truth for all business models, rules, and domain events. Zero EF Core dependencies.


## Project Structure

```
Domain.Entities/
├── Commons/              # Abstract base classes (EntityDEM, AuditableDEM)
├── Entities/             # Concrete domain entities, organized by feature/subdomain
│   ├── Administration/   # User and role entities
│   ├── System/           # System-level entities (e.g. ModuleDEM)
│   └── Transaction/      # Transactional entities
│       └── Common/       # TransactionalDocumentDEM and shared transactional types
├── Enums/                # All system-wide enums (single source of truth)
├── Events/
│   └── Bases/            # DomainBaseEvent — base class for all domain events
├── Extensions/           # Domain-specific extension methods (e.g. DomainEntityExtensions)
├── Markers/              # Tag interfaces for type identification (e.g. IEntity)
├── Providers/            # Concrete provider implementations (e.g. DateTimeProvider)
└── ValueObjects/         # Immutable value objects (e.g. MoneyVO)
```


## Naming Convention

| Type | Suffix | Example |
|---|---|---|
| Domain Entity Model | `[Feature]DEM` | `ModuleDEM`, `EntityDEM` |
| Domain Base Event | `DomainBaseEvent` | (base class) |
| Value Object | `[Concept]VO` | `MoneyVO` |
| Marker interface | `I[Concept]` | `IEntity` |
| Provider interface | `I[Concept]Provider` | (in `/Providers/`) |

**DEM = Domain Entity Model.** All domain entity classes carry this suffix.


## Base Classes (Commons)

### `EntityDEM`
Root base class. All domain entities inherit from `EntityDEM`, directly or indirectly.

- Carries the domain event collection:
  ```csharp
  public List<DomainBaseEvent> DomainEvents { get; protected set; }
  ```
- Domain events are raised onto this list by the entity and dispatched by the Application layer after persistence.

### `AuditableDEM`
Extends `EntityDEM`. Used for all entities that require audit tracking (created/modified metadata).
All auditable entities inherit from `AuditableDEM` rather than `EntityDEM` directly.

### `TransactionalDocumentDEM`
`Entities/Transaction/Common/` — extends `AuditableDEM`. Base for all transactional document entities (orders, receipts, etc.). Provides document number, date, and status fields.
Chain: `EntityDEM → AuditableDEM → TransactionalDocumentDEM`


## Domain Events

### `DomainBaseEvent`
Located in `Events/Bases/DomainBaseEvent.cs`. Inherits MediatR's `INotification`.
All domain events extend `DomainBaseEvent`.

> ⚠️ **Architectural Debt:** The Domain referencing MediatR for `INotification` is a known coupling issue.
> Do not attempt to fix this without explicit instruction. See `architectural_debts.md`.


## Enums
Domain is the single source of truth for all business-domain enums. Place in `Domain.Entities/Enums/` unless explicitly Kernel- or Shared-scoped.

## Markers
Tag interfaces (`IEntity`, etc.) for type identification — no members, presence is purpose.

## Extensions / Providers
Domain-scoped helpers (`DomainEntityExtensions`, `DateTimeProvider`). No application or infrastructure concerns here.

## Value Objects
Immutable, identity-free types (e.g. `MoneyVO`). Equality by value. Business rules belong on the VO itself.


## Rules

- Never add EF Core references or data annotations to any Domain class
- Never return Domain entities beyond the Application boundary — always map to a DTO in `Application.UseCases`
- Domain logic belongs on entities and value objects — not in handlers, repositories, or services
- Aggregate roots must own all mutations to their child entities
  - ⚠️ This is currently inconsistently enforced — verify per aggregate before writing handlers (see `architectural_debts.md`)
