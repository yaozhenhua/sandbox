# Error Handling

> **Load when**: Code contains try/catch blocks, throws exceptions, propagates errors,
> implements retry logic, or handles failure modes.

## Core Questions

### Exception Strategy
- Are exceptions used for exceptional conditions (not control flow)?
- Are specific exception types caught (not bare `catch (Exception)`)?
- Are exceptions re-thrown correctly (`throw;` not `throw ex;`)?
- Are custom exceptions used where standard ones don't convey enough context?

### Error Propagation
- Do errors propagate to a level where they can be meaningfully handled?
- Are errors wrapped with context when crossing abstraction boundaries?
- Is the original exception preserved as `InnerException`?
- Are error details sufficient for debugging (but not leaking to users)?

### Failure Modes
- What happens when this code fails? Is the failure graceful?
- Are resources cleaned up on failure (using/finally/Dispose)?
- Is partial state rolled back on failure?
- Can this failure cascade to other components?

### Retry & Resilience
- Is retry logic idempotent-safe?
- Are retries bounded (max attempts, backoff)?
- Is jitter applied to prevent thundering herd?
- Are transient vs permanent failures distinguished?
- Are circuit breakers considered for repeated failures?

### Logging & Observability
- Are errors logged with sufficient context (correlation IDs, parameters)?
- Are expected errors logged at appropriate levels (Warning vs Error)?
- Is sensitive data excluded from error logs?
- Are metrics/alerts configured for critical failure paths?

### Dispose & Cleanup
- Are `IDisposable` resources in `using` blocks or try/finally?
- Is cleanup order correct (reverse of creation)?
- Are partial-creation scenarios handled (object A created, object B fails)?
- Are finalizers/destructors needed? (Usually not — prefer `Dispose`.)

## Anti-Patterns to Flag

### General C#/.NET

| Pattern | Risk |
|---------|------|
| `catch (Exception) { }` — empty catch | 🔴 Swallowed errors hide bugs completely |
| `catch (Exception ex) { throw ex; }` — rethrow loses stack trace | 🟡 Debugging nightmare — use `throw;` |
| `catch` without logging or rethrowing | 🟡 Silent failure — error disappears |
| Retry without backoff or max attempts | 🟡 Amplifies outages — retry storm |
| Retry without jitter | 🟡 Thundering herd on shared dependency |
| Resource created but not disposed on exception path | 🟡 Resource leak (connections, handles, streams) |
| Returning error codes instead of exceptions in C# | 🔵 Inconsistent with .NET idioms |
| Logging full exception object in user-facing HTTP response | 🟡 Information disclosure |
| `finally` block that can throw (masks original exception) | 🟡 Original error lost |
| Unobserved `Task` exception | 🟡 Crashes process in .NET < 4.5, silent loss in >= 4.5 |
| `catch (Exception)` when only specific types are transient | 🟡 Retrying non-transient errors (auth failures, bad requests) |
| `AggregateException` not unwrapped — caller sees wrapper instead of cause | 🟡 Confusing stack traces |
| `ExceptionDispatchInfo.Capture` not used when rethrowing across async boundary | 🔵 Stack trace incomplete |
| Catch block modifies shared state without considering partial failure | 🟡 Inconsistent state after error |
| `using` on `null`-possible reference without null check | 🟡 `NullReferenceException` in cleanup path |
| `Dispose()` called explicitly AND via `using` (double-dispose) | 🟡 `ObjectDisposedException` if not idempotent |
| `OperationCanceledException` caught and logged as error (it's expected on cancellation) | 🔵 Log noise — log at Debug/Info level |
| `TimeoutException` not distinguished from other failures in retry logic | 🟡 Timeout should not count same as bad request |
| Error message constructed with `string.Format` using user input without sanitization | 🟡 Log injection |
| Missing correlation ID / request ID in error log | 🔵 Cannot trace error to originating request |

## Output Template

```
### Error Handling
- **Exception Strategy**: [findings]
- **Failure Modes**: [findings]
- **Resource Cleanup**: [findings]
- **Resilience**: [findings]
```
