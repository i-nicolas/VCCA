# 04 — Blazor & UI

[← Back to Index](../README.md)

---

## How the UI is Organized

All UI code lives in `Web.BlazorServer/`.

```
Web.BlazorServer/
├── Components/
│   ├── Base/
│   │   ├── BaseComponent.razor         ← Every component inherits this
│   │   └── BaseForm.razor              ← CVU pages inherit this (extends BaseComponent)
│   ├── Layout/
│   │   └── ProtectedLayout.razor       ← Wrap authenticated pages with this
│   ├── Pages/
│   │   └── [Feature]/                  ← One folder per feature
│   │       ├── OrderPage.razor         ← List/read-only page markup
│   │       ├── OrderPage.razor.cs      ← All logic (inherits BaseComponent)
│   │       ├── OrderCVU.razor.cs       ← Create/View/Update form (inherits BaseForm<TItem>)
│   │       └── OrderCVU.razor
│   ├── Security/                       ← Auth controller, services, policy providers
│   └── Shared/
│       ├── Abstraction/
│       │   └── AppDataGrid.razor       ← Reusable data grid wrapper
│       ├── Skeletons/                  ← Loading skeleton components
│       ├── Others/                     ← Header, Footer, NavigationMenu
│       └── CascadingValues/
├── Defaults/
│   ├── AppActions.cs                   ← Enum of all named UI actions
│   └── AppActionOptionPresets.cs       ← Preset factory for AppActionFactory options
├── Extensions/
├── Handlers/
│   ├── Repositories/[Feature]/         ← Handler interfaces (IXxxHandler) injected into pages
│   └── Implementations/[Feature]/      ← Thin MediatR dispatchers (call IMediator.Send())
├── Helpers/
│   └── AuthorizationHelper.cs          ← Permission-check helpers
├── Registers/
│   └── BlazorServerDI.cs               ← DI registration for web-layer services
├── Services/
│   ├── Repositories/                   ← IAlertService, IBusyService, IToastService, etc.
│   └── Implementation/                 ← Concrete service implementations
├── ViewModels/
│   ├── Administration/Role/            ← RoleVM, RolePermissionVM
│   ├── Administration/User/            ← UserVM, UserDataGridVM, etc.
│   ├── Commons/                        ← EntityVM, AuditableVM
│   ├── Enums/                          ← PageActionTypeEnum
│   ├── Others/                         ← AccountVM, PersonNameVM, etc.
│   ├── Security/                       ← AuthenticationVM
│   └── System/                         ← ModuleVM, NavigationRouteVM, etc.
└── wwwroot/
    ├── assets/                          ← Images and static files
    └── js/custom-scripts/               ← login.js, logout.js
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

`BaseComponent` provides shared lifecycle helpers, common services, and utilities so you don't have to set them up in every component. This includes `AppActionFactory`, `IBusyService`, `IToastService`, `IAlertService`, `NavigationManager`, `DialogService`, `IJSRuntime`, `ICurrentUserService`, `AuthorizationHelper`, and `AppAuthenticationService`. Never re-inject any of these — they are already available.

---

## BaseForm\<TItem\> — CVU Page Base

**Create/View/Update (CVU) pages inherit `BaseForm<TItem>` instead of `BaseComponent`.**

```
BaseComponent
  └── BaseForm<TItem>   ← CVU pages inherit this
```

`BaseForm<TItem>` manages form data binding (`FormData`, `FormDataClone`), `RadzenTemplateForm<TItem>` integration, unsaved-change tracking, and the submit/cancel lifecycle. It requires implementing three abstract methods: `InitializeEditing()`, `CancelEditing()`, and `HandleSubmit()`.

> Full reference: see `.agent-md/ui_abstractions.md`

---

## Handlers — The Injection Point

Components inject `IXxxHandler` interfaces from `Handlers/Repositories/[Feature]/` — **never** `IMediator`, `AppDbContext`, or infrastructure repositories directly.

```csharp
// ✅ Correct — inject the Handler interface
[Inject] private IOrderHandler OrderHandler { get; set; }

// ❌ Wrong — never inject these into components
[Inject] private IMediator Mediator { get; set; }
[Inject] private AppDbContext DbContext { get; set; }
```

`Handlers/Implementations/[Feature]/` contains the thin dispatchers that implement `IXxxHandler` by calling `IMediator.Send()`.

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
    // ✅ Inject the Handler interface, not IMediator or DbContext
    [Inject] private IOrderHandler OrderHandler { get; set; }

    private List<OrderVM> _orders = new();

    protected override async Task OnInitializedAsync()
    {
        var action = await AppActionFactory.RunAsync(
            async () => await OrderHandler.GetAllOrdersAsync(),
            AppActionOptionPresets.Loading(AppActions.GetAllOrders.GetDescription()));

        action.OnSuccess(result => { _orders = result?.Adapt<List<OrderVM>>() ?? []; });
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

CVU pages inherit `BaseForm<TItem>` and implement three abstract methods:

```csharp
// OrderCVU.razor.cs
public partial class OrderCVU : BaseForm<OrderVM>
{
    [Inject] private IOrderHandler OrderHandler { get; set; }

    protected override async Task InitializeEditing()
    {
        // Populate FormData when opening an existing record
        var action = await AppActionFactory.RunAsync(
            async () => await OrderHandler.GetOrderAsync(Id),
            AppActionOptionPresets.Loading(AppActions.GetOrder.GetDescription()));

        action.OnSuccess(dto => dto?.Adapt(FormData));
        AdaptToClone(); // snapshot for cancel/restore
    }

    protected override async Task HandleSubmit()
    {
        var action = await AppActionFactory.RunAsync(
            async () => await OrderHandler.SaveOrderAsync(FormData),
            AppActionOptionPresets.Confirmed(AppActions.SaveOrder.GetDescription()));

        action.OnSuccess(_ => NavManager.NavigateTo("/orders"));
    }

    protected override Task CancelEditing()
    {
        AdaptToForm(); // restore snapshot
        NavManager.NavigateTo("/orders");
        return Task.CompletedTask;
    }
}
```

---

### Radzen Notifications (Toasts)

Use `IToastService` for user-facing feedback — it is already injected in `BaseComponent` as `ToastService`. Do not inject `NotificationService` directly.

```csharp
// In .razor.cs — ToastService is already available via BaseComponent
private async Task HandleSaveAsync()
{
    var action = await AppActionFactory.RunAsync(
        async () => await OrderHandler.SaveOrderAsync(FormData),
        AppActionOptionPresets.Confirmed(AppActions.SaveOrder.GetDescription()));
    // AppActionFactory shows success/failure toasts automatically via AppActionOptions
}

// Manual toast (when not using AppActionFactory):
ToastService.Success("Order saved successfully.");
ToastService.Error("Something went wrong.");
```

`AppActionFactory` handles toasts automatically when `ShowToastOnSuccess` / `ShowToastOnFailure` are enabled in the options (the default for `Confirmed` and `Silent` presets).

---

### Radzen Dialogs

`DialogService` is already injected in `BaseComponent` — do not re-inject it.

```csharp
// In .razor.cs — DialogService is available via BaseComponent
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
