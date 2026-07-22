# SmartCondo

[![CI](https://github.com/pablofelipe/SmartCondo/actions/workflows/ci.yml/badge.svg)](https://github.com/pablofelipe/SmartCondo/actions/workflows/ci.yml)
[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](LICENSE)
[![Release](https://img.shields.io/github/v/release/pablofelipe/SmartCondo)](https://github.com/pablofelipe/SmartCondo/releases)
[![Last Commit](https://img.shields.io/github/last-commit/pablofelipe/SmartCondo)](https://github.com/pablofelipe/SmartCondo/commits/main)

Condominium administration platform that simplifies user management, communication between residents and administrators, and hierarchical permission control.

## Overview

SmartCondo is a full-stack application for managing residential condominiums. A condominium administrator registers towers, apartments, residents and vehicles; residents exchange messages with the administration and receive notifications. Access is governed by a hierarchical role model (system administrator → condominium administrator → resident and staff roles), enforced on every endpoint.

The project is organized as a monorepo:

```text
SmartCondo/
├── backend/               # ASP.NET Core 8 REST + GraphQL API
│   ├── src/SmartCondoApi/
│   ├── tests/SmartCondoApi.Tests/
│   └── SmartCondo.sln
├── frontend/              # React 19 + TypeScript PWA
├── docs/                  # Architecture, ADRs, diagrams, API reference and guides
├── docker/                # Dockerfiles and nginx configuration
├── infra/                 # Terraform (Azure and AWS), one root module per cloud
├── .github/workflows/     # CI pipeline
└── docker-compose.yml     # Full local environment (API + frontend + PostgreSQL)
```

## Features

- **Authentication** — JWT-based login with ASP.NET Core Identity; password reset flow with e-mailed, expiring tokens
- **Hierarchical permissions** — capability/scope/relationship-based authorization for system administrators, condominium administrators, residents and staff (see `docs/adr/0005` onward)
- **Condominium management** — CRUD for condominiums, towers, apartments and user profiles
- **Vehicle registry** — resident vehicle management exposed through a GraphQL endpoint (queries, mutations, filtering)
- **Messaging** — direct messages between residents and administration, with read tracking
- **Notifications** — real-time delivery through WebSocket connections: a native in-process implementation by default (container hosting), AWS API Gateway when running as a Lambda function
- **Dashboard** — aggregated statistics for administrators

## Architecture

Component diagram: [docs/diagrams/architecture.mmd](docs/diagrams/architecture.mmd).

- The API exposes **REST** endpoints (versioned under `/api/v1`) for most resources and a **GraphQL** endpoint (HotChocolate) for vehicle queries and mutations.
- Persistence uses **Entity Framework Core** with **PostgreSQL**; schema changes are tracked as EF Core migrations and applied through a key-protected migration endpoint, which also seeds roles and the initial administrator account.
- **Deployment is container-first and cloud-agnostic** (ADR-0011): the same Docker image runs unmodified on Azure Container Apps and AWS ECS/Fargate, provisioned by Terraform — see [infra/](infra/README.md). AWS Lambda hosting (`LambdaEntryPoint`) remains available as a secondary, non-portable mode.
- All configuration (database, JWT signing key, SMTP, CORS origins) comes from **environment variables** — see [.env.example](.env.example).

More detail in [docs/architecture](docs/architecture/overview.md), decision records in [docs/adr](docs/adr), API reference in [docs/api](docs/api/rest-api.md), and setup/validation guides in [docs/guides](docs/guides).

## Tech stack

| Layer | Technologies |
|---|---|
| Backend | .NET 8, ASP.NET Core, Entity Framework Core 9, HotChocolate (GraphQL), ASP.NET Core Identity, JWT, Swagger |
| Database | PostgreSQL 16 |
| Frontend | React 19, TypeScript, Apollo Client, React Router, CRA (PWA template) |
| Tests | MSTest, Moq, EF Core InMemory (backend); Jest, React Testing Library (frontend, partial coverage) |
| Infrastructure | Docker, docker-compose, nginx, GitHub Actions, Terraform (Azure Container Apps + AWS ECS/Fargate), AWS Lambda (secondary deployment mode) |

## Running locally

### With Docker (recommended)

Requires Docker and Docker Compose.

```bash
cp .env.example .env       # then edit the values (at minimum DB_PASSWORD and JWT_KEY)
docker compose up --build
```

| Service | URL |
|---|---|
| Frontend | http://localhost:3000 |
| API | http://localhost:5000 |
| Swagger | http://localhost:5000/swagger |
| GraphQL | http://localhost:5000/graphql |
| PostgreSQL | localhost:5432 |

Generate a valid JWT key with:

```bash
openssl rand -base64 32
```

After the containers are up, apply migrations and seed the initial data (uses `MIGRATION_AUTH_KEY`, `ADMIN_EMAIL` and `ADMIN_PASSWORD` from your `.env`):

```bash
curl -X POST http://localhost:5000/api/v1/migration/migrate \
  -H "X-Migration-Auth: <your MIGRATION_AUTH_KEY>"
```

Then sign in on the frontend with `ADMIN_EMAIL` / `ADMIN_PASSWORD`.

### Without Docker

See the [backend README](backend/README.md) and the [frontend README](frontend/README.md).

## Running tests

```bash
cd backend
dotnet test SmartCondo.sln
```

The suite covers authentication flows, messaging, user registration rules and supporting services, mostly using an in-memory database and mocked dependencies. A separate integration suite (`tests/SmartCondoApi.Tests/Integration/`) runs migrations and cascade-delete behavior against a real PostgreSQL instance via Testcontainers, and requires a local Docker daemon. Frontend tests exist (`frontend/src/utils/ApiUtils.test.tsx`, Jest) but are not yet wired into the CI pipeline — run them locally with `npm test`.

More guides: [getting started from a clean machine](docs/guides/getting-started.md), [functional validation walkthrough](docs/guides/functional-validation.md), [troubleshooting](docs/guides/troubleshooting.md).

## Deployment

The canonical deployment target is a single Docker image, run unmodified on either cloud's managed-container service, provisioned by Terraform — see [ADR-0011](docs/adr/0011-container-first-cloud-agnostic-deployment.md) and [infra/README.md](infra/README.md) for the runbook. AWS Lambda hosting (`LambdaEntryPoint`) is a secondary, non-portable mode retained for the WebSocket API Gateway path.

## Roadmap

**Implemented recently:**
- Multi-cloud infrastructure as code (Terraform, Azure Container Apps + AWS ECS/Fargate) — ADR-0011
- Native in-process WebSocket notifications for container hosting, replacing AWS API Gateway as the default path
- Generic SMTP e-mail delivery, replacing AWS SES

**Planned:**
- [ ] Integration tests against a real PostgreSQL instance (Testcontainers)
- [ ] Wire frontend tests into CI

**Non-goals (deliberate, see [trade-offs](docs/architecture/overview.md#trade-offs)):**
- Kubernetes, a metrics/tracing stack (Prometheus/Grafana), asynchronous messaging, additional cloud providers, CI/CD automation of the deploy
- Frontend component tests — the business complexity is intentionally concentrated in the backend, where it is extensively covered by automated tests; the frontend is a deliberately thin CRUD client over that API

**Technical debt:**
- Lambda hosting mode is not required to keep feature parity with the container-first path going forward (accepted tradeoff, ADR-0011)

## License

Licensed under the [Apache License 2.0](LICENSE).
