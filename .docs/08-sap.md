# 08 — SAP Integration

[← Back to Index](../README.md)

---

## Overview

SAP is an external system that a VCCA-structured project communicates with for certain business data. All SAP communication is **outbound only** (we call SAP, SAP doesn't call us).

**Location:** `src/integration/Integration.Sap`

---

## The Rule

> **All SAP HTTP calls must go through `Integration.Sap`.** No exceptions.

```
Blazor Component
  → Web Repository
    → Web Handler → IMediator.Send()
      → Application Handler
        → Integration.Sap   ← SAP calls only happen here
          → SAP External API
```

---

## How to Add a New SAP Endpoint

Follow these steps in order:

### Step 1 — Add the HTTP client method in `Integration.Sap`

```csharp
// Integration.Sap/Services/SapOrderService.cs
public async Task<SapOrderResponse> GetOrderFromSapAsync(string sapOrderNumber)
{
    var response = await _httpClient.GetAsync($"/api/orders/{sapOrderNumber}");
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<SapOrderResponse>();
}
```

### Step 2 — Add a MediatR Command/Query + Handler in `Application.UseCases`

```csharp
// Application.UseCases/Queries/Orders/GetSapOrderQuery.cs
public record GetSapOrderQuery(string SapOrderNumber) : IRequest<SapOrderDto>;

// Application.UseCases/Queries/Orders/GetSapOrderHandler.cs
public class GetSapOrderHandler : IRequestHandler<GetSapOrderQuery, SapOrderDto>
{
    private readonly ISapOrderService _sapService;

    public GetSapOrderHandler(ISapOrderService sapService)
    {
        _sapService = sapService;
    }

    public async Task<SapOrderDto> Handle(GetSapOrderQuery request, CancellationToken ct)
    {
        var sapResult = await _sapService.GetOrderFromSapAsync(request.SapOrderNumber);
        return sapResult.Adapt<SapOrderDto>();
    }
}
```

### Step 3 — Expose via `IXxxService` if the UI needs it

If the Blazor UI needs to trigger this, wire it up through the normal Web Repository → Web Handler pipeline.

---

## What NOT to Do

```csharp
// ❌ WRONG — Calling SAP directly from an Application Handler
public class SomeHandler : IRequestHandler<SomeCommand, SomeDto>
{
    private readonly HttpClient _httpClient; // Direct HTTP client, not through Integration.Sap!

    public async Task<SomeDto> Handle(SomeCommand request, CancellationToken ct)
    {
        var result = await _httpClient.GetAsync("https://sap.company.com/api/..."); // ❌
    }
}

// ❌ WRONG — Calling SAP from a Blazor component
@inject HttpClient Http

private async Task LoadSapData()
{
    var result = await Http.GetAsync("https://sap.company.com/api/..."); // ❌
}
```

---

## Why This Structure?

Keeping all SAP calls in one place means:

- If SAP's API changes, you only update one project
- All SAP error handling is centralized
- Easy to mock SAP in tests
- Clear visibility into what external calls the system makes

---

## Next Step

➡️ Read [09 — Code Conventions](./09-conventions.md) for naming rules and patterns to follow.
