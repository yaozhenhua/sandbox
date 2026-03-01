# Security

> **Load when**: Code handles user input, authentication, authorization, cryptography,
> secrets, network boundaries, file I/O, or configuration.

## Core Questions

### Input Validation
- Is all external input validated before use?
- Are inputs sanitized for injection (SQL, XSS, command injection, path traversal)?
- Are deserialization inputs trusted? (Untrusted deserialization = RCE risk)
- Are file paths validated against traversal (`../`)?

### Authentication & Authorization
- Is authentication checked before authorization?
- Are authorization checks present on every entry point (not just the UI)?
- Is the principle of least privilege applied?
- Are tokens validated (expiry, issuer, audience, signature)?
- Are default credentials or hardcoded secrets present?

### Secrets & Credentials
- Are secrets stored securely (Key Vault, env vars) — never in code or config files?
- Are connection strings parameterized, not interpolated?
- Are secrets logged or included in error messages?
- Is PII handled according to compliance requirements?

### Cryptography
- Are deprecated algorithms used (MD5, SHA1, DES)?
- Are random values generated with cryptographic RNG?
- Are keys of sufficient length?
- Is TLS enforced for network communication?

### Network & Trust Boundaries
- Are CORS policies appropriate?
- Are rate limits in place for public endpoints?
- Is input from other services treated as untrusted?
- Are error messages leaking internal details to callers?

## Anti-Patterns to Flag

### General C#/.NET

| Pattern | Risk |
|---------|------|
| Hardcoded password, API key, connection string, or SAS token | 🔴 Credential exposure |
| `string.Format` or interpolation in SQL queries | 🔴 SQL injection |
| `Process.Start` with user-supplied arguments without escaping | 🔴 Command injection |
| `new Random()` for security-sensitive values (tokens, nonces) | 🔴 Predictable output — use `RandomNumberGenerator` |
| Catching auth/authz exceptions and continuing execution | 🔴 Authentication/authorization bypass |
| Logging user tokens, passwords, or bearer headers | 🔴 Credential exposure in logs |
| `ServicePointManager.ServerCertificateValidationCallback = (s, c, ch, e) => true` | 🔴 MITM vulnerability — disables TLS validation |
| Missing `[Authorize]` on controller/action | 🟡 Unauthenticated access to endpoint |
| Error response includes stack trace or internal type names | 🟡 Information disclosure |
| `XmlSerializer` / `BinaryFormatter` on untrusted input | 🔴 Remote code execution — use `System.Text.Json` |
| `Path.Combine(basePath, userInput)` without canonicalization check | 🔴 Path traversal — `../` escapes base |
| `Assembly.Load` / `Activator.CreateInstance` from user input | 🔴 Arbitrary code loading |
| HTTP redirect followed without validating target domain | 🟡 Open redirect / SSRF |
| CORS `AllowAnyOrigin()` + `AllowCredentials()` | 🔴 Credential theft via malicious origin |
| Regex with user input without timeout (`RegexOptions.None`) | 🟡 ReDoS — catastrophic backtracking |
| `MD5` / `SHA1` used for integrity or password hashing | 🟡 Deprecated — use `SHA256`+ or `bcrypt`/`Argon2` |
| Secrets in `appsettings.json` committed to source control | 🔴 Credential in repo history |
| JWT validated without checking `exp`, `iss`, or `aud` claims | 🔴 Token reuse / forgery |
| `AllowAnonymous` on endpoint that mutates state | 🔴 Unauthenticated write |
| PII stored in plain text in logs or telemetry | 🟡 Compliance violation (GDPR, HIPAA) |

## Output Template

```
### Security
- **Input Validation**: [findings]
- **Auth**: [findings]
- **Secrets**: [findings]
- **Network**: [findings]
```
