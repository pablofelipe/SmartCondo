# API reference

Full, always-current documentation is served by **Swagger UI** at `/swagger` (Development environment) and by the **GraphQL IDE** at `/graphql`. This page is a map of the surface area.

Base path: `/api/v1` — all endpoints except login, password reset and migration require a `Authorization: Bearer <JWT>` header.

## REST resources

| Resource | Route | Purpose |
|---|---|---|
| Auth | `POST /auth/login` | Authenticate and receive a JWT |
| Forgot password | `/forgotpassword/…` | Request and confirm password reset via e-mailed token |
| User profiles | `/userprofile/…` | Register and manage residents, staff and administrators |
| User types | `/usertype/…` | Role/type catalog |
| Condominiums | `/condominium/…` | Condominium CRUD |
| Towers | `/tower/…` | Towers and apartment layout per condominium |
| Vehicles | `/vehicles/…` | Vehicle registry (also on GraphQL) |
| Messages | `/messages/…` | Resident ↔ administration messaging |
| Dashboard | `/dashboard/…` | Aggregated statistics |
| Migration | `POST /migration/migrate` | Apply EF migrations + create the initial admin (requires `X-Migration-Auth`) |

## GraphQL

Endpoint: `/graphql` (HotChocolate). The schema exposes the vehicle domain:

- **Queries** — vehicle listing with a typed filter input (`VehicleFilterInput`); filtering is hand-rolled, not HotChocolate's native filter DSL
- **Mutations** — create/update vehicles (`VehicleInput`)

Example query:

```graphql
query {
  vehicles(filter: { licensePlate: "ABC" }) {
    licensePlate
    model
    color
  }
}
```

## Error contract

Domain errors are translated to semantic HTTP status codes by the controllers (400 invalid input, 401 unauthorized/locked/disabled, 404 not found), each with a JSON `{ "message": … }` body. Unhandled exceptions are normalized by `ErrorHandlingMiddleware`, which uses the same `{ "message": … }` shape. Model-binding/validation failures instead get ASP.NET Core's automatic `ValidationProblemDetails` body (`{ "errors": { field: [msgs] }, "title", "status", … }`).
