# API Design

> **Load when**: Code changes public or internal APIs, adds new interfaces, modifies
> method signatures, changes contracts, or affects versioning.

## Core Questions

### Contract Stability
- Are public API signatures changed in a backward-compatible way?
- Are new parameters optional/defaulted to preserve existing callers?
- Is method overloading used instead of breaking signature changes?
- Are obsolete members marked with `[Obsolete]` and migration path documented?

### Naming & Discoverability
- Do method names clearly convey what they do (verb + noun)?
- Are async methods suffixed with `Async`?
- Are boolean parameters replaced with enums for clarity?
- Are parameter names descriptive (not `p1`, `str`, `flag`)?

### Parameter Design
- Are parameters ordered logically (most important first, options last)?
- Are nullable parameters clearly documented?
- Is `CancellationToken` the last parameter on async methods?
- Are collections accepted as `IEnumerable<T>` / `IReadOnlyList<T>` (not concrete types)?

### Return Types
- Are return types appropriately specific (not `object` or `dynamic`)?
- Are nullable return values documented with `[return: MaybeNull]` or `?`?
- Is `Task<T>` vs `ValueTask<T>` chosen appropriately?
- Are error results returned via exceptions vs result types consistently?

### Abstraction & Encapsulation
- Is the API surface minimal (no unnecessary public members)?
- Are implementation details hidden behind interfaces?
- Is the dependency direction correct (abstractions don't depend on details)?
- Are factory methods or builders used where constructors would be complex?

### Versioning & Evolution
- Can this API evolve without breaking changes?
- Are options/configuration objects used for future extensibility?
- Is the feature flagged for safe rollout?
- Is the API documented (XML docs for public members)?

## Anti-Patterns to Flag

### General C#/.NET

| Pattern | Risk |
|---------|------|
| Removing or renaming a public method | 🔴 Breaking change for all callers |
| Adding required parameter to existing public method | 🔴 Breaking change — use overload or optional parameter |
| Returning `null` where caller expects non-null (no `?` annotation) | 🔴 `NullReferenceException` in caller |
| Accepting `List<T>` when `IEnumerable<T>` or `IReadOnlyList<T>` suffices | 🟡 Over-constraining callers |
| `bool` parameter that changes behavior (use enum or separate methods) | 🔵 Readability — callers see `true`/`false` with no context |
| Missing XML docs on public API | 🔵 Discoverability — IntelliSense shows nothing |
| Exposing mutable internal collection via public property (no defensive copy) | 🟡 Encapsulation leak — callers can corrupt state |
| Constructor with >5 parameters (use builder, options object, or factory) | 🔵 Usability — hard to remember parameter order |
| `out` parameter on async method (not supported — design issue) | 🔴 Compile error or forced sync wrapper |
| Async method missing `Async` suffix | 🟡 Convention violation — callers don't know it's async |
| Sync method named with `Async` suffix | 🟡 Misleading — callers expect `Task` return |
| `CancellationToken` not as last parameter | 🔵 Convention violation |
| Public method returning `void` where `Task` enables async evolution | 🟡 Locks API into sync-only forever |
| Changing `IEnumerable<T>` return to `List<T>` (or vice versa) | 🟡 Behavioral contract change (lazy → eager) |
| Throwing from property getter (properties should be lightweight) | 🟡 Violates caller expectations — use method instead |
| `static` mutable state on public API class | 🔴 Thread-unsafe singleton pattern |
| Sealing a class that was previously inheritable | 🔴 Breaking change for derived types |
| Default parameter value change on public method | 🟡 Recompile-dependent — old callers keep old default |
| Interface method added without default implementation (.NET 8+) | 🔴 Breaking change for all implementors |

### Constructor Chaining & Backward Compatibility

| Pattern | Risk |
|---------|------|
| New constructor that doesn't chain to old (duplicated init logic) | 🟡 Divergent behavior between old and new callers |
| Old constructor chains to new with `null` for new param — correct pattern | 🟢 Zero behavioral change for existing callers |
| New optional parameter defaults to behavior-changing value | 🟡 Existing callers silently get new behavior on recompile |

## Output Template

```
### API Design
- **Contract**: [backward compatible? breaking changes?]
- **Usability**: [naming, parameters, return types]
- **Extensibility**: [can this evolve safely?]
```
