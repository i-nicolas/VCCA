# 06 — Domain & Business Rules

[← Back to Index](../README.md)

---

## What Is the Domain Layer?

The Domain layer is the **core of the system** — it defines what data looks like and what rules govern it. It has zero dependencies on anything else (no EF Core, no HTTP, no MediatR\*).

> \* Domain does reference MediatR for `INotification` on domain events. This is a known architectural debt — see [Architectural Debts](./99-architectural-debts.md).

**Location:** `core/domain/Domain.Entities`

---

## EntityDEM — The Base of All Entities

Every entity in the system inherits from `EntityDEM`:

```csharp
public abstract class EntityDEM : IDomainEntity
{
    public Guid Id { get; private set; }  // Auto-generated unique ID

    protected EntityDEM()
    {
        Id = Guid.NewGuid();
        DomainEvents = [];
    }
}
```

There's also `AuditableDEM` for entities that need audit tracking:

```
EntityDEM               ← Base: has Id
  └── AuditableDEM      ← Adds: CreatedBy, CreatedDate, UpdatedBy, UpdatedDate
        └── OrderDEM    ← Your entity
```

Use `AuditableDEM` for anything the business needs to track changes on.

---

## How to Build an Entity

Entities follow a consistent pattern:

### 1. Private setters — protect the data

```csharp
public class OrderDEM : AuditableDEM
{
    // ✅ Private set — can only change through methods
    public string CustomerName { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal Total { get; private set; }
}
```

### 2. Static factory method — the only way to create

```csharp
// ✅ Controlled creation
public static OrderDEM Create(string customerName, decimal total)
{
    return new OrderDEM(customerName, total);
}

// ✅ Private constructor — forces use of Create()
private OrderDEM(string customerName, decimal total)
{
    CustomerName = Guard.Against.NullOrEmpty(customerName, nameof(customerName));
    Total = Guard.Against.NegativeOrZero(total, nameof(total));
    Status = OrderStatus.Pending;
}
```

### 3. Public methods for changes — never direct assignment

```csharp
// ✅ State changes go through methods
public void Approve()
{
    if (Status != OrderStatus.Pending)
        throw new InvalidOperationException("Only pending orders can be approved.");
    Status = OrderStatus.Approved;
}

public void UpdateCustomerName(string newName)
{
    CustomerName = Guard.Against.NullOrEmpty(newName, nameof(newName));
}
```

### Full Example

```csharp
public class OrderDEM : AuditableDEM
{
    public string CustomerName { get; private set; }
    public OrderStatus Status { get; private set; }

    private readonly List<OrderLineDEM> _lines = [];
    public IReadOnlyCollection<OrderLineDEM> Lines => _lines.AsReadOnly();

    private OrderDEM() { } // Required by EF Core

    private OrderDEM(string customerName)
    {
        CustomerName = Guard.Against.NullOrEmpty(customerName, nameof(customerName));
        Status = OrderStatus.Pending;
    }

    public static OrderDEM Create(string customerName)
        => new(customerName);

    public void AddLine(OrderLineDEM line)
    {
        Guard.Against.Null(line, nameof(line));
        _lines.Add(line);
    }

    public void Approve()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Only pending orders can be approved.");
        Status = OrderStatus.Approved;
    }
}
```

---

## Ardalis.Guards — Input Validation

We use `Guard.Against.*` to validate inputs in entity constructors and methods. This makes validation expressive and consistent.

```csharp
// Common guards
Guard.Against.Null(value, nameof(value));                    // Must not be null
Guard.Against.NullOrEmpty(text, nameof(text));               // Must not be null or ""
Guard.Against.NullOrWhiteSpace(text, nameof(text));          // Must not be null, "", or "   "
Guard.Against.NegativeOrZero(number, nameof(number));        // Must be > 0
Guard.Against.Negative(number, nameof(number));              // Must be >= 0
Guard.Against.OutOfRange(value, nameof(value), min, max);    // Must be within range
```

---

## Value Objects

Value objects represent a concept **by their value**, not by an ID. Two `MoneyVO` objects with the same amount are considered equal.

```csharp
public class MoneyVO
{
    public decimal Amount { get; }
    public string Currency { get; }

    public MoneyVO(decimal amount, string currency)
    {
        Amount = Guard.Against.Negative(amount, nameof(amount));
        Currency = Guard.Against.NullOrEmpty(currency, nameof(currency));
    }

    // Value objects are equal if their values match
    public override bool Equals(object? obj)
        => obj is MoneyVO other && Amount == other.Amount && Currency == other.Currency;
}
```

---

## Domain Events

Domain events let one part of the system notify other parts that something happened, **without tight coupling**.

```csharp
// 1. Define the event
public class OrderApprovedEvent : DomainBaseEvent
{
    public Guid OrderId { get; }
    public OrderApprovedEvent(Guid orderId) => OrderId = orderId;
}

// 2. Fire it from the entity
public void Approve()
{
    Status = OrderStatus.Approved;
    DomainEvents.Add(new OrderApprovedEvent(Id));  // Raise the event
}

// 3. Handle it in Application layer (via MediatR INotificationHandler)
public class OrderApprovedHandler : INotificationHandler<OrderApprovedEvent>
{
    public async Task Handle(OrderApprovedEvent notification, CancellationToken ct)
    {
        // e.g. Send a confirmation email, update SAP, etc.
    }
}
```

---

## Enums

All domain-relevant enums live in the Domain layer:

```csharp
// Domain.Entities/Enums/OrderStatus.cs
public enum OrderStatus
{
    [Description("Pending")]
    Pending,

    [Description("Approved")]
    Approved,

    [Description("Cancelled")]
    Cancelled
}
```

---

## ⚠️ Aggregate Root Warning

> The codebase has **inconsistent aggregate root enforcement** — some child entities can be modified from outside the aggregate root. Before writing a handler that modifies child entities, verify how that specific aggregate is structured.

This is a known architectural debt. See [Architectural Debts](./99-architectural-debts.md).

---

## Next Step

➡️ Read [07 — Authentication](./07-auth.md) to learn how login and permissions work.
