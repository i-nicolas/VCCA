# Code Conventions

## Naming
| Type | Convention | Example |
|---|---|---|
| Service interface | `IXxxService` | `IUserService` |
| Service implementation | `XxxService` | `UserService` |
| Repository interface (entity-specific) | `IXxxReadRepository` | `IUserReadRepository` |
| Repository implementation (entity-specific) | `XxxReadRepository` | `UserReadRepository` |
| MediatR command | `XxxCmd` | `CreateRoleCmd` |
| MediatR query | `XxxQry` | `GetAllRolesQry` |
| MediatR handler | `XxxCmdHandler` / `XxxQryHandler` | `CreateRoleCmdHandler` |
| DTO | `XxxDTO` | `UserDTO` |
| Blazor page/component | `XxxPage` / `XxxComponent` | `UserListPage` |

## General Rules
- One class per file; filename must match class name
- Constructor injection — no service locator pattern
- `async/await` throughout — never `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()`
- Interfaces for all services and repositories
- No `dynamic` or anonymous types across layer boundaries
- No raw SQL with string interpolation — always parameterized
- No Domain entities across layer boundaries — always map to a DTO first
- No hardcoded secrets or connection strings anywhere in code or config

## Error Handling
- **Unexpected/system errors** → throw, let global middleware handle
- **Expected domain failures** → return `null`, `bool`, or a result wrapper — do not throw
