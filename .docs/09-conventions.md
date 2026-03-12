# 09 — Code Conventions

[← Back to Index](../README.md)

---

## Naming Conventions

Consistent naming makes code predictable. Always follow this table:

| Type | Pattern | Example |
|---|---|---|
| Service interface | `IXxxService` | `IOrderService` |
| Service implementation | `XxxService` | `OrderService` |
| Repository interface | `IXxxRepository` | `IOrderRepository` |
| Repository implementation | `XxxRepository` | `OrderRepository` |
| MediatR command | `XxxCommand` | `CreateOrderCommand` |
| MediatR query | `XxxQuery` | `GetOrderByIdQuery` |
| MediatR handler | `XxxHandler` | `CreateOrderHandler` |
| DTO | `XxxDto` | `OrderDto` |
| Domain entity | `XxxDEM` | `OrderDEM` |
| Value object | `XxxVO` | `MoneyVO` |
| Blazor page | `XxxPage` | `OrderListPage` |
| Blazor component | `XxxComponent` | `OrderCardComponent` |
| ViewModel | `XxxVM` | `OrderVM` |

---

## File Organization

- **One class per file** — always
- **Filename must match class name** exactly
- Feature folders group related files together:

```
Commands/
└── Orders/
    ├── CreateOrderCommand.cs    ← Command definition
    └── CreateOrderHandler.cs   ← Handler for that command
```

---

## Dependency Injection

Always use **constructor injection**. Never use service locator.

```csharp
// ✅ Constructor injection
public class OrderService : IOrderService
{
    private readonly IOrderRepository _repository;

    public OrderService(IOrderRepository repository)
    {
        _repository = repository;
    }
}

// ❌ Service locator — don't do this
public class OrderService : IOrderService
{
    public async Task DoSomething()
    {
        var repo = ServiceLocator.Get<IOrderRepository>(); // ❌
    }
}
```

---

## Async/Await

Use `async/await` **everywhere**. This is non-negotiable.

```csharp
// ✅ Correct
public async Task<OrderDto> GetOrderAsync(Guid id)
{
    return await _repository.GetByIdAsync(id);
}

// ❌ These will cause problems — never use them
var result = GetOrderAsync(id).Result;
GetOrderAsync(id).Wait();
GetOrderAsync(id).GetAwaiter().GetResult();
```

---

## Error Handling

We have two different strategies depending on the type of error:

| Error Type | Strategy | Example |
|---|---|---|
| **Expected / business failure** | Return a value (`null`, `bool`, result wrapper) | "Order not found" → return `null` |
| **Unexpected / system error** | Throw an exception | Database connection failure → throw |

```csharp
// ✅ Expected failure — return null (caller handles the "not found" case)
public async Task<OrderDto?> GetOrderAsync(Guid id)
{
    var order = await _repository.GetByIdAsync(id);
    return order?.Adapt<OrderDto>(); // Returns null if not found
}

// ✅ Unexpected failure — throw (global middleware catches it)
public async Task<OrderDto> GetRequiredOrderAsync(Guid id)
{
    var order = await _repository.GetByIdAsync(id)
        ?? throw new InvalidOperationException($"Order {id} was expected but not found.");
    return order.Adapt<OrderDto>();
}

// ❌ Don't throw exceptions for normal business cases
public async Task<OrderDto> GetOrderAsync(Guid id)
{
    var order = await _repository.GetByIdAsync(id);
    if (order == null)
        throw new Exception("Order not found"); // ❌ This is an expected case, not a crash
}
```

---

## Interfaces

**All services and repositories must have interfaces.** This enables dependency injection, testing, and swapping implementations.

```csharp
// ✅ Always define the interface
public interface IOrderRepository
{
    Task<OrderDEM?> GetByIdAsync(Guid id);
    Task AddAsync(OrderDEM order);
}

// ✅ Then implement it
public class OrderRepository : IOrderRepository
{
    // ...
}
```

---

## Things You Must Never Do

| ❌ Never Do This | Why |
|---|---|
| `.Result`, `.Wait()`, `.GetAwaiter().GetResult()` | Blocks threads, causes deadlocks in Blazor |
| Raw SQL with string interpolation | SQL injection vulnerability |
| Returning Domain entities across layer boundaries | Breaks encapsulation, exposes business internals |
| Hardcoding secrets, connection strings, or JWT keys | Security risk, will be committed to git |
| Using `dynamic` or anonymous types across layers | Breaks type safety and intellisense |
| Injecting `IMediator` or `AppDbContext` into Blazor components | Skips the pipeline, violates layering |

---

## Layer Boundary Rules

```
✅ Presentation  → can use → Application
✅ Application   → can use → Domain
✅ Application   → can use → Infrastructure (via interfaces)
✅ Infrastructure → implements → Application interfaces

❌ Domain        → must NOT use → Application
❌ Domain        → must NOT use → Infrastructure
❌ Application   → must NOT use → Presentation
❌ Presentation  → must NOT use → Infrastructure directly
```

---

## Next Step

➡️ Read [10 — Building & Running](./10-building.md) to set up your local environment.
