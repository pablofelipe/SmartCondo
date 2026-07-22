# Getting started

Run the full stack locally from a clean machine, using Docker. For running the API or the frontend without Docker, see [`backend/README.md`](../../backend/README.md) and [`frontend/README.md`](../../frontend/README.md) instead. For deploying to Azure or AWS instead of running locally, skip to [Beyond local: real cloud deployment](#beyond-local-real-cloud-deployment).

## Prerequisites

- Docker and Docker Compose
- `curl` (or any HTTP client) to call the migration endpoint
- `openssl` (or any way to generate a random base64 string) for the JWT signing key

## 1. Clone and configure

```bash
git clone https://github.com/pablofelipe/SmartCondo.git
cd SmartCondo
cp .env.example .env
```

Edit `.env`. At minimum, set:

- `DB_PASSWORD` — any value, this is a local database
- `JWT_KEY` — base64, must decode to at least 32 bytes:

  ```bash
  openssl rand -base64 32
  ```

- `ADMIN_EMAIL` / `ADMIN_PASSWORD` — credentials for the initial administrator account, created in step 3
- `MIGRATION_AUTH_KEY` — any value; it protects the migration endpoint, see step 3

The other variables (`EmailSettings__*`, `ALLOWED_ORIGINS`, `FRONTEND_BASE_URL`) have working defaults for local use. See [`.env.example`](../../.env.example) for the full list and [`backend/README.md`](../../backend/README.md#configuration) for what each one does.

## 2. Build and start

```bash
docker compose up --build
```

This starts three containers: PostgreSQL, the API, and the frontend (served by nginx). The API container waits for PostgreSQL's healthcheck before starting; the frontend waits for the API's.

| Service | URL |
|---|---|
| Frontend | http://localhost:3000 |
| API | http://localhost:5000 |
| Swagger | http://localhost:5000/swagger |
| GraphQL | http://localhost:5000/graphql |
| PostgreSQL | localhost:5432 |

## 3. Apply migrations and seed the admin account

The database starts empty. Apply EF Core migrations and create the initial administrator account through the migration endpoint (guarded by `X-Migration-Auth`, compared against `MIGRATION_AUTH_KEY`):

```bash
curl -X POST http://localhost:5000/api/v1/migration/migrate \
  -H "X-Migration-Auth: <your MIGRATION_AUTH_KEY>"
```

This also seeds the `UserType` lookup rows (the role catalog). It's safe to call again later — it only creates the admin account if one doesn't already exist yet.

## 4. Verify

- **Health checks:**

  ```bash
  curl http://localhost:5000/health/live    # process is up
  curl http://localhost:5000/health/ready   # database is reachable
  ```

- **Swagger UI** at http://localhost:5000/swagger — confirms the API is serving and lists every REST endpoint.
- **Sign in** on the frontend (http://localhost:3000) with `ADMIN_EMAIL` / `ADMIN_PASSWORD`. A successful login and a populated dashboard confirm the database, migrations and JWT signing are all working end to end.

For a deeper walkthrough — creating condominiums, exercising different roles, sending a message, watching it arrive over WebSocket — see [functional-validation.md](functional-validation.md).

## 5. Shutdown

```bash
docker compose down          # stop containers, keep the database volume
docker compose down -v       # stop containers and delete the database volume
```

## Troubleshooting

See [troubleshooting.md](troubleshooting.md) for problems encountered running this stack and provisioning the cloud deployments.

## Beyond local: real cloud deployment

Local `docker compose` is for development. The same Docker image is also the artifact deployed to Azure Container Apps or AWS ECS/Fargate in production — provisioned by Terraform, not by hand. See [`infra/README.md`](../../infra/README.md) for that runbook and [ADR-0011](../adr/0011-container-first-cloud-agnostic-deployment.md) for why this deployment shape was chosen.
