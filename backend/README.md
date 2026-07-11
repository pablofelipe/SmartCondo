# SmartCondo — Backend

ASP.NET Core 8 API exposing REST and GraphQL endpoints for the SmartCondo platform.

## Layout

```text
backend/
├── SmartCondo.sln
├── src/SmartCondoApi/
│   ├── Controllers/        # REST endpoints (versioned, /api/v1)
│   ├── GraphQL/            # HotChocolate schema: vehicle queries and mutations
│   ├── Services/           # Business logic, one folder per domain area
│   ├── Models/             # EF Core entities and SmartCondoContext
│   ├── Migrations/         # EF Core migrations
│   ├── Dto/                # Request/response contracts
│   ├── Infra/              # Cross-cutting: token handling, error middleware
│   ├── Exceptions/         # Domain exceptions mapped to HTTP status codes
│   ├── Startup.cs          # DI, authentication, CORS, GraphQL, Swagger
│   ├── Program.cs          # Host bootstrap
│   └── LambdaEntryPoint.cs # AWS Lambda hosting entry point (optional)
└── tests/SmartCondoApi.Tests/
    ├── Controllers/        # Controller behavior tests (happy paths and error mapping)
    ├── Services/           # Service unit tests
    └── Helpers/            # Test fixtures: in-memory context, seeded users, mocks
```

## Layering

- **Controllers** validate input, dispatch to services and translate domain exceptions into HTTP responses (`ErrorHandlingMiddleware` covers anything unhandled).
- **Services** hold the business rules and are registered per domain area in `Startup.ConfigureServices`. Each service receives its dependencies through constructor injection, which keeps them directly testable with mocks.
- **Models** are EF Core entities; `SmartCondoContext` also seeds the role/permission hierarchy.
- **GraphQL** complements REST for the vehicle domain, providing filtered queries with projections.

## Configuration

Configuration is environment-driven. `appsettings.json` ships with empty placeholders only; real values come from environment variables (see [.env.example](../.env.example) at the repository root):

| Variable | Purpose |
|---|---|
| `DB_HOST`, `DB_NAME`, `DB_USER`, `DB_PASSWORD` | Assembled into the PostgreSQL connection string at startup |
| `JWT_KEY` | Base64-encoded HMAC-SHA256 signing key (must decode to ≥ 32 bytes) |
| `ALLOWED_ORIGINS` | Comma-separated CORS allow-list |
| `EmailSettings__*` | SMTP settings for confirmation and password-reset e-mails |
| `ADMIN_EMAIL`, `ADMIN_PASSWORD` | Initial administrator created by the migration endpoint |
| `MIGRATION_AUTH_KEY` | Shared key required by `POST /api/v1/migration/migrate` |
| `FrontendSettings__BaseUrl` | Frontend base URL used in password-reset links |

## Run

```bash
cd backend
dotnet restore SmartCondo.sln
dotnet run --project src/SmartCondoApi
```

Swagger UI is available at `/swagger` in the Development environment.

### Database

Point `DB_*` variables at a PostgreSQL instance, then apply migrations either with the EF CLI:

```bash
dotnet ef database update --project src/SmartCondoApi
```

or through the seeding endpoint (also creates roles and the admin account):

```bash
curl -X POST http://localhost:5000/api/v1/migration/migrate \
  -H "X-Migration-Auth: <MIGRATION_AUTH_KEY>"
```

## Tests

```bash
dotnet test SmartCondo.sln
```

Tests use MSTest with Moq and the EF Core InMemory provider — no external services required.

## AWS Lambda deployment (optional)

`LambdaEntryPoint` allows hosting the same application as an AWS Lambda function behind API Gateway; `aws-lambda-tools-defaults.json` carries the deployment defaults for the .NET Lambda CLI.
