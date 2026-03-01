# Code Review Skill

Formal, structured code review with dedicated modules for performance, security, correctness, async/threading, API
design, and error handling.

## Quick Start

```bash
# Review uncommitted changes
@code-review review my git diff

# Review a specific file
@code-review review src/MyService.cs

# Focused review on a specific concern
@code-review check this for thread safety
@code-review audit security of this endpoint

# Review staged changes
@code-review review my staged changes
```

## Modules

| Module | Focus Area | When to Load |
|--------|-----------|--------------|
| **CORRECTNESS.md** | Logic bugs, edge cases, invariants | Always (every review) |
| **PERFORMANCE.md** | Allocations, hot paths, scalability, O(n) analysis | Perf-sensitive code, collections, loops, DB queries |
| **SECURITY.md** | Input validation, auth, secrets, injection, crypto | Network boundaries, user input, auth code, config |
| **ASYNC-THREADING.md** | Async/await, threading, concurrency, locks | Any async code, shared state, parallelism |
| **API-DESIGN.md** | Public contracts, breaking changes, usability | Public/internal APIs, interface changes, versioning |
| **ERROR-HANDLING.md** | Exception strategy, failure modes, resilience | Try/catch blocks, error propagation, retry logic |

## Workflow

```
Every Review:    Triage → CORRECTNESS
+ Performance:   Triage → CORRECTNESS → PERFORMANCE
+ Security:      Triage → CORRECTNESS → SECURITY
+ Concurrency:   Triage → CORRECTNESS → ASYNC-THREADING
+ API Changes:   Triage → CORRECTNESS → API-DESIGN
+ Error Paths:   Triage → CORRECTNESS → ERROR-HANDLING
Full Review:     Triage → CORRECTNESS → [all applicable modules]
```

## Customization

Each module is a standalone markdown file. To customize for your project:
- Add project-specific anti-patterns to any module
- Create new modules (e.g., `DATABASE.md`, `KUBERNETES.md`)
- Adjust severity thresholds in the agent routing table
- Add domain-specific checklists (see the `lens` skill in NRP repo for examples)

## Philosophy

1. **Signal over noise** — only flag issues that matter. Never comment on formatting or style.
2. **Evidence-based** — every finding must cite the specific code that triggers it.
3. **Actionable** — every finding includes what to do about it.
4. **Composable** — modules can be mixed and matched per review.
