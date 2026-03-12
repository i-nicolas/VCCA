# 02 — Layer Guide

[← Back to Index](../README.md)

---

## Overview

Each layer in VCCA has a **single, clear responsibility**. If you're ever unsure where a piece of code belongs, refer to this guide.

---

## Layer 1: Domain — The Rules Engine

**Location:** `core/domain/Domain.Entities`

This is the **heart of the system**. It contains the data models and the rules that govern them. It has absolutely **no knowledge** of databases, HTTP, or UI.

### What lives here

| Thing | Description | Example |
|---|---|---|
| **Entities** | The main data objects | `OrderDEM`, `UserDEM`, `ModuleDEM` |
| **Value Objects** | Immutable, identity-free values | `MoneyVO`, `AddressVO` |
| **Enums** | System-wide enumerations | `AuditType`, `OrderStatus` |
| **Domain Events** | Notifications fired when something important happens | `OrderCreatedEvent` |
| **Aggregate Roots** | Entities that own and protect child data | An `Order` controlling its `OrderLines` |

### What does NOT live here

- ❌ Database code (no EF Core)
- ❌ HTTP or web code
- ❌ Application logic (workflows)

### Example Entity

```csharp
public class ModuleDEM : EntityDEM
{
    public string Name { get; private set; }
    public bool Active { get; private set; }

    // All changes go through methods — properties are private set
    public void Activate() => Active = true;
    public void Deactivate() => Active = false;

    // Factory method — the only way to create a Module
    public static ModuleDEM Create(string name, string code, ...)
    {
        return new ModuleDEM(name, code, ...);
    }
}
```

> 💡 **Why private setters?** This forces all state changes through controlled methods, protecting business rules. You can't accidentally set `Active = true` from outside — you must call `Activate()`.

---

## Layer 2: Application — The Orchestrator

**Location:** `core/application/Application.UseCases` and `Application.DataTransferObjects`

This layer **coordinates** what happens in response to a request. It calls the domain and infrastructure (via interfaces) but contains no UI or database code itself.

### What lives here

| Thing | Description |
|---|---|
| **MediatR Handlers** | Process a specific command or query |
| **Commands** | Requests to *change* something (`CreateOrderCommand`) |
| **Queries** | Requests to *read* something (`GetOrderByIdQuery`) |
| **IXxxService interfaces** | Internal contracts that Infrastructure must implement |
| **Mapster Profiles** | Rules for converting Domain entities to DTOs |
| **DTOs** | Simple data packets with no behavior |

### Folder Structure

```
Application.UseCases/
├── Commands/
│   └── Orders/
│       ├── CreateOrderCommand.cs
│       └── CreateOrderHandler.cs
└── Queries/
    └── Orders/
        ├── GetOrderByIdQuery.cs
        └── GetOrderByIdHandler.cs
```

### Example Handler

```csharp
public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orderRepository;

    public CreateOrderHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        // 1. Create the domain entity
        var order = OrderDEM.Create(request.CustomerId, request.Items);

        // 2. Save via repository (infrastructure does the actual saving)
        await _orderRepository.AddAsync(order);

        // 3. Map to DTO and return (never return the raw entity)
        return order.Adapt<OrderDto>();
    }
}
```

### What does NOT live here

- ❌ Blazor or UI code
- ❌ Raw SQL or EF Core DbContext calls
- ❌ ViewModels (those are Presentation-layer concerns)

---

## Layer 3: Infrastructure — The Technical Details

**Locations:**
- `src/infrastructure/database/Database.Libraries` — interfaces & abstractions
- `src/infrastructure/database/Database.MsSql` — concrete implementations
- `src/integration/Integration.Sap` — SAP HTTP client

This layer **implements** the contracts defined by the Application layer.

### What lives here

| Project | Contents |
|---|---|
| **Database.Libraries** | `IRepository<T>`, EF Core base classes |
| **Database.MsSql** | `AppDbContext`, migrations, `XxxRepository` implementations, entity configs |
| **Integration.Sap** | HTTP client methods for calling SAP APIs |

### Repository Pattern

We use two types of repositories (both are fine — don't consolidate them):

```
IRepository<T>       Generic CRUD — use for simple operations
IXxxRepository       Specific to an entity — use for complex queries
```

### What does NOT live here

- ❌ Business rules
- ❌ UI code
- ❌ DTO definitions

---

## Layer 4: Presentation — The Entry Points

**Location:** `src/presentation/Web.BlazorServer`

This is the only layer users directly interact with. Its sole job is to **receive input, pass it to the Application layer, and display the result**.

### What lives here

| Thing | Description |
|---|---|
| **Blazor Components** | `.razor` + `.razor.cs` page files |
| **Web Repositories** | Per-feature groupings of MediatR dispatch calls |
| **Web Handlers** | Thin wrappers that call `IMediator.Send()` |
| **ViewModels** | UI-specific display models (not DTOs) |
| **Security** | JWT auth, authentication state provider |

### What does NOT live here

- ❌ Business logic
- ❌ Direct database access
- ❌ Direct SAP calls

---

## Quick Reference: "Where Does This Go?"

| I need to... | It belongs in... |
|---|---|
| Define what an Order looks like | `Domain.Entities` |
| Write a rule like "orders can't be empty" | `Domain.Entities` |
| Create a "Place Order" feature | `Application.UseCases/Commands/Orders/` |
| Read a list of orders | `Application.UseCases/Queries/Orders/` |
| Convert an Order entity to a DTO | `Application.UseCases` (Mapster profile) |
| Write the actual SQL/EF query | `Database.MsSql` |
| Create a page for placing orders | `Web.BlazorServer/Components/Pages/` |
| Format an order for display | `Web.BlazorServer/ViewModels/` |
| Call SAP for order data | `Integration.Sap` |
| Write a string helper method | `Shared` |

---

## Next Step

➡️ Read [03 — Request Pipeline](./03-request-pipeline.md) to see how all these layers connect when a user does something.
