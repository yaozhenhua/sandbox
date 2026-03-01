# Personal GitHub Agent

## Code Review

### Structure

```
   ~/.github/
   ├── agents/
   │   └── code-review.agent.md          ← Agent entry point (routing, output format)
   └── skills/
       └── code-review/
           ├── SKILL.md                   ← Overview, quick start, workflows
           ├── CORRECTNESS.md             ← Logic, edge cases, invariants (always loaded)
           ├── PERFORMANCE.md             ← Complexity, allocations, I/O, scalability
           ├── SECURITY.md                ← Input validation, auth, secrets, crypto
           ├── ASYNC-THREADING.md         ← Async/await, concurrency, cancellation
           ├── API-DESIGN.md              ← Contracts, breaking changes, extensibility
           └── ERROR-HANDLING.md          ← Exception strategy, retry, resource cleanup
```

###  Usage

```
   @code-review review my git diff
   @code-review audit this file for security
   @code-review check async patterns in ConnectionPool.cs
```

###  Customization

- Add anti-patterns: Append to any module's anti-pattern table
- Add modules: Create new .md files (e.g., DATABASE.md) and add routing in the agent file
- Per-repo overrides: Copy into a repo's .github/ to add repo-specific checklists (repo-level takes precedence)
