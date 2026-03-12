# 04 — Blazor & UI

[← Back to Index](../README.md)

---

## How the UI is Organized

All UI code lives in `src/presentation/Web.BlazorServer/`.

```
Web.BlazorServer/
├── Components/
│   ├── Base/
│   │   └── BaseComponent.razor         ← Every component inherits this
│   ├── Layout/
│   │   └── ProtectedLayout.razor       ← Wrap authenticated pages with this
│   ├── Pages/
│   │   └── [Feature]/                  ← One folder per feature
│   │       ├── OrderPage.razor         ← Markup only
│   │       ├── OrderPage.razor.cs      ← All logic goes here
│   │       ├── OrderCVUPage.razor      ← Create/View/Update page
│   │       └── OrderCVUPage.razor.cs
│   └── Shared/
│       ├── Abstraction/
│       │   └── AppDataGrid.razor       ← Reusable UI components
│       └── CascadingValues/
├── Handlers/
│   └── Implementations/[Feature]/      ← Web Handlers (MediatR dispatchers)
├── Repositories/
│   └── Implementations/[Feature]/      ← Web Repositories (injected into pages)
├── ViewModels/
│   └── [Feature]VM.cs                  ← UI display models
└── wwwroot/
    ├── assets/                          ← Images and static files
    └── js/                              ← Page-specific JavaScript
```

---

## BaseComponent — The Foundation

**Every component must inherit `BaseComponent`.**

```csharp
// ✅ Correct
@inherits BaseComponent

// ❌ Wrong — never skip this
// (no @inherits line)
```

`BaseComponent` provides shared lifecycle helpers, common services, and utilities so you don't have to set them up in every component. Never duplicate what it already provides.

---

## Code-Behind Convention

Each page has **two files** that work together:

| File | Contains |
|---|---|
| `OrderPage.razor` | HTML-like markup only — what the user sees |
| `OrderPage.razor.cs` | C# logic — event handlers, lifecycle, DI |

### The `.razor` file (markup only)

```razor
@page "/orders"
@inherits BaseComponent

<h1>Orders</h1>

@if (_isLoading)
{
    <p>Loading...</p>
}
else
{
    <AppDataGrid Items="@_orders" />
}

<button @onclick="HandleSaveAsync">Save</button>
```

### The `.razor.cs` file (all logic)

```csharp
// OrderPage.razor.cs
public partial class OrderPage : BaseComponent
{
    // ✅ Inject the Web Repository, not IMediator or DbContext
    [Inject] private IOrderRepository OrderRepository { get; set; }

    private List<OrderVM> _orders = new();
    private bool _isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        _orders = await OrderRepository.GetAllOrdersAsync();
        _isLoading = false;
    }

    private async Task HandleSaveAsync()
    {
        await OrderRepository.SaveOrderAsync(_selectedOrder);
    }
}
```

---

## ViewModels vs DTOs

These two things look similar but serve different purposes:

| | ViewModel (`[Feature]VM.cs`) | DTO (`Application.DataTransferObjects`) |
|---|---|---|
| **Where** | `Web.BlazorServer/ViewModels/` | `Application.DataTransferObjects/` |
| **Purpose** | Display in the UI | Data contract between layers |
| **Can contain** | `IsSelected`, `IsEditing`, formatted display strings | Plain data properties |
| **Returned from handlers?** | ❌ Never | ✅ Yes |

```csharp
// DTO — plain data from Application layer
public class OrderDto
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; }
    public decimal Total { get; set; }
}

// ViewModel — UI-specific, may have extra display fields
public class OrderVM
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; }
    public decimal Total { get; set; }
    public bool IsSelected { get; set; }       // UI state
    public bool IsEditing { get; set; }        // UI state
    public string FormattedTotal => $"₱{Total:N2}";  // Display formatting
}
```

**Mapping happens in the component:**

```csharp
var dto = await OrderRepository.GetOrderAsync(id);
var viewModel = dto.Adapt<OrderVM>(); // Map DTO → ViewModel inside the component
```

---

## Async Rules — Very Important

Blazor Server runs on a **SignalR connection**. Blocking that connection will freeze the UI for all users.

```csharp
// ✅ Always async
protected override async Task OnInitializedAsync()
{
    _data = await SomeRepository.GetDataAsync();
}

// ❌ Never block — this can freeze the entire app
var data = SomeRepository.GetDataAsync().Result;      // .Result blocks!
SomeRepository.GetDataAsync().Wait();                  // .Wait() blocks!
SomeRepository.GetDataAsync().GetAwaiter().GetResult(); // Same problem!
```

### When to call `StateHasChanged()`

Only call `StateHasChanged()` when you change state **outside** of a normal Blazor event callback:

```csharp
// ✅ Needed — state changed outside Blazor's event cycle (e.g. from a timer or external event)
_timer = new Timer(async _ =>
{
    _count++;
    await InvokeAsync(StateHasChanged);
}, null, 0, 1000);

// ❌ Not needed — Blazor automatically re-renders after @onclick handlers
private async Task HandleClickAsync()
{
    _items = await Repository.GetItemsAsync();
    // No need for StateHasChanged() here
}
```

---

## Radzen UI Components

