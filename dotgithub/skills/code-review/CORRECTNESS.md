# Correctness & Reliability

> **Always loaded.** This is the baseline module for every review.

## Core Questions

### Logic
- Does the code do what the author intended?
- Are all branches reachable and correct?
- Are loop bounds correct (off-by-one, empty collections, negative counts)?
- Are boolean conditions correct (De Morgan's, short-circuit evaluation)?

### State & Invariants
- Are class/struct invariants maintained across all code paths?
- Can fields be left in an inconsistent state after an exception?
- Are null checks present where needed? Are they absent where redundant?
- Is mutable state modified safely (no aliasing surprises)?

### Edge Cases
- Empty collections, null inputs, zero/negative values
- Integer overflow/underflow
- Concurrent modification during iteration
- First call vs subsequent calls (initialization)
- Boundary values (min, max, exactly-at-limit)

### Data Flow
- Is every variable initialized before use?
- Are return values checked (especially for APIs that return error codes or null)?
- Is data validated at trust boundaries?
- Are conversions/casts safe (narrowing, lossy)?

### Behavioral Correctness
- Does the change preserve existing behavior for unmodified paths?
- If behavior changes intentionally, is it documented?
- Are backward compatibility guarantees maintained?
- Do comments and names match actual behavior?

## Anti-Patterns to Flag

### General C#/.NET

| Pattern | Risk |
|---------|------|
| `Enumerable.Range(0, n)` where `n` can be negative | 🔴 `ArgumentOutOfRangeException` — `Parallel.For` handles negative gracefully, `Enumerable.Range` does not |
| Modifying a collection while iterating (`foreach` + `Remove`) | 🔴 `InvalidOperationException` or silent corruption |
| `==` on floating point values | 🟡 Precision issues — use tolerance-based comparison |
| String comparison without `StringComparison` parameter | 🟡 Culture-dependent bugs in Turkish-I, etc. |
| Disposing an object that's still referenced elsewhere | 🔴 `ObjectDisposedException` in other consumers |
| Implicit narrowing conversions (`long` → `int`, `double` → `float`) | 🟡 Silent data truncation |
| `default` on non-nullable reference type without null check downstream | 🟡 Deferred `NullReferenceException` |
| Checked cast (`(int)longValue`) without range validation | 🔴 `OverflowException` or silent truncation in unchecked context |
| `Dictionary[key]` without `TryGetValue` or `ContainsKey` guard | 🔴 `KeyNotFoundException` |
| `Substring(index)` without bounds check | 🔴 `ArgumentOutOfRangeException` |
| Enum parsed from external input without `Enum.TryParse` | 🟡 Unvalidated cast, unexpected values |
| LINQ `.Single()` / `.First()` on potentially empty or multi-element sequences | 🔴 `InvalidOperationException` |
| `DateTime.Now` vs `DateTime.UtcNow` inconsistency | 🟡 Time zone bugs, daylight saving errors |
| `readonly` field holding mutable reference type (false safety) | 🟡 Mutation through reference still possible |
| `switch` without `default` case on non-exhaustive input | 🟡 Silent no-op on unexpected values |
| Struct implementing `IDisposable` (boxing on `using`) | 🟡 Dispose may not run as expected |
| `Task.WhenAll` — only first exception surfaces unless you inspect `.Exception` | 🟡 Lost exceptions |
| Check-then-act without atomicity (`if (!dict.ContainsKey) dict.Add`) | 🔴 Race condition / duplicate key |

## Output Template

```
### Correctness
- **Logic**: [findings]
- **Edge Cases**: [findings]
- **Invariants**: [findings]
- **Behavioral Change**: [intentional/unintentional/none]
```
