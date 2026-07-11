# ADR 0002 — JWT authentication on top of ASP.NET Core Identity

**Status:** Accepted

## Context

The API serves a SPA and must also be hostable on AWS Lambda, where server-side session state is impractical. Authentication needs password hashing, lockout and role management; authorization needs a role hierarchy (system administrator → condominium administrator → resident/staff).

## Decision

Use ASP.NET Core Identity for user, password and role management, and issue stateless JWTs (HMAC-SHA256) on login. Tokens carry the user's claims; every request is validated for issuer, audience, lifetime and signature. The signing key is injected through the `JWT_KEY` environment variable as a base64 value that must decode to at least 32 bytes — the application refuses to start otherwise.

## Consequences

- No session affinity: any API instance (container or Lambda invocation) can serve any request.
- Identity provides battle-tested password hashing and lockout behavior for free.
- Key rotation requires re-issuing tokens; acceptable for this application's session length.
- Tests must generate structurally valid keys — the suite uses a fixed base64 test key.
