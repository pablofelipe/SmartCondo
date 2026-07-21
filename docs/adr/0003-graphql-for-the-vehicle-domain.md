# ADR 0003 — GraphQL (HotChocolate) for the vehicle domain, REST elsewhere

**Status:** Accepted

## Context

Vehicle listing screens need flexible filtering (by owner, plate, model, tower) and different field selections per screen. Modeling every combination as REST query parameters was producing a growing set of ad-hoc endpoints.

## Decision

Expose the vehicle domain through a HotChocolate GraphQL endpoint (`/graphql`) with typed inputs, filtering and projections, while keeping REST (`/api/v1`) for the remaining resources where request shapes are stable.

## Consequences

- The client fetches exactly the vehicle fields it renders; filter combinations don't multiply endpoints.
- Filtering and field selection are implemented by hand in `VehicleService`, not through HotChocolate's `[UseProjection]`/`[UseFiltering]`/`[UseSorting]`/`[UsePaging]` middleware — see Amendment below.
- Two protocols coexist in the same API; GraphQL is deliberately confined to one bounded domain to keep the operational surface small.

## Amendment

The implementation has always used hand-rolled LINQ filtering in `VehicleService.GetFilteredVehiclesAsync`, never HotChocolate's `[UseProjection]`/`[UseFiltering]`/`[UseSorting]`/`[UsePaging]` middleware — those attributes have been present, commented out, since the vehicle module's first commit. This ADR originally credited projections with "avoiding over-fetching from PostgreSQL"; that benefit was never realized, since projections were never enabled. That justification is retracted; it described an intended mechanism, not the one actually running.

The query's composition is no longer just a filtering concern, either. Since the authorization domain model (ADR-0005) was applied to the vehicle domain, `GetFilteredVehiclesAsync` must shape the query itself around Capability, Relationship and Scope before any filter is applied: an actor without view Capability gets a self-ownership-only query that ignores the filter input entirely (Relationship), and an actor without unbounded Scope gets an additional tenant restriction. This authorization-driven shaping has to run first and decide what the base query even is — HotChocolate's built-in middleware composes automatically over whatever `IQueryable` a resolver returns, using GraphQL's own filter syntax, with no seam for that per-actor decision to happen first.

Reactivating the built-in middleware today would therefore require an architectural redesign — moving actor-dependent query shaping into a reusable place the middleware can compose with (or replacing the current `VehicleFilterInput` DTO with HotChocolate's native filter types) — not simply uncommenting the attributes. No such redesign is planned now.

This decision is worth revisiting if authorization for list/read operations is ever consolidated into a reusable, resolver-agnostic pipeline; until then, hand-rolled filtering remains the correct choice given the coupling to per-actor query shaping described above. The Decision above (GraphQL for the vehicle domain, REST elsewhere) is unaffected — it still holds on the single-endpoint / flexible-filter-combination rationale, independent of which filtering mechanism is used internally.
