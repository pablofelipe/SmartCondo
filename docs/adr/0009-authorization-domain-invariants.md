# ADR 0009 — Authorization domain invariants catalog

**Status:** Accepted

## Context

ADR-0005 and ADR-0008 each state, in prose, facts that must always hold for the authorization model to stay coherent. Left embedded in those documents' prose, these facts are easy to lose track of as the system evolves and hard to check a proposed change against. This ADR extracts them into a single, named catalog. Unlike ADR-0005, ADR-0006, ADR-0007 and ADR-0008, this document records no independent decision and considers no alternatives — every entry is a direct consequence of a decision already made elsewhere, cited accordingly.

## Decision

The following invariants hold for the SmartCondo authorization model at all times.

1. **Tenant isolation** — no operation may observe or affect a tenant other than those in the acting Actor's Scope, except for the Platform Operator. *(ADR-0005 — Scope)*
2. **Single-tenant scope** — a Tenant Administrator's Scope is always exactly one tenant; multi-tenant administration requires a new domain concept, not an expanded Scope. *(ADR-0008)*
3. **Role immutability** — a Role's Capability set is identical across every tenant; no tenant may customize it. *(ADR-0005 — Capability)*
4. **Capability composition** — an authorization decision is always the full composition of Capability, (Scope or Relationship), and State; no factor may be silently skipped for a given operation, only satisfied trivially. *(ADR-0005)*
5. **Relationship independence** — Relationship-based authority is granted independently of administrative Capability; an Actor with zero administrative Capability can still hold full authority over their own resources. *(ADR-0005 — Relationship)*
6. **State restricts, never grants** — a resource's or actor's State may suspend an operation that Capability and Scope would otherwise authorize; it may never authorize an operation those factors deny. *(ADR-0005 — State)*
7. **Traceable scope grants** — every Scope an Actor holds must be traceable to a specific act of authority performed by another Actor who was themselves authorized, per the authority chain, to grant it. *(ADR-0008)*
8. **Authority chain integrity** — no Actor may, through their own Capability, grant a Role with authority equal to or greater than their own, except the Platform Operator's self-perpetuating root. *(ADR-0008)*
9. **Delegation preserves origin** — a Delegated Tenant Representative's authority does not change their underlying archetype; they remain a Tenant Member with an additional narrow Capability, not a Tenant Administrator. *(ADR-0005 — actor archetypes; ADR-0008)*
10. **Subject non-authority** — a Subject is never a source of authorization for any operation, including operations on its own record. *(ADR-0005 / ADR-0006 — Actor vs. Subject)*
11. **Single kernel** — exactly one shared surface computes authorization decisions; no bounded context may reimplement any part of it independently. *(ADR-0007)*

## Consequences

- This catalog is the reference for any future review or automated check that needs to answer "does this change violate an authorization invariant" — a violation of any entry here is a defect, not a design choice under debate.
- Adding an invariant here requires it to already follow from an accepted ADR; this document is not the place to introduce a new rule that has not been decided elsewhere first.
- The four open questions recorded in ADR-0008 (revocation granularity, access recovery, succession, mandate expiry) are deliberately not listed as invariants — they are unresolved, and listing them here would misrepresent that.
