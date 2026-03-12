# 99 — Known Architectural Debts

[← Back to Index](../README.md)

---

> ⚠️ **Important:** These are known issues in the codebase. **Do not fix any of these without explicit instruction from a senior engineer or tech lead.** They are documented so you're aware of them, not so you can fix them.

---

## What Is Technical Debt?

Technical debt is like taking a shortcut when building a house. It gets you there faster, but eventually you'll need to come back and do it properly. These items are known shortcuts or inconsistencies in the codebase.

---

## 1. Domain References MediatR

**Where:** `core/domain/Domain.Entities`

**The issue:** The Domain layer imports MediatR just to use `INotification` for domain events. This couples the "pure" business layer to an external library.

**Impact on you:** You can still use domain events normally — this doesn't break anything. Just know it's not architecturally ideal.

**Correct fix (when instructed):** Create a local `IDomainEvent` interface in Domain, then adapt it to MediatR's `INotification` in the Application layer.

---

## 2. Inconsistent Aggregate Root Enforcement

**Where:** `core/domain/Domain.Entities`

**The issue:** In proper DDD, only the "aggregate root" entity should be modified directly. Child entities should only change through the root. In our codebase, this isn't always enforced — some child entities can be modified directly from outside.

**Impact on you:** Before writing a handler that touches child entities, **check how that specific aggregate is structured**. Don't assume the rules are enforced just because they should be.

---

## 3. Both Generic and Specific Repositories Coexist

**Where:** `src/infrastructure/database/Database.MsSql`

**The issue:** We have both `IRepository<T>` (generic) and `IXxxRepository` (entity-specific) in use, with no strict rule on which to prefer.

**Current rule:** Use specific repositories for complex queries, generic for simple CRUD. Don't try to consolidate them.

---

## 4. Mixed Error Handling Strategies

**Where:** `core/application/Application.UseCases`

**The issue:** Some existing handlers throw exceptions for expected failures (like "record not found"), while others return null or a result wrapper. There's no consistent approach in the existing code.

**Current rule for new code:** Expected failures → return a value (`null`, `bool`, result wrapper). Unexpected/system errors → throw. Apply this going forward. Don't retroactively fix old handlers.

---

## 5. Web/Handlers and Web/Repositories Live in the Presentation Layer

**Where:** `src/presentation/Web.BlazorServer/Handlers/`, `Web.BlazorServer/Repositories/`

**The issue:** From a strict Clean Architecture perspective, MediatR dispatch coordination arguably belongs in the Application layer. Here, it lives in the Web (Presentation) layer.

**This is intentional.** It's how this codebase works (called "Pipeline A"). All new features follow this pattern. Do not move these to the Application layer.

---

## Summary Table

| # | Issue | Affect Your Work? | Action |
|---|---|---|---|
| 1 | Domain → MediatR coupling | Rarely | Use domain events normally |
| 2 | Inconsistent aggregate roots | ⚠️ Sometimes | Check per-aggregate before writing |
| 3 | Mixed repository types | Minor | Follow current rules |
| 4 | Mixed error handling | Minor | Follow rules for new code only |
| 5 | Web layer dispatch coordination | None | Follow Pipeline A |

---

[← Back to Index](../README.md)
