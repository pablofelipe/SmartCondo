# ADR 0010 — The authenticated actor carries current account and tenant state, resolved per request

**Status:** Accepted

## Context

ADR-0005 defines State as one of the four factors in every authorization decision and requires it to be "evaluated at decision time against the resource's current condition, never assumed from a snapshot taken earlier, e.g., at login." An independent architecture audit found that the running system does not comply with this for the acting actor's own state: `User.Enabled` and lockout are checked only inside `AuthService.Login`; the actor's own tenant (`Condominium.Enabled`) is not checked anywhere in the authentication path for an already-registered member — only new registrations are blocked from joining a disabled condominium. A JWT issued before an account or its tenant is disabled remains fully usable, with no re-validation, until it expires (24 hours).

`AuthenticatedActor` (introduced in Phase 1 Step 3, ADR-0007's shared kernel) is the value type answering "who is acting." Today it carries only `Id` and `Role`, built synchronously by `AuthenticatedActorFactory.FromClaimsPrincipal` parsing JWT claims — no state, no database access. Separately, every Scope-checked service (`UserProfileService`, `VehicleService`, `CondominiumService`, `TowerService`) independently queries `SmartCondoContextExtensions.GetActorCondominiumIdAsync(actor.Id)` to answer the same recurring question — the actor's own tenant — each paying its own database round-trip for a fact that doesn't vary by service.

Four mechanisms were evaluated as alternatives to closing the State gap: a fresh per-request query, an in-memory cache with a TTL, `SecurityStamp`-based token revalidation, and shortening the JWT's lifetime. The first is the only one that satisfies ADR-0005's invariant as written without redefining what "decision time" means; the others either reintroduce a bounded staleness window (cache, shorter lifetime) or solve a different problem (a general revocation primitive, valuable but not required to close this specific gap).

## Decision

**`AuthenticatedActor` is extended to carry the actor's current state, not just its identity:**

```csharp
public sealed record AuthenticatedActor(long Id, string Role, bool Enabled, int? CondominiumId, bool CondominiumEnabled);
```

`CondominiumEnabled` only constrains actors who have a `CondominiumId`; a Platform Operator/`SystemAdministrator` with no tenant is unaffected by it, consistent with ADR-0008's unbounded-Scope treatment of that archetype.

**Resolution moves from a static claims parser to an injectable, database-backed kernel service.** `AuthenticatedActorFactory` (a static utility) is replaced by `IAuthenticatedActorResolver`, a scoped service backed by `SmartCondoContext`, registered in DI like any other service. Controllers and GraphQL resolvers inject it (constructor injection for controllers, a `[Service]` parameter for resolvers) instead of calling a static method — this keeps the existing boundary that controllers never touch `SmartCondoContext` directly, and it avoids repeating Phase 1 Step 3's specific finding that eagerly constructor-injecting a *resolved* actor breaks `[AllowAnonymous]` actions (`UserProfileController.ConfirmEmail`): here the constructor receives the *resolver*, an inert dependency until its method is explicitly called inside an action body, so anonymous actions that never call it never touch the database for this reason.

**Resolution enforces State, not just reads it.** `IAuthenticatedActorResolver.ResolveAsync` throws `UnauthorizedAccessException` — the same type every controller and resolver already handles uniformly as HTTP 403 — when the actor's own `Enabled` is false, or when `CondominiumId` is set and the tenant's `Enabled` is false. This makes it structurally impossible to obtain a live `AuthenticatedActor` for a disabled account or tenant; no caller can forget the check, because there is no path to an actor value that skips it.

**`AuthService.Login` gains the missing tenant check.** It already checks `User.Enabled` and lockout; it now also checks the target `Condominium.Enabled` before issuing a token, closing the gap that today lets a member of a disabled tenant obtain a brand-new token. This is a separate enforcement point from `IAuthenticatedActorResolver` — login has no JWT yet to resolve an actor from — so both are required; neither substitutes for the other.

**Redundant per-service tenant lookups are removed.** Every call to `SmartCondoContextExtensions.GetActorCondominiumIdAsync(actor.Id)` that existed purely to answer "what is *this acting actor's* own tenant" is replaced by reading `actor.CondominiumId` directly, since `IAuthenticatedActorResolver` now resolves that once per request. Lookups that resolve a *different* entity's tenant (e.g., a target resource's owner) are unaffected — this only removes the actor's-own-tenant duplication.

**Out of scope, explicitly:** `WebSocketFunctions.ConnectHandler` validates its JWT directly (Sprint A) outside the ASP.NET Core authentication pipeline and does not go through `IAuthenticatedActorResolver`. It does not re-check `Enabled`/`CondominiumEnabled` today; closing that gap is a known, tracked item, not addressed by this ADR.

## Consequences

- ADR-0009's invariant 6 (State restricts, never grants) is now honored for `User.Enabled`, lockout, and `Condominium.Enabled`, both at login and on every subsequent authenticated request — not merely at token issuance.
- One additional database round-trip is introduced on every authenticated request that did not already resolve the actor's own tenant (chiefly self-service/Relationship-only paths); requests that already did so now pay that cost once, inside actor resolution, instead of twice.
- `AuthenticatedActor` now has a freshness lifetime of "this request," not "whatever was true when the JWT was signed" — it is a resolved snapshot, not a decoded claim set. This is a deliberate, narrow departure from JWT statelessness for the specific facts State requires; it is not license to keep widening `AuthenticatedActor` without the same scrutiny; each field added pays its cost on every authenticated request in the system.
- `WebSocketFunctions.ConnectHandler` remains a known gap, tracked, not closed here.

## Deferred decisions

Unlike an open question, each of these is a decision already made — not now — with the reasoning and reopening criteria recorded, matching the convention established in ADR-0008.

**SecurityStamp-based token revalidation.** Not implemented. `IAuthenticatedActorResolver` already gives per-request freshness for the two states currently known to matter (`Enabled`, `CondominiumEnabled`). A general-purpose "invalidate this session for any reason" primitive (password change, suspected compromise) is real future value, built on infrastructure this codebase already partially has (`UserManager.UpdateSecurityStampAsync` is called at registration, never revalidated) — but it is a distinct feature, not required to close the gap this ADR addresses. Revisit if a product requirement emerges for revoking a session for a reason other than account/tenant `Enabled` (e.g., "log out everywhere" after a password change).

**Refresh tokens / shorter-lived access tokens.** Not implemented. The per-request check makes the JWT's 24-hour lifetime irrelevant to State staleness specifically — a disabled account or tenant is caught on the very next request regardless of token age. Shortening token lifetime remains independently worth considering (it reduces the value of a stolen token for reasons unrelated to State), but that is a separate decision with its own UX cost (re-authentication frequency) if not paired with a refresh flow.

**Caching the per-request check.** Not implemented. Given today's single-instance deployment with no reported load problem, a fresh query per request is the correct default. Revisit only if request latency from the added query is measured to be a real problem — and even then, a TTL alone would reintroduce the exact staleness this ADR fixes, just with a smaller window; any future cache would need explicit invalidation on disable alongside a TTL, not instead of one.

**Distributed session revocation** (e.g., a shared blocklist across multiple app instances). Not implemented; not yet relevant to a single-instance deployment. Revisit if a horizontal-scaling deployment (Sprint E territory) makes per-instance state insufficient.
