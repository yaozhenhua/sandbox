# Performance & Scalability

> **Load when**: Code touches hot paths, loops, collections, DB queries, serialization,
> allocation-heavy patterns, or anything on the request critical path.

## Core Questions

### Algorithmic Complexity
- What is the time complexity of the changed code? Is it appropriate?
- Are there nested loops over collections that could be O(n²) or worse?
- Could a different data structure reduce complexity (e.g., HashSet vs List for lookups)?
- Are LINQ chains creating unnecessary intermediate allocations?

### Allocations & GC Pressure
- Are objects allocated in hot loops that could be pooled or reused?
- Are strings concatenated in loops (use StringBuilder)?
- Are closures capturing variables unnecessarily (lambda allocations)?
- Are `Task`/`ValueTask` choices appropriate?
- Are arrays/lists pre-sized when the count is known?

### I/O & Network
- Are database queries batched or N+1?
- Is data fetched eagerly when lazy loading would suffice (or vice versa)?
- Are HTTP calls made inside loops?
- Is caching used where appropriate? Is cache invalidation correct?
- Are connections pooled and reused?

### Scalability
- How does this behave at 10x, 100x current load?
- Are there single-writer bottlenecks (locks, semaphores)?
- Is fan-out bounded? Could a single request trigger unbounded work?
- Are timeouts set on all external calls?

### Memory
- Are large objects (>85KB) allocated that could fragment the LOH?
- Are streams disposed properly?
- Are event handlers unsubscribed to prevent leaks?
- Is `IDisposable` implemented where needed?

## Anti-Patterns to Flag

### General C#/.NET

| Pattern | Risk |
|---------|------|
| `list.Contains()` in a loop (should be `HashSet`) | 🟡 O(n²) time complexity |
| `string + string` in a loop | 🟡 O(n²) allocations — use `StringBuilder` |
| `ToList()` followed by `Count()` | 🔵 Unnecessary allocation — just use `Count()` |
| LINQ `.Where().First()` | 🔵 Minor — use `.First(predicate)` |
| Unbounded `Task.WhenAll` over large collection | 🟡 Memory spike, thread pool starvation |
| `new HttpClient()` per request | 🔴 Socket exhaustion — use `IHttpClientFactory` |
| Missing `ConfigureAwait(false)` in library code | 🔵 Potential deadlock in sync-over-async callers |
| `SELECT *` or missing pagination | 🟡 Unbounded data fetch |
| `ToList()` materializing full query when only iterating once | 🟡 Unnecessary memory — use `IEnumerable` |
| `.OrderBy().First()` instead of `.MinBy()` | 🔵 O(n log n) vs O(n) |
| `Regex` compiled on every call in hot path | 🟡 Compile overhead — use `static readonly Regex` or source generator |
| Boxing value types via interface dispatch (`IComparable` on struct) | 🟡 Allocation per call in hot loop |
| `params T[]` in frequently called method | 🟡 Array allocation per call — consider overloads |
| Capturing outer variable in LINQ lambda inside loop (closure allocation) | 🟡 Allocation per iteration |
| `Task.Delay` in tight retry loop without backoff | 🟡 Thread pool pressure |
| `Enumerable.Range().Select(async => ...)` without concurrency throttle | 🟡 Unbounded parallelism |
| `ConcurrentDictionary.GetOrAdd` with expensive factory called multiple times | 🟡 Factory not guaranteed single-execution |
| `GC.Collect()` called explicitly | 🔴 Pauses all threads — almost never correct |
| `dynamic` keyword in hot path | 🟡 DLR overhead, no compile-time optimization |
| `async` method that never `await`s (returns synchronously) | 🔵 Unnecessary state machine — return `Task.FromResult` |

## Output Template

```
### Performance
- **Complexity**: [O(?) analysis]
- **Allocations**: [findings]
- **I/O**: [findings]
- **Scalability**: [how it behaves under load]
```
