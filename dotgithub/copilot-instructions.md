# Code Review Instructions

When I ask you to review code, review a diff, review a PR, or audit code, follow this structured approach.

## Workflow

1. **Triage**: Classify scope, risk level (Low/Medium/High), and which focus areas apply.
2. **Always check**: Correctness & Reliability.
3. **Check when relevant**: Performance, Security, Async/Threading, API Design, Error Handling.
4. **Report findings** using severity: 🔴 Critical, 🟡 Warning, 🔵 Info.
5. **Only flag issues that matter** — no style/formatting comments.
6. **Cite the specific code** for every finding.

## Output Format

```
## Triage
- **Scope**: [files/areas changed]
- **Risk**: [Low/Medium/High] — [reason]
- **Focus Areas**: [list]

## Findings

### 🔴 Critical
[findings or "None"]

### 🟡 Warning
[findings or "None"]

### 🔵 Info
[findings or "None"]

## Summary
- **Verdict**: [Approve / Request Changes / Needs Discussion]
- **Top Risks**: [1-3 sentence summary]
- **Confidence**: [X/10] — [what would increase it]
```

---

## Focus Area: Correctness & Reliability (always check)

Questions to ask:
- Does the code do what the author intended? Are all branches correct?
- Loop bounds correct (off-by-one, empty collections, negative counts)?
- Class/struct invariants maintained across all code paths, including exception paths?
- Edge cases: empty collections, null inputs, zero/negative values, overflow, boundary values?
- Return values checked? Conversions/casts safe? Data validated at trust boundaries?
- Does the change preserve existing behavior for unmodified paths?
- Do comments and PR description match actual behavior?
- If string comparision is used, or Dictionary key is string, is the comparison ordinal case sensitive or insensitive?
  Will case sensitive comparision cause unintended behavior?

