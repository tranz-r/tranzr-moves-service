# Development Best Practices (.NET 9 / C#)

## Context

Global development guidelines for Agent OS projects targeting .NET 9 and C#.

<conditional-block context-check="core-principles">
IF this Core Principles section already read in current context:
  SKIP: Re-reading this section
  NOTE: "Using Core Principles already in context"
ELSE:
  READ: The following principles
</conditional-block>

## Core Principles

### Keep It Simple
- Implement code using the **simplest solution** that satisfies requirements
- Avoid unnecessary abstraction layers or generic over-engineering
- Prefer **clarity over cleverness**; code should be obvious to future maintainers
- Use built-in .NET APIs and language features before introducing custom helpers

### Optimize for Readability
- Prioritize **readability and maintainability** over premature optimization
- Choose meaningful names for classes, methods, and variables (`PaymentProcessor`, not `PP`)
- Write **self-documenting code**; comments should explain *why* something is done, not *what*
- Keep methods **short and focused** (preferably <50 lines, ideally doing one thing)

### DRY (Don't Repeat Yourself)
- Extract repeated business logic into **private methods** or **domain services**
- Centralize common infrastructure logic in **utility classes** or **extension methods**
- Use **C# 12 features** (like primary constructors and collection expressions) to reduce boilerplate
- Reuse **DTOs and mappers** consistently (e.g., via Mapperly or AutoMapper if adopted)

### File & Project Structure
- Keep files/classes focused on a **single responsibility**
- Group related code into **namespaces matching project layers** (e.g., `AgentOs.Domain`, `AgentOs.Infrastructure`)
- Apply **Clean Architecture layering**:
  - `Domain` → business rules, entities, value objects
  - `Application` → use cases, commands/queries, interfaces
  - `Infrastructure` → EF Core, external services
  - `API` → controllers, minimal APIs, DTOs
- Use consistent naming conventions across all layers (PascalCase types, camelCase locals, `_field` for private fields)
- Avoid "God classes" or dumping unrelated code into a single module

---

<conditional-block context-check="dependencies" task-condition="choosing-external-library">
IF current task involves choosing an external library:
  IF Dependencies section already read in current context:
    SKIP: Re-reading this section
    NOTE: "Using Dependencies guidelines already in context"
  ELSE:
    READ: The following guidelines
ELSE:
  SKIP: Dependencies section not relevant to current task
</conditional-block>

## Dependencies

### Choose Libraries Wisely
When adding third-party dependencies:
- Prefer **official Microsoft or .NET Foundation libraries** if available (e.g., EF Core, System.Text.Json)
- Ensure the library is:
  - Actively maintained (recent commits within 6 months)
  - Widely adopted and trusted (downloads, GitHub stars, community usage)
  - Well-documented, with clear API reference and examples
- Check open issues/PRs to gauge responsiveness of maintainers
- Avoid **heavy frameworks** for simple tasks; use lightweight, purpose-fit packages
- Evaluate licensing (MIT, Apache-2.0 preferred; avoid GPL unless required)
- Introduce a dependency only if it solves a problem better than the native framework
- Review security advisories (GitHub Dependabot, osv.dev)

---
