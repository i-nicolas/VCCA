# Blazor Conventions

## BaseComponent
All components inherit `BaseComponent` (`/Components/Base/BaseComponent.razor`).

- Provides shared lifecycle methods, common DI, and helpers used across all components
- Every new `.razor` file must inherit `BaseComponent` — no exceptions
- Do not duplicate anything `BaseComponent` already provides in individual components


## Project Structure

```
Web.BlazorServer/
├── wwwroot/
│   ├── assets/                  # Static images and assets
│   └── js/
│       └── custom-scripts/      # Page-specific JS (login.js, logout.js)
├── Components/
│   ├── App.razor
│   ├── Routes.razor
│   ├── _Imports.razor
│   ├── Base/
│   │   ├── BaseComponent.razor         # Base class for ALL components
│   │   └── BaseForm.razor              # Base class for CVU (Create/View/Update) pages
│   ├── Layout/
│   │   └── ProtectedLayout.razor       # Authenticated layout wrapper
│   ├── Pages/
│   │   └── [Feature]/                  # Feature folders containing page components
│   │       ├── [Page].razor + .razor.cs
│   │       └── [Feature]CVU.razor.cs   # CVU form page (inherits BaseForm<TItem>)
│   ├── Security/                       # Auth controllers, services, policy providers
│   └── Shared/
│       ├── Abstraction/                # Reusable UI abstractions (AppDataGrid, AppBody, etc.)
│       ├── Skeletons/                  # Loading skeleton components
│       ├── Others/                     # Header, Footer, NavigationMenu
│       └── CascadingValues/            # HasUnsavedChangesProvider, LoadingScreenProvider
├── Defaults/
│   ├── AppActions.cs                   # Enum of all named UI actions (with [Description] labels)
│   └── AppActionOptionPresets.cs       # Preset factory for AppActionFactory options
├── Extensions/
│   └── CustomDialogOptionsExtensions.cs
├── Handlers/
│   ├── Repositories/
│   │   └── [Feature]/                  # Per-feature handler interfaces
│   │       └── IXxxHandler.cs          # Injected into components via @inject
│   └── Implementations/
│       └── [Feature]/                  # Web-layer MediatR dispatchers
│           └── [Feature]Handler.cs     # Calls IMediator.Send() directly
├── Helpers/
│   ├── AuthorizationHelper.cs          # Permission-check helpers (Can.Do(...))
│   ├── FilterValueTypeHelper.cs
│   └── PageActionHelper.cs
├── Registers/
│   └── BlazorServerDI.cs               # DI registration for web-layer services
├── Services/
│   ├── Repositories/                   # Web-layer service interfaces (IAlertService, IBusyService, IToastService, IAppActionFactory, etc.)
│   └── Implementation/                 # Concrete implementations (AlertService, BusyService, ToastService, AppActionFactory, UnsavedChangesService, grid services)
└── ViewModels/
    ├── Abstraction/                     # e.g. DataGridResultVM
    ├── Administration/
    │   ├── Role/                        # RoleVM, RolePermissionVM
    │   └── User/                        # UserVM, UserDataGridVM, UserPermissionVM, UserPasswordVM
    ├── Commons/                         # EntityVM, AuditableVM
    ├── Enums/                           # PageActionTypeEnum
    ├── Others/                          # AccountVM, EmailVM, PersonNameVM, etc.
    ├── Security/                        # AuthenticationVM
    └── System/                          # ModuleDataGridVM, NavigationRouteVM, etc.
```

## Forms & Validation
Custom form handling — no `EditForm` or `DataAnnotationsValidator`.

- All Create/View/Update (CVU) pages inherit `BaseForm<TItem>` (extends `BaseComponent`)
- `BaseForm<TItem>` binds to a `RadzenTemplateForm<TItem>` and manages form state, cloning, unsaved-change tracking, and the submit lifecycle
- List/read-only pages inherit `BaseComponent` directly
- Validation logic lives in the component's code-behind (`.razor.cs`)
- Do not use `EditForm` — always use `RadzenTemplateForm<TItem>` for form pages
- Full `BaseForm<TItem>` reference: see `ui_abstractions.md`


## Code-Behind Convention
Page components use partial class code-behind files:

```
OrderPage.razor       # Markup only
OrderPage.razor.cs    # Logic, lifecycle, DI injections
```

- Keep `.razor` files focused on markup
- All `@inject`, lifecycle methods, and event handlers go in `.razor.cs`

## Layout & Auth
- Protected pages use `ProtectedLayout.razor` as their layout
- Authentication handled via `AppAuthenticationService.cs` in `/Security/`
