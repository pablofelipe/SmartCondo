# ADR 0003 — GraphQL (HotChocolate) for the vehicle domain, REST elsewhere

**Status:** Accepted

## Context

Vehicle listing screens need flexible filtering (by owner, plate, model, tower) and different field selections per screen. Modeling every combination as REST query parameters was producing a growing set of ad-hoc endpoints.

## Decision

Expose the vehicle domain through a HotChocolate GraphQL endpoint (`/graphql`) with typed inputs, filtering and projections, while keeping REST (`/api/v1`) for the remaining resources where request shapes are stable.

## Consequences

- The client fetches exactly the vehicle fields it renders; filter combinations don't multiply endpoints.
- Projections push the field selection down to EF Core, avoiding over-fetching from PostgreSQL.
- Two protocols coexist in the same API; GraphQL is deliberately confined to one bounded domain to keep the operational surface small.
