# Authentication & Authorization

## Location
All authentication and authorization logic lives in `Web.BlazorServer/Components/Security/`.


## Authentication

### Mechanism
Custom implementation of **Microsoft Cookie Authentication** (not JWT, not ASP.NET Core Identity).

### Login Flow
1. User submits credentials via the Blazor login page
2. A JavaScript file (`wwwroot/js/custom-scripts/login.js`) POSTs the login payload to `AuthenticationController`
3. `AuthenticationController` forwards credentials to the Application layer via MediatR
4. If the Application layer returns a valid `UserDTO`, the controller creates an **authentication cookie** populated with the user's claims (roles, permissions, and user info)
5. Subsequent requests are authenticated via this cookie

### Logout Flow
- Handled via `AuthenticationController` — JavaScript sends a GET request to the logout route, which clears the authentication cookie

### Key Files
| File | Purpose |
|---|---|
| `AuthenticationController.cs` | POST login route and GET logout route — issues and clears the auth cookie |
| `AppAuthenticationService.cs` | Authentication service abstraction |
| `AppAuthenticationStateProvider.cs` | Custom Blazor `AuthenticationStateProvider` — provides auth state to Blazor components |
| `wwwroot/js/custom-scripts/login.js` | JavaScript that POSTs credentials to the login controller route |
| `wwwroot/js/custom-scripts/logout.js` | JavaScript that sends a GET request to the logout controller route |

> The cookie is a **plain claims cookie** — no JWT token is involved. It uses `CookieAuthenticationDefaults.AuthenticationScheme` with a `ClaimsPrincipal` built from a `ClaimsIdentity`.


## Authorization

### Mechanism
Both **role-based** and **permission-based** authorization.

- Roles and permissions are stored in the database
- At login, roles and permissions are loaded and written into the authentication cookie as **claims**
- `AppPolicyProvider` and `PermissionRequirement` power the permission-based policy system

### Key Files
| File | Purpose |
|---|---|
| `AuthorizationController.cs` | Evaluates whether the user's Permissions claim satisfies a required permission policy |
| `AppAuthorizationAttribute.cs` | Custom `[Authorize]` attribute — accepts `action` and `resource`, generates a policy name |
| `AppPolicyProvider.cs` | Dynamic policy provider — builds policies at runtime from permission claims |
| `PermissionRequirement.cs` | Custom `IAuthorizationRequirement` representing a specific permission policy |

### `AppAuthorizationAttribute`
Inherits `AuthorizeAttribute`. Accepts an `action` and `resource` and constructs a policy name:
```
permission.{RESOURCE}.{ACTION}
```
Example:
```csharp
[AppAuthorization("CREATE", "ORDERS")]
// Resolves to policy: "permission.ORDERS.CREATE"
```

### `AppPolicyProvider`
Implements `IAuthorizationPolicyProvider`. On each policy evaluation, dynamically builds an `AuthorizationPolicy` containing a `PermissionRequirement` for any policy name starting with `"permission"` — no static registration needed. Fallbacks to `DefaultAuthorizationPolicyProvider` for others.


## Claims
User roles and permissions are loaded from the DB at login and stored as claims in the authentication cookie.
Permission checks are claim-based at runtime — no DB query is needed per request.

The following claims are written to the cookie at login:

| Claim | Value |
|---|---|
| `Id` | User's GUID |
| `Name` | User's full name |
| `RoleId` | User's role GUID |
| `Role` | User's role name |
| `Email` | User's email address |
| `Permissions` | JSON-serialized array of permission strings (e.g. `["ORDERS.CREATE", "USERS.VIEW"]`) |


## Rules
- Never hardcode credentials, signing keys, or secrets anywhere in code or config
- Do not re-implement token/cookie generation outside of `AuthenticationController`
- Use `AppAuthorizationAttribute` or `AuthorizeView` for UI-level access control — do not manually check claims in component logic
- Role and permission changes take effect at next login — claims are not refreshed mid-session
- Do not add authorization logic to the Application or Domain layers — it belongs in the Presentation layer


## Adding Auth to a New Feature
1. Apply `AppAuthorizationAttribute` or `AuthorizeView` to the page/component
2. Ensure the required permission or role is seeded in the DB
3. The claim will be available after the user's next login
4. Use `AppAuthenticationStateProvider` for reading auth state inside components if needed
