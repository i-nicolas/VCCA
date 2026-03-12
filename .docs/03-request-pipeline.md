# 03 — The Request Pipeline

[← Back to Index](../README.md)

---

## What Is the Request Pipeline?

The **request pipeline** is the path that every user action travels through — from a button click all the way to the database and back.

> ⚠️ **This is the most important thing to understand.** Every feature you build follows this exact path. Never skip a step.

---

## The Full Pipeline

```
👤 User clicks a button
        │
        ▼
┌───────────────────────────────┐
│   Blazor Component            │  .razor / .razor.cs
│   (inherits BaseComponent)    │  Handles UI events
└──────────────┬────────────────┘
               │  calls
               ▼
┌───────────────────────────────┐
│   Web Repository              │  /Repositories/[Feature]/
│   (injected via @inject)      │  Groups related operations
└──────────────┬────────────────┘
               │  calls
               ▼
┌───────────────────────────────┐
│   Web Handler                 │  /Handlers/[Feature]/
│                               │  Calls IMediator.Send()
└──────────────┬────────────────┘
               │  IMediator.Send(command/query)
               ▼
┌───────────────────────────────┐
│   Application Handler         │  Application.UseCases
│   (MediatR handler)           │  Orchestrates the use case
└──────────────┬────────────────┘
               │  calls
               ▼
┌───────────────────────────────┐
│   IXxxRepository              │  Database.Libraries (interface)
│   (infrastructure contract)   │  XxxRepository (Database.MsSql)
└──────────────┬────────────────┘
               │  queries
               ▼
┌───────────────────────────────┐
│   AppDbContext                │  Database.MsSql
│                               │  EF Core → SQL Server
└───────────────────────────────┘
```

---

## Step-by-Step Walkthrough

Let's trace a real example: **"User saves an Order"**

### Step 1 — Blazor Component triggers the action

```csharp
// OrderPage.razor.cs
private async Task HandleSaveAsync()
{
    var result = await OrderRepository.SaveOrderAsync(ViewModel);
    // Update UI...
}
```

- The component **only talks to its Web Repository**
- No direct database calls, no direct MediatR here

### Step 2 — Web Repository packages the request

```csharp
// Repositories/Implementations/Orders/OrderRepository.cs
public async Task<OrderDto> SaveOrderAsync(OrderVM viewModel)
{
    var command = new SaveOrderCommand(viewModel.Id, viewModel.Items);
    return await _handler.SaveOrderAsync(command);
}
```

- Groups all order-related operations in one place
- Maps the ViewModel into a Command (Application-layer type)

### Step 3 — Web Handler dispatches to MediatR

```csharp
// Handlers/Implementations/Orders/OrderHandler.cs
public async Task<OrderDto> SaveOrderAsync(SaveOrderCommand command)
{
    return await _mediator.Send(command);
}
```

- This is the **only** place `IMediator.Send()` is called
- No logic here — just dispatch

### Step 4 — Application Handler processes the use case

```csharp
// Application.UseCases/Commands/Orders/SaveOrderHandler.cs
public async Task<OrderDto> Handle(SaveOrderCommand request, CancellationToken ct)
{
    var order = await _orderRepository.GetByIdAsync(request.Id);

    order.UpdateItems(request.Items); // Domain method enforces rules

    await _orderRepository.UpdateAsync(order);

    return order.Adapt<OrderDto>(); // Map to DTO, never return raw entity
}
```

- Orchestrates the work: load → modify → save → return
- Uses domain methods (not direct property assignments)
- Returns a DTO, never a Domain entity

### Step 5 — Repository hits the database

```csharp
// Database.MsSql/Repositories/OrderRepository.cs
public async Task UpdateAsync(OrderDEM order)
{
    _context.Orders.Update(order);
    await _context.SaveChangesAsync();
}
```

- Pure EF Core data access
- No business logic here

---

## The Return Journey

Data flows back **the same way in reverse**:

```
Database result
  → Repository returns Domain entity
    → Handler maps entity to DTO (Mapster)
      → MediatR returns DTO to Web Handler
        → Web Repository returns DTO to Component
          → Component maps DTO to ViewModel for display
```

> 💡 **DTOs vs ViewModels:** DTOs are the Application layer's data format. ViewModels are the UI's display format. Never use a ViewModel in Application code, and never display raw DTOs directly in components — always map.

---

## What NOT to Do

```csharp
// ❌ WRONG — Injecting IMediator directly into a component
@inject IMediator Mediator
await Mediator.Send(new SomeCommand()); // Skip the Repository/Handler chain

// ❌ WRONG — Calling the database from a component
@inject AppDbContext DbContext
var orders = await DbContext.Orders.ToListAsync(); // Infrastructure in Presentation!

// ❌ WRONG — Putting business logic in a Web Handler
public async Task<OrderDto> SaveAsync(SaveOrderCommand command)
{
    if (command.Items.Count == 0) throw new Exception("..."); // Logic doesn't belong here!
    return await _mediator.Send(command);
}
```

---

## Quick Checklist for New Features

When building a new feature, create files in this order:

1. ✅ **Domain** — Does the entity already exist? If not, create/update it
2. ✅ **Command/Query** — Define what data the request needs
3. ✅ **Application Handler** — Write the use case logic
4. ✅ **DTO** — Define what data comes back
5. ✅ **Web Handler** — Thin dispatcher calling `IMediator.Send()`
6. ✅ **Web Repository** — Group the operation with related ones
7. ✅ **ViewModel** — UI-specific display model
8. ✅ **Blazor Component** — The page/component the user sees

---

## Next Step

➡️ Read [04 — Blazor & UI](./04-blazor-ui.md) to learn how to build UI components correctly.
