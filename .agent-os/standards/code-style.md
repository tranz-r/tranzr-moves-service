# Code Style Guide (.NET 9+ / C#)

## Context

Global code style rules for Agent OS projects targeting .NET 9+.

<conditional-block context-check="general-formatting">
IF this General Formatting section already read in current context:
  SKIP: Re-reading this section
  NOTE: "Using General Formatting rules already in context"
ELSE:
  READ: The following formatting rules
</conditional-block>

## General Formatting

### Indentation & Layout
- Use **4 spaces** for indentation (never tabs)
- **Allman braces** (opening brace on a new line) for types, methods, properties, and control blocks
- Prefer **file-scoped namespaces**: `namespace MyApp;`
- One type per file; filename matches the public type
- Keep lines to a reasonable length (~120 chars); wrap long expressions neatly
- Place `using` directives **outside** the namespace, sorted alphabetically, and remove unused usings
- Use **implicit usings** (SDK default) unless explicitly disabled

### Whitespace & Newlines
- One blank line between members for readability
- No trailing whitespace. End files with a newline
- Space after keywords: `if (condition)`, `for (...)`, not `if(condition)`

### Expressions & Language Features
- Prefer **expression-bodied members** for simple properties/forwarders
- Use **target-typed `new()`** where clear
- Use **nullable reference types** (`<Nullable>enable</Nullable>`) and handle null intentionally
- Prefer **`var`** when the type is obvious; otherwise use explicit type

## Naming Conventions

- **Namespaces, Classes, Structs, Records, Enums, Delegates, Methods, Properties, Events:** **PascalCase**  
  Example: `public sealed class PaymentProcessor`
- **Parameters, Local Variables:** **camelCase**  
  Example: `decimal amountTotal`
- **Private Fields:** **_camelCase** with leading underscore  
  Example: `private readonly ILogger _logger;`
- **Constants (`const`) and static readonly fields:** **PascalCase**  
  Example: `private const int MaxRetryCount = 3;`
- **Interfaces:** **PascalCase** with `I` prefix  
  Example: `public interface IPaymentService`
- **Enum Members:** **PascalCase**  
  Example: `Pending`, `Completed`, `Failed`
- **Generic Type Parameters:** **T**-prefixed PascalCase  
  Example: `where TRequest : IRequest`

> Rationale: Matches .NET design guidelines and Roslyn analyzer expectations.

## String Formatting

- Default to **double quotes** for strings: `"Hello World"`
- Prefer **interpolated strings** for composition: `$"User {userId} not found"`
- Use **verbatim** strings for paths/regex: `@"C:\data\inbox"`
- Use **raw string literals** (`""" ... """`) for multi-line content (JSON, SQL, templates)
- Avoid `string.Format` in new code; prefer interpolation or structured logging

## Code Comments & Documentation

- Use **XML doc comments** (`///`) on public APIs (classes, methods, public properties)
- Summarize behavior and note side effects/constraints
- For complex logic, add concise comments explaining the **why**, not the obvious **what**
- Keep comments up-to-date with code changes
- Use **TODO:**/**HACK:** tags sparingly, with owner/issue link if possible

## Error Handling

- Use exceptions for exceptional cases, not control flow
- Throw the **most specific** exception type
- Include **context-rich messages**; preserve inner exceptions  
  Example: `throw new InvalidOperationException("Unable to settle invoice", ex);`
- Avoid swallowing exceptions. When catching, either handle or rethrow with context

## Async & Concurrency

- Use **async/await** end-to-end; suffix async methods with **Async**: `GetQuoteAsync`
- Avoid `Task.Result`/`Wait()`. Prefer `await`
- For cancellation, **accept** `CancellationToken` parameters and pass them downstream

## Collections & LINQ

- Prefer **LINQ** for readability, but avoid excessive chaining in hot paths
- Choose appropriate types: `List<T>`, `IReadOnlyList<T>`, `IEnumerable<T>` as needed
- Do not mutate collections during `foreach`

## Immutability & Data Models

- Prefer **records** for immutable/value-like models:  
  `public record Address(string Street, string City);`
- For domain entities (EF Core), use classes with encapsulated invariants; guard state in constructors/setters

## Logging

- Use **structured logging** with message templates:  
  ```csharp
  _logger.LogWarning("Payment {PaymentId} failed with code {Code}", paymentId, code);
