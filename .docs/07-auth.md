# 07 — Authentication & Authorization

[← Back to Index](../README.md)

---

## Overview

Authentication uses a **custom Microsoft Cookie Authentication** implementation — not JWT, not ASP.NET Core Identity. Everything related to auth lives in `Web.BlazorServer/Components/Security/`.

> 💡 Think of it like a wristband at an event. When you log in, you get a wristband (the cookie) stamped with everything about you (your roles and permissions). Every time you enter a room (page), the bouncer checks your wristband — no need to go back to the front desk each time.

---

## Authentication

### How Login Works

```
1. User submits credentials on the Blazor login page
        │
        ▼
2. login.js POSTs the credentials to AuthenticationController
        │
        ▼
3. AuthenticationController forwards to Application layer via MediatR
        │
        ▼
4. Application layer validates and returns a UserDTO
        │
        ▼
5. Controller builds a ClaimsPrincipal and issues an auth cookie
        │
        ▼
6. All subsequent requests are authenticated via that cookie
```

The cookie is a **plain claims cookie** — it uses `CookieAuthenticationDefaults.AuthenticationScheme` with a `ClaimsPrincipal` built from a `ClaimsIdentity`. No JWT token is involved.

### How Logout Works

JavaScript posts to the logout route on `AuthenticationController`, which clears the authentication cookie.

### Authentication Key Files

| File | Purpose |
|---|---|
| `AuthenticationController.cs` | POST login and logout routes — issues and clears the auth cookie |
| `AppAuthenticationService.cs` | Authentication service abstraction |
| `AppAuthenticationStateProvider.cs` | Custom Blazor `AuthenticationStateProvider` — provides auth state to components |
| `wwwroot/js/login.js` | JavaScript that POSTs credentials to the login controller route |

---

## Authorization

The system supports both **role-based** and **permission-based** authorization.

- Roles and permissions are stored in the database
- At login, they are loaded and written into the authentication cookie as **claims**
- All permission checks happen against those claims at runtime — **no database query per request**

### Authorization Key Files

| File | Purpose |
|---|---|
| `AuthorizationController.cs` | Handles permission/access checks |
| `AppAuthorizationAttribute.cs` | Custom `[Authorize]` attribute — accepts `action` and `resource`, generates a policy name |
| `AppPolicyProvider.cs` | Dynamic policy provider — builds policies at runtime from permission claims |
| `PermissionRequirement.cs` | Custom `IAuthorizationRequirement` representing a specific permission |

### `AppAuthorizationAttribute` — Permission-Based Access Control

This is the primary way to protect a page or action. It accepts an `action` and `resource` and constructs a policy name in the format:

```
permission.{RESOURCE}.{ACTION}
```

**Example:**

```csharp
[AppAuthorization("CREATE", "ORDERS")]
// Resolves to policy: "permission.ORDERS.CREATE"
```

### `AppPolicyProvider` — How Policies Are Evaluated

Policies are generated **dynamically at runtime** — nothing needs to be registered at startup.

```
[AppAuthorization("CREATE", "ORDERS")]
  → Policy name: "permission.ORDERS.CREATE"
    → AppPolicyProvider.GetPolicyAsync("permission.ORDERS.CREATE")
      → Builds AuthorizationPolicy with PermissionRequirement("permission.ORDERS.CREATE")
        → Evaluated against user's Permissions claim in the cookie
          → Access granted or denied
```

If the policy name doesn't start with `"permission"`, it falls back to the default ASP.NET Core policy provider.

---

## Claims

The following claims are written into the cookie at login:

| Claim | Value |
|---|---|
| `Id` | User's GUID |
| `Name` | User's full name |
| `RoleId` | User's role GUID |
| `Role` | User's role name |
| `Email` | User's email address |
| `Permissions` | Serialized string of all granted permissions |

> ⚠️ **Role and permission changes take effect at next login.** Claims are not refreshed mid-session.

---

## Securing Pages and Components

### Protecting an entire page

```razor
@page "/orders"
@layout ProtectedLayout
@inherits BaseComponent

<!-- ProtectedLayout enforces authentication for this page -->
```

### Permission-based protection (preferred)

```csharp
[AppAuthorization("VIEW", "ORDERS")]
public partial class OrderListPage : BaseComponent { ... }
```

### Role/permission checks in markup

```razor
<AuthorizeView Policy="permission.ORDERS.DELETE">
    <Authorized>
        <button>Delete Order</button>
    </Authorized>
    <NotAuthorized>
        <p>You don't have permission to delete orders.</p>
    </NotAuthorized>
</AuthorizeView>
```

### Reading auth state in code

```csharp
// In .razor.cs — use AppAuthenticationStateProvider, not the base class directly
[Inject] private AppAuthenticationStateProvider AuthStateProvider { get; set; }

protected override async Task OnInitializedAsync()
{
    var authState = await AuthStateProvider.GetAuthenticationStateAsync();
    var user = authState.User;
}
```

---

## Rules

- Never hardcode credentials, signing keys, or secrets anywhere in code or config files
- Do not re-implement cookie generation outside of `AuthenticationController`
- Use `AppAuthorizationAttribute` or `AuthorizeView` for UI-level access control — do not manually check claims in component logic
- Role and permission changes take effect at next login — claims are not refreshed mid-session
- Do not add authorization logic to the Application or Domain layers — it belongs in the Presentation layer

---

## Adding Auth to a New Feature

1. Apply `[AppAuthorization("ACTION", "RESOURCE")]` or `<AuthorizeView>` to the page/component
2. Ensure the required permission or role is seeded in the database
3. The claim will be available after the user's next login
4. Use `AppAuthenticationStateProvider` for reading auth state inside components if needed

---

## Next Step

➡️ Read [08 — SAP Integration](./08-sap.md) to learn how we communicate with SAP.