Anti-patterns (C#/.NET):
- 🔴 `Enumerable.Range(0, n)` where n can be negative — throws, unlike `Parallel.For`
- 🔴 Modifying collection while iterating (`foreach` + `Remove`)
- 🔴 `Dictionary[key]` without `TryGetValue` guard
- 🔴 `.Single()`/`.First()` on potentially empty sequences
- 🔴 Disposing object still referenced elsewhere
- 🔴 Check-then-act without atomicity (`if (!dict.ContainsKey) dict.Add`)
- 🟡 String comparison without `StringComparison` parameter
- 🟡 Implicit narrowing conversions (`long`→`int`)
- 🟡 `DateTime.Now` vs `DateTime.UtcNow` inconsistency
- 🟡 `switch` without `default` case on non-exhaustive input
- 🟡 `Task.WhenAll` — only first exception surfaces unless you inspect `.Exception`
- 🟡 Feature flag defaults to `true` (new code ON at first deploy, no bake time)
- 🟡 Not all changes are protected by the feature flag, which complicates the incident mitigation if any issue is
  identified in production
- 🟡 Feature flag read at construction time only — requires restart to toggle

---

## Focus Area: Performance & Scalability

When to check: hot paths, loops, collections, DB queries, serialization, request critical path.

Questions to ask:
- Time complexity appropriate? Nested loops → O(n²)?
- Objects allocated in hot loops that could be pooled?
- Strings concatenated in loops? (Use `StringBuilder`)
- DB queries batched or N+1? HTTP calls inside loops?
- How does this behave at 10x, 100x load? Fan-out bounded?
- Timeouts on all external calls?

Anti-patterns (C#/.NET):
- 🔴 `new HttpClient()` per request — socket exhaustion
- 🔴 Create a new connection or client object per request — socket exhaustion
- 🔴 `GC.Collect()` called explicitly
- 🔴 Creating connections inside lock/semaphore — blocks all consumers
- 🟡 `list.Contains()` in loop — O(n²), use `HashSet`
- 🟡 `string + string` in loop — O(n²) allocations
- 🟡 Unbounded `Task.WhenAll` over large collection — memory spike, thread pool starvation
- 🟡 `Enumerable.Range().Select(async => ...)` without concurrency throttle
- 🟡 `Regex` compiled on every call in hot path — use `static readonly`
- 🟡 Closure capturing in LINQ lambda inside loop — allocation per iteration
- 🟡 `ConcurrentDictionary.GetOrAdd` with expensive factory — may execute multiple times
- 🔵 `ToList()` then `Count()` — just use `Count()`
- 🔵 `async` method that never awaits — unnecessary state machine

---

## Focus Area: Security

When to check: user input, auth, crypto, secrets, network boundaries, file I/O, config.

Questions to ask:
- All external input validated? Sanitized for injection?
- Auth checked on every entry point? Least privilege applied?
- Tokens validated (expiry, issuer, audience, signature)?
- Secrets in code, config, or logs?
- Deprecated crypto algorithms? Cryptographic RNG for security values?
- Error messages leaking internals?

Anti-patterns (C#/.NET):
- 🔴 Hardcoded password, API key, connection string, SAS token
- 🔴 String interpolation in SQL queries — injection
- 🔴 `Process.Start` with user-supplied arguments — command injection
- 🔴 `BinaryFormatter`/`XmlSerializer` on untrusted input — RCE
- 🔴 `Path.Combine(base, userInput)` without canonicalization — path traversal
- 🔴 Catching auth exceptions and continuing — bypass
- 🔴 Logging tokens/passwords/bearer headers
- 🔴 Disabling certificate validation
- 🔴 CORS `AllowAnyOrigin()` + `AllowCredentials()`
- 🔴 JWT validated without `exp`/`iss`/`aud`
- 🟡 Missing `[Authorize]` on controller action
- 🟡 Error response includes stack trace
- 🟡 `new Random()` for security values — use `RandomNumberGenerator`
- 🟡 Regex with user input without timeout — ReDoS
- 🟡 PII in plain text in logs/telemetry

---

## Focus Area: Async & Threading

When to check: async/await, Task, threads, locks, semaphores, concurrent collections, Parallel.*.

Questions to ask:
- `ConfigureAwait(false)` consistent in library code?
- Async methods properly awaited? No accidental fire-and-forget?
- `.Result`/`.Wait()`/`.GetAwaiter().GetResult()` — safe here? Deadlock risk?
- Mutable state shared across threads? Synchronized properly?
- `CancellationToken` threaded through full call chain?
- Fire-and-forget tasks — exceptions observed?
- `ValueTask` consumed exactly once?

Anti-patterns (C#/.NET):
- 🔴 `lock` held across `await`
- 🔴 `.Result`/`.Wait()` on UI/ASP.NET sync context — deadlock
- 🔴 `async void` (except event handlers)
- 🔴 Shared mutable field without synchronization
- 🔴 Double-awaiting `ValueTask`
- 🔴 `Parallel.ForEach` with async lambda — implicit `async void`
- 🔴 `Thread.Sleep` in async method — use `Task.Delay`
- 🔴 `Interlocked` mixed with non-atomic reads of same field
- 🟡 Fire-and-forget `Task` without exception handling
- 🟡 `SemaphoreSlim` without `try/finally` Release
- 🟡 `TaskCompletionSource` without `RunContinuationsAsynchronously`
- 🟡 `Task.WhenAny` without cancelling losing tasks
- 🟡 `Timer` callback without reentrancy guard
- 🟡 `GetAwaiter().GetResult()` wrapping async factory — blocks thread pool

---

## Focus Area: API Design

When to check: public/internal API changes, new interfaces, method signature changes, versioning.

Questions to ask:
- Backward-compatible? New params optional/defaulted?
- Async methods suffixed with `Async`? `CancellationToken` last param?
- Return types specific enough? Nullable documented?
- API surface minimal? Implementation details hidden?
- Can this evolve without breaking changes?

Anti-patterns (C#/.NET):
- 🔴 Removing/renaming public method — breaking change
- 🔴 Adding required parameter to existing method — breaking change
- 🔴 Returning `null` where caller expects non-null
- 🔴 Sealing previously inheritable class
- 🔴 Interface method added without default implementation
- 🟡 Accepting `List<T>` when `IEnumerable<T>` suffices
- 🟡 Exposing mutable internal collection via public property
- 🟡 Async method missing `Async` suffix
- 🟡 Public method returning `void` where `Task` enables future async
- 🟡 Default parameter value change — old callers keep old default
- 🟡 New constructor not chaining to old — divergent init logic
- 🟡 New optional param defaults to behavior-changing value
- 🔵 `bool` parameter that changes behavior — use enum
- 🔵 Missing XML docs on public API

---

## Focus Area: Error Handling

When to check: try/catch, exception propagation, retry logic, failure modes, resource cleanup.

Questions to ask:
- Specific exception types caught? Not bare `catch (Exception)`?
- Exceptions re-thrown with `throw;` not `throw ex;`?
- Resources cleaned up on failure? Partial state rolled back?
- Retry idempotent-safe? Bounded with backoff and jitter?
- Errors logged with correlation IDs? Sensitive data excluded?
- `IDisposable` in `using` blocks?

Anti-patterns (C#/.NET):
- 🔴 `catch (Exception) { }` — empty catch swallows everything
- 🟡 `catch (Exception ex) { throw ex; }` — loses stack trace
- 🟡 `catch` without logging or rethrowing — silent failure
- 🟡 Retry without backoff/max attempts — retry storm
- 🟡 Retry without jitter — thundering herd
- 🟡 Resource created but not disposed on exception path
- 🟡 `finally` block that can throw — masks original exception
- 🟡 Unobserved `Task` exception
- 🟡 `catch (Exception)` retrying non-transient errors
- 🟡 Batch operation: one item fails, successful items leak resources
- 🟡 Checkin faulted/closed connection without validation
- 🔵 `OperationCanceledException` logged as error — expected on cancellation