We use **[Radzen Blazor Components](https://blazor.radzen.com/)** as our primary UI component library. Radzen provides a rich set of production-ready components — data grids, dialogs, forms, dropdowns, date pickers, and more — all built for Blazor Server.

> 📖 **Official Docs:** https://blazor.radzen.com/docs/

### Installing Radzen

Radzen is available as a NuGet package:

```bash
dotnet add package Radzen.Blazor
```

Then register it in `Program.cs`:

```csharp
builder.Services.AddRadzenComponents();
```

And import the stylesheet in your layout (e.g. `_Host.cshtml` or `App.razor`):

```html
<link rel="stylesheet" href="_content/Radzen.Blazor/css/default.css" />
```

---

### Common Radzen Components

| Component | Tag | Use For |
|---|---|---|
| Text Input | `<RadzenTextBox>` | Single-line text fields |
| Number Input | `<RadzenNumeric>` | Numeric fields with type safety |
| Dropdown | `<RadzenDropDown>` | Select from a list |
| Date Picker | `<RadzenDatePicker>` | Date/time selection |
| Data Grid | `<RadzenDataGrid>` | Tabular data display |
| Button | `<RadzenButton>` | Actions |
| Dialog | `<RadzenDialog>` | Modal popups |
| Notification | `<RadzenNotification>` | Toast messages |

---

### Forms & Validation with Radzen

We use **Radzen's `RadzenTemplateForm`** for forms — not Blazor's built-in `EditForm`.

```razor
<!-- ❌ Don't use Blazor's built-in EditForm -->
<EditForm Model="@_order">
    <DataAnnotationsValidator />
</EditForm>

<!-- ✅ Use RadzenTemplateForm -->
<RadzenTemplateForm TItem="OrderVM" Data="@_order" Submit="HandleSaveAsync">
    
    <RadzenFormField Text="Customer Name" Style="width: 100%">
        <RadzenTextBox @bind-Value="@_order.CustomerName" />
    </RadzenFormField>

    <RadzenFormField Text="Total Amount" Style="width: 100%">
        <RadzenNumeric TValue="decimal" @bind-Value="@_order.Total" />
    </RadzenFormField>

    <RadzenFormField Text="Order Date" Style="width: 100%">
        <RadzenDatePicker TValue="DateTime" @bind-Value="@_order.OrderDate" />
    </RadzenFormField>

    <RadzenFormField Text="Status" Style="width: 100%">
        <RadzenDropDown @bind-Value="@_order.StatusId"
                        Data="@_statusOptions"
                        TextProperty="Label"
                        ValueProperty="Value" />
    </RadzenFormField>

    <RadzenButton ButtonType="ButtonType.Submit" Text="Save" />
    <RadzenButton ButtonStyle="ButtonStyle.Light" Text="Cancel" Click="HandleCancelAsync" />

</RadzenTemplateForm>
```

Validation logic still lives in the `.razor.cs` file — Radzen's form simply calls `HandleSaveAsync` on submit:

```csharp
// OrderCVUPage.razor.cs
public partial class OrderCVUPage : BaseComponent
{
    [Inject] private IOrderRepository OrderRepository { get; set; }

    private OrderVM _order = new();
    private List<StatusOption> _statusOptions = new();

    protected override async Task OnInitializedAsync()
    {
        _statusOptions = await OrderRepository.GetStatusOptionsAsync();
    }

    private async Task HandleSaveAsync()
    {
        // Validate first — validation logic lives here, not in markup
        if (string.IsNullOrWhiteSpace(_order.CustomerName))
        {
            // show notification or set validation message
            return;
        }

        await OrderRepository.SaveOrderAsync(_order);
    }

    private void HandleCancelAsync()
    {
        NavigationManager.NavigateTo("/orders");
    }
}
```

---

### Radzen Notifications (Toasts)

Use `NotificationService` for user-facing feedback:

```csharp
// In .razor.cs — inject the service
[Inject] private NotificationService NotificationService { get; set; }

private async Task HandleSaveAsync()
{
    await OrderRepository.SaveOrderAsync(_order);

    NotificationService.Notify(new NotificationMessage
    {
        Severity = NotificationSeverity.Success,
        Summary = "Saved",
        Detail = "Order saved successfully.",
        Duration = 3000
    });
}
```

Add `<RadzenNotification />` once in your layout to enable it globally.

---

### Radzen Dialogs

```csharp
// In .razor.cs — inject the service
[Inject] private DialogService DialogService { get; set; }

private async Task HandleDeleteAsync(Guid orderId)
{
    var confirmed = await DialogService.Confirm(
        "Are you sure you want to delete this order?",
        "Confirm Delete",
        new ConfirmOptions { OkButtonText = "Delete", CancelButtonText = "Cancel" }
    );

    if (confirmed == true)
        await OrderRepository.DeleteOrderAsync(orderId);
}
```

Add `<RadzenDialog />` once in your layout to enable it globally.

> 📖 Full component reference and live examples: **https://blazor.radzen.com/**

---

## Layout & Protected Pages

Authenticated pages use `ProtectedLayout`:

```razor
@page "/orders"
@layout ProtectedLayout
@inherits BaseComponent
```

---

## Next Step

➡️ Read [05 — Database & Data Access](./05-database.md) to learn how data is stored and retrieved.
