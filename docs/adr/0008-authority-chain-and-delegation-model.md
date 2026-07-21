# ADR 0008 — Authority chain and delegation model

**Status:** Accepted

## Context

ADR-0005 established that an actor's Scope is derived from their institutional membership in a tenant, not freely chosen by the actor and not implied by their role alone. That leaves an open question: membership granted by whom, and under what authority? Without an answer, Scope has no principled origin — it would just be read from a raw stored value, with nothing preventing that value from being set arbitrarily.

The product's existing capability table (`RolePermissions`) already encodes a partial, unenforced answer to this question, in the fields that describe which role can bring which other roles into existence. This ADR makes that answer explicit, extends it to a complete chain, and records which parts of it are still open business decisions rather than settled ones.

## Decision

Authority in SmartCondo forms a directed chain, not a flat set of independent grants:

- **The platform itself is the root of authority.** The first Platform Operator is not granted by any in-system actor — it originates from possession of deployment-level credentials, outside the actor graph entirely. This is deliberate: the root of trust should not depend on the system already containing a trusted actor.
- **The Platform Operator grants Tenant Administrator authority**, one tenant at a time, and may also grant Platform Operator authority to other actors (the root is self-perpetuating, appropriate for a small, trusted operating team rather than a widely delegated role).
- **A Tenant Administrator grants authority within their own tenant only**, to: a peer-but-subordinate administrative delegate, a Delegated Tenant Representative, Tenant Members, Operational Staff, and External Collaborators. A Tenant Administrator can never grant Platform Operator authority, and an administrative delegate can never grant authority equal to or above a Tenant Administrator — the two are not peers, despite both holding broad administrative capability.
- **A Delegated Tenant Representative grants a narrower slice of authority** — limited to bringing new Tenant Members into existence, not administrative or operational roles. The system deliberately does not model the real-world process by which a representative comes to hold that position (e.g., an election among residents) — it only reflects the outcome, granted top-down by the Tenant Administrator. This is an intentional simplification, not a gap.
- **Operational Staff may grant existence only to Subjects**, never to Actors — consistent with ADR-0005's Actor/Subject distinction. Registering a Subject is not a delegation of authority; it is the creation of a record.

**Corollary — tenant scope is singular.** A Tenant Administrator's Scope is always exactly one tenant. This is not a technical limitation; it follows directly from the chain above: authority is granted per tenant, by a single act, and nothing in the chain produces a grant spanning multiple tenants. If a future business need requires one actor to administer several tenants (e.g., a management company operating a portfolio of condominiums), the correct evolution is to introduce a new domain concept for that portfolio-level authority and let Scope derive from it — not to stretch the Tenant Administrator role into a many-tenant relationship it was never granted.

### Open questions

The following are not resolved by this ADR. None of them block the authorization work already planned; they are recorded here so the gap is a conscious, tracked decision rather than a silent omission, and so future work extends this ADR instead of introducing a parallel, inconsistent mechanism:

1. **Revocation granularity** — today, revoking authority means disabling the entire account. Whether partial revocation (e.g., removing Delegated Tenant Representative status while the actor remains a Tenant Member) needs to be a first-class concept is undecided.
2. **Access recovery** — there is no defined path for restoring a Tenant Administrator's access if it is lost (e.g., an employee departure without handover). Whether this must always route through the Platform Operator, or can be delegated within the tenant, is undecided.
3. **Succession** — what happens to a tenant whose only Tenant Administrator is removed is undecided: manual Platform Operator intervention, automatic promotion of the administrative delegate, or something else.
4. **Mandate expiry and inheritance** — whether a Delegated Tenant Representative's authority reverts automatically when their real-world mandate ends, or always requires a manual act by the Tenant Administrator, is undecided.

### Deferred decisions

Unlike the open questions above, this is not an unresolved question — it is a decision that has been made (not now), with the reasoning and the criteria for revisiting it recorded explicitly, so the choice stays conscious rather than defaulting by inertia.

**Audit and traceability posture.** This ADR's Consequences state that every Scope grant must be traceable to a specific act of authority; ADR-0009's invariant 7 restates the same requirement. As implemented, that traceability exists only at decision time — the chain of authority is checked and enforced synchronously by the operation that creates the grant. Nothing durable is persisted that would let someone later reconstruct who granted what, to whom, when, and under whose authority — the only records that exist are incidental application log lines, not a structured, queryable trail, and no audit-log entity exists anywhere in the domain model.

This is deliberately deferred, not planned, as of 2026-07-21: no compliance requirement or incident has been reported that would justify the cost of a durable, structured audit trail — a new entity, a migration, and instrumentation across every authorization-relevant write path in the system. Revisit this decision if any of the following occurs: (a) a compliance or contractual requirement to reconstruct authorization history after the fact; (b) an actual incident where the absence of such a trail impeded investigation; (c) a deployment context where an external party (auditor, regulator, enterprise customer) requires it.

## Consequences

- Every Scope grant must be traceable to a specific act of authority performed by an actor who was themselves authorized, per this chain, to perform it. An operation that creates a new actor's institutional membership without validating the granting actor's position in this chain is a defect against this ADR, not an acceptable shortcut. Per the Deferred decisions above, "traceable" is satisfied at decision time (the chain is checked and enforced when the grant happens); it does not currently imply a durable, after-the-fact audit record.
- The existing capability fields that encode this chain (which role may bring which other roles into existence) are the source of truth for enforcement; they must not be duplicated or reimplemented ad hoc by individual use cases.
- No actor may, through their own granted capability, produce another actor with authority equal to or greater than their own, except where this ADR explicitly allows it (the Platform Operator root).
- The open questions and deferred decisions above are explicitly out of scope for the authorization work currently planned. Any future work on revocation, recovery, succession, mandate handling, or audit/traceability must amend this ADR rather than being implemented as an isolated feature.
- This ADR depends on ADR-0005 and is depended upon by the Scope-enforcement work planned for the implementation phase; it does not by itself change any running behavior.
