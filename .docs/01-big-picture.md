# 01 — The Big Picture

[← Back to Index](../README.md)

---

## What Is VCCA?

**Vertical Cleaner Clean Architecture (VCCA)** is a custom software architecture pattern used across our projects. It is primarily based on **Clean Architecture** — which organizes code into layers with clear, single responsibilities — with structural ideas borrowed from **Vertical Slice Architecture** to keep feature code grouped and cohesive.

A VCCA project is built on **.NET 8 with Blazor Server**, connecting to a **Microsoft SQL Server** database, and optionally integrating with external systems like **SAP**.

---

## The Restaurant Analogy 🍽️

Think of the system like a restaurant:

| Restaurant Role | System Equivalent | What It Does |
|---|---|---|
| **Customer** | Browser / User | Makes a request |
| **Waiter** | Blazor UI (Presentation) | Takes the order, delivers the result |
| **Head Chef** | Application Layer | Decides *how* to fulfill the request |
| **Recipe Book** | Domain Layer | Defines *what the rules are* |
| **Kitchen Equipment & Suppliers** | Infrastructure | Actually stores and retrieves data |
| **Outside Vendor** | SAP Integration | External supplier of special ingredients |

The **chef doesn't go fetch groceries himself** — he tells the supplier what he needs. Similarly, our business logic never touches the database directly.

---

## Solution Map

```
your-project/
│
├── core/                          ← The brain of the system
│   ├── domain/
│   │   └── Domain.Entities        ← Business rules, data shapes
│   └── application/
│       ├── Application.DataTransferObjects   ← Data packets between layers
│       └── Application.UseCases              ← "What the system can do"
│
├── shared/                        ← Utilities used everywhere
│   └── Shared                     ← Helpers, extensions
│
└── src/                           ← Technical plumbing
    ├── infrastructure/
    │   └── database/
    │       ├── Database.Libraries  ← Abstract database interfaces
    │       └── Database.MsSql      ← Actual SQL Server code
    ├── integration/
    │   └── Integration.Sap         ← SAP HTTP client
    └── presentation/
        └── Web.BlazorServer        ← The website users see
```

---

## The Four Layers

```
┌─────────────────────────────────────────────┐
│              PRESENTATION                   │
│         (Web.BlazorServer)                  │
│     What the user sees and interacts with   │
├─────────────────────────────────────────────┤
│              APPLICATION                    │
│         (Application.UseCases)              │
│    Orchestrates "what needs to happen"      │
├─────────────────────────────────────────────┤
│                 DOMAIN                      │
│           (Domain.Entities)                 │
│      The core business rules and data       │
├─────────────────────────────────────────────┤
│             INFRASTRUCTURE                  │
│    (Database.MsSql + Integration.Sap)       │
│       The technical implementations         │
└─────────────────────────────────────────────┘
```

### The Golden Rule of Dependencies

**Inner layers never know about outer layers.**

- ✅ Presentation can use Application
- ✅ Application can use Domain
- ❌ Domain must NOT use Application or Infrastructure
- ❌ Application must NOT use Presentation

This is what lets us change the database engine or the UI framework without rewriting business logic.

---

## Key Libraries Used

| Library | Purpose | Used In |
|---|---|---|
| **MediatR** | Sends commands/queries between layers without tight coupling | Application, Web |
| **EF Core** | Talks to SQL Server | Database.MsSql |
| **Dapper** | Lightweight raw SQL queries | Database.MsSql |
| **Mapster** | Converts between entity types (e.g. Domain → DTO) | Application.UseCases |
| **Ardalis.Guards** | Validates inputs cleanly (e.g. `Guard.Against.Null(...)`) | Domain, Application |
| **Radzen Blazor** | Rich UI component library (grids, forms, dialogs, etc.) | Web.BlazorServer |
| **Blazor Server** | Web UI that runs server-side | Web.BlazorServer |
| **B1SLayer** | SAP HTTP integration client | Integration.Sap |

---

## Next Step

➡️ Read [02 — Layer Guide](./02-layers.md) to understand what each layer is responsible for.
