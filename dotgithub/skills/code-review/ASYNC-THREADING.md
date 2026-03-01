# Async & Threading

> **Load when**: Code uses async/await, Task, threads, locks, semaphores, concurrent
> collections, Parallel.*, or shared mutable state.

## Core Questions

### Async/Await Correctness
- Is `ConfigureAwait(false)` used consistently in library/non-UI code?
- Are async methods properly awaited (no fire-and-forget without explicit intent)?
- Is `Task.Result` or `.GetAwaiter().GetResult()` used? Is it safe in this context?
- Are `ValueTask` instances consumed exactly once (no double-await)?
- Is `Task.Run` used appropriately (not wrapping already-async methods)?

### Sync-over-Async / Async-over-Sync
- Is synchronous code blocking on async results (deadlock risk)?
- Is `Task.Run` used to offload synchronous work to the thread pool?
- Is the sync path truly synchronous, or does it call async methods internally?
- Are sync wrappers around async methods documented with rationale?

### Concurrency & Shared State
- Is mutable state shared across threads/tasks?
- Are locks held across await points (illegal with `lock`, valid with `SemaphoreSlim`)?
- Are concurrent collections used where needed (`ConcurrentDictionary`, `ConcurrentBag`)?
- Is there a risk of deadlock (lock ordering, nested locks)?
- Are race conditions possible between check-and-act sequences?

### Cancellation
- Is `CancellationToken` threaded through the full call chain?
- Are long-running operations cancellable?
- Is `OperationCanceledException` handled appropriately (not swallowed or treated as error)?
- Are cancellation tokens checked at appropriate intervals in loops?

### Task Lifecycle
- Are fire-and-forget tasks observed for exceptions (unobserved task exceptions crash the process)?
- Are tasks properly disposed when implementing `IDisposable`?
- Is `Task.WhenAll` exception handling correct (only first exception surfaces by default)?
- Are background tasks tracked and cleaned up on shutdown?

## Anti-Patterns to Flag

### General C#/.NET

| Pattern | Risk |
|---------|------|
| `lock` held across `await` | 🔴 Compile error or undefined behavior |
| `.Result` or `.Wait()` on UI/ASP.NET sync context | 🔴 Deadlock — sync context can't re-enter |
| `async void` (except event handlers) | 🔴 Unobserved exceptions, caller cannot await |
| Fire-and-forget `Task` without exception handling | 🟡 Silent failures, unobserved `TaskException` |
| `Task.Run` wrapping an already-async method | 🟡 Unnecessary thread pool hop |
| Shared mutable field without synchronization | 🔴 Race condition — data corruption |
| `SemaphoreSlim` without `try/finally` Release | 🟡 Semaphore leak on exception — deadlocks callers |
| Double-awaiting a `ValueTask` | 🔴 Undefined behavior — consume exactly once |
| `CancellationToken.None` passed to cancellable external API | 🔵 Missing cancellation support — can't abort |
| `Interlocked` mixed with non-atomic reads of same field | 🔴 Torn reads on 64-bit fields on 32-bit runtime |
| `async Task` method that returns `Task.CompletedTask` in all paths | 🔵 Unnecessary state machine overhead |
| `TaskCompletionSource` created without `TaskCreationOptions.RunContinuationsAsynchronously` | 🟡 Inline continuation can deadlock or starve caller |
| `Channel<T>` read without respecting `ChannelClosedException` | 🟡 Unhandled close |
| `Parallel.ForEach` with async lambda (uses `async void` implicitly) | 🔴 Fire-and-forget, no exception propagation — use `Parallel.ForEachAsync` |
| `Thread.Sleep` in async method | 🔴 Blocks thread pool thread — use `Task.Delay` |
| `ConcurrentDictionary.AddOrUpdate` with side-effect in value factory | 🟡 Factory may execute multiple times |
| `ReaderWriterLockSlim` without `try/finally` pattern | 🟡 Lock leak on exception |
| `Timer` callback that doesn't guard against reentrancy | 🟡 Overlapping executions on slow callbacks |
| `Lazy<T>` with `LazyThreadSafetyMode.None` in multi-threaded context | 🔴 Race: multiple initializations, torn reads |
| `ManualResetEvent` / `AutoResetEvent` in async code (blocks thread) | 🟡 Use `SemaphoreSlim` or `ManualResetEventSlim` instead |
| `await` inside `catch` / `finally` on older .NET (< 6.0) | 🟡 Behavior varies by framework version |
| `Task.WhenAny` without cancelling losing tasks | 🟡 Leaked tasks continue running, consuming resources |

## Output Template

```
### Async & Threading
- **Async Patterns**: [findings]
- **Concurrency Safety**: [findings]
- **Cancellation**: [findings]
- **Task Lifecycle**: [findings]
```
