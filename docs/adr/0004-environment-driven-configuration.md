# ADR 0004 — Environment-driven configuration and a guarded migration endpoint

**Status:** Accepted

## Context

Earlier iterations of the project kept connection strings and keys in `appsettings.json` variants, which is both a leak risk and awkward across environments (local, Docker, Lambda). Serverless deployments additionally lack shell access for running `dotnet ef database update`.

## Decision

1. All sensitive or environment-specific values come from environment variables: `DB_*` (assembled into the connection string at startup), `JWT_KEY`, `ALLOWED_ORIGINS`, `EmailSettings__*`, `ADMIN_*`, `MIGRATION_AUTH_KEY`. `appsettings.json` ships with empty placeholders only.
2. Database migrations and initial seeding are exposed as `POST /api/v1/migration/migrate`, protected by a shared key in the `X-Migration-Auth` header, so any deployment target can be migrated with a single authenticated HTTP call.

## Consequences

- The repository contains no credentials; `.env` files are git-ignored and documented by `.env.example`.
- The same image runs unchanged in docker-compose and AWS Lambda — only the environment differs.
- The migration endpoint is a deliberate, auditable administrative surface; it refuses to run when the key is unset.
