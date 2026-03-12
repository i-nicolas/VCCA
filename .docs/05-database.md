# 05 — Database & Data Access

[← Back to Index](../README.md)

---

## Overview

We use **Entity Framework Core (EF Core)** to interact with **Microsoft SQL Server**. All database code lives in the `Infrastructure` layer — never in Application or Presentation.

```
Database.Libraries    ← Interfaces and base abstractions (no SQL here)
Database.MsSql        ← Actual EF Core code, migrations, configurations
```

---

## The Repository Pattern

Instead of calling the database directly from handlers, we use **repositories** — classes that wrap all database interactions for a specific entity.

Think of a repository like a **filing cabinet** for one type of data. You ask it for orders, it gives you orders. You don't care how it stores them.

### Two Types of Repositories

We have both in the codebase — this is intentional, don't consolidate them:

| Type | Interface | Use When |
|---|---|---|
| **Generic** | `IRepository<T>` | Simple CRUD (Create, Read, Update, Delete) |
| **Specific** | `IOrderRepository` | Complex queries specific to an entity |

```csharp
// Generic — good for simple saves/reads
await _repository.AddAsync(order);
await _repository.GetByIdAsync(id);

// Specific — good for complex queries
await _orderRepository.GetOrdersWithLineItemsByCustomerAsync(customerId);
await _orderRepository.GetPendingOrdersOlderThanAsync(DateTime.UtcNow.AddDays(-7));
```

**Rule:** Prefer specific repositories for anything beyond basic CRUD.

---

## Entity Configuration

We configure EF Core table mappings **separately** from the entity class using `IEntityTypeConfiguration<T>`:

```csharp
// ✅ Configuration in Database.MsSql
public class OrderConfiguration : IEntityTypeConfiguration<OrderDEM>
{
    public void Configure(EntityTypeBuilder<OrderDEM> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CustomerName).HasMaxLength(200).IsRequired();
    }
}

// ❌ Don't use data annotations on the Domain entity
public class OrderDEM : EntityDEM
{
    [Required]           // ❌ No EF attributes in Domain
    [MaxLength(200)]     // ❌ Keep Domain clean
    public string CustomerName { get; private set; }
}
```

This keeps the Domain layer free of EF Core dependencies.

---

## Query Rules

### Always use `.AsNoTracking()` for reads

```csharp
// ✅ Read-only query — EF won't track changes (faster, uses less memory)
var orders = await _context.Orders
    .AsNoTracking()
    .Where(o => o.CustomerId == customerId)
    .ToListAsync();

// ❌ Missing AsNoTracking on a read query wastes resources
var orders = await _context.Orders
    .Where(o => o.CustomerId == customerId)
    .ToListAsync();
```

### Always use async methods

```csharp
// ✅ Correct
var orders = await _context.Orders.ToListAsync();
var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
await _context.SaveChangesAsync();

// ❌ Never use synchronous versions
var orders = _context.Orders.ToList();    // Blocks the thread
```

### Parameterized queries only

```csharp
// ✅ Safe — EF Core handles parameterization automatically
var orders = await _context.Orders
    .Where(o => o.CustomerName == customerName)
    .ToListAsync();

// ✅ Raw SQL — parameterized inputs only
var orders = await _context.Orders
    .FromSqlRaw("SELECT * FROM Orders WHERE CustomerName = {0}", customerName)
    .ToListAsync();

// ❌ NEVER string interpolation in raw SQL — SQL injection risk!
var orders = await _context.Orders
    .FromSqlRaw($"SELECT * FROM Orders WHERE CustomerName = '{customerName}'")
    .ToListAsync();
```

---

## Migrations

Migrations are how we update the database schema when the code changes.

### Creating a Migration

Run this from the **solution root** (not inside a project folder):

```bash
dotnet ef migrations add AddOrderStatusColumn \
  --project src/infrastructure/database/Database.MsSql \
  --startup-project src/presentation/Web.BlazorServer
```

**Always review the generated migration file before applying it.** EF Core sometimes generates unexpected changes.

### Applying Migrations

```bash
dotnet ef database update \
  --project src/infrastructure/database/Database.MsSql \
  --startup-project src/presentation/Web.BlazorServer
```

### Migration Rules

| Rule | Why |
|---|---|
| ✅ Always review before applying | EF can generate unexpected drops |
| ✅ Add a new migration to fix mistakes | Don't edit already-applied migrations |
| ❌ Never modify an applied migration | Breaks the migration history on other environments |

---

## Connection String

The database connection string is **never** in code or config files.

```bash
# Set as environment variable
ConnectionStrings__DefaultConnection="Server=...;Database=...;..."
```

See [10 — Building & Running](./10-building.md) for full setup instructions.

---

## Next Step

➡️ Read [06 — Domain & Business Rules](./06-domain.md) to understand how entities and business rules are structured.
