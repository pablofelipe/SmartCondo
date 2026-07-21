# ADR 0005 — Authorization domain model

**Status:** Accepted

## Context

Authorization in SmartCondo grew organically. A fixed capability table per user type exists (`RolePermissions`), alongside a handful of unrelated, ad-hoc ownership checks scattered across individual use cases, but the system has never had a single, explicit model of what "authorization" means for this domain. As a result, enforcement is inconsistent: some operations check the acting user's type, some additionally check tenant membership, most check nothing beyond "the caller is authenticated," and none check whether the caller owns the specific resource being acted upon.

Before deciding any implementation mechanism, the domain needs a model that would remain valid even if the system were rewritten in a different language or framework. This ADR defines that model. It intentionally says nothing about how it gets enforced — that is a matter for the implementation phase and is a consequence of this decision, not an input to it.

A foundational premise, confirmed for this product: **capability is fixed per role and versioned with the product.** A Condominium Administrator has the same conceptual authority in every condominium; nothing in the domain allows a tenant to customize which capabilities a role holds. What varies between tenants is never the capability matrix — it is the set of resources a given actor's capability applies to.

## Decision

Authorization is the composition of two independent planes plus two modifiers, never a single flat rule set.

**Capability** — what a role can, in principle, do. Fixed per role, defined by the product, not customizable per tenant. Answers "is this class of operation even conceivable for this kind of actor," independent of any specific resource instance.

**Scope** — the set of tenants under a given actor's jurisdiction. Derived from the actor's institutional membership, not from their role. Two actors with the same role (e.g., two Condominium Administrators) have identical Capability and disjoint Scope. A Platform Operator is the sole actor whose Scope is unbounded, by definition of that role rather than by an explicit grant (see ADR-0008 for how Scope is established for every other role).

**Relationship** — a direct link between one specific actor and one specific resource instance, independent of administrative Capability. Self-ownership ("this is my own profile," "this is my own vehicle") is the primary example: it grants authority on its own, without requiring any administrative capability, and without being subordinate to the role hierarchy.

**State** — the current condition of a resource or actor that may suspend an operation that Capability and Scope would otherwise authorize (e.g., a disabled tenant blocks new registrations even for an actor who otherwise qualifies). State can only restrict, never grant, authority beyond what the other factors already allow.

These combine as:

```
Decision(Actor, Operation, Resource) =
    [ ( Capability(Role(Actor), Operation) AND Scope(Actor) contains Tenant(Resource) )
      OR Relationship(Actor, Resource) satisfies Operation ]
    AND State(Resource) permits Operation
```

Self-service (the Relationship path) and the administrative path are independent sources of authority, not two ways of satisfying the same requirement — self-service needs no administrative Capability at all, because the authority to act on one's own resource does not derive from one's role. Which paths apply to a given operation is a property of that operation's definition, not a universal rule; some operations (e.g. creating a brand-new resource with no prior owner) only ever have an administrative path, because there is no existing resource to hold a Relationship to.

### Actors

The domain recognizes a fixed set of actor archetypes, independent of the specific role catalog:

- **Platform Operator** — exists outside any tenant; the only actor with unbounded Scope.
- **Tenant Administrator** — full administrative Capability, Scope bounded to exactly one tenant.
- **Delegated Tenant Representative** — a Tenant Member granted a narrow slice of administrative Capability (e.g., an elected resident representative); remains fundamentally a Tenant Member, not an Administrator.
- **Tenant Member** — the party the product exists for; near-zero administrative Capability, full Relationship-based authority over their own resources.
- **Operational Staff** — works inside one tenant, narrow Capability limited to specific operational tasks.
- **External Collaborator** — touches the system tangentially, minimal Capability, no administrative footprint.

A further distinction cuts across this list: not every entry in the domain's role catalog necessarily represents an **Actor** (something capable of initiating an operation). Some represent **Subjects** — records managed by an Actor, without the ability to act themselves. The precise vocabulary for naming and applying this distinction is the subject of ADR-0006; this ADR only establishes that the distinction is structural, not incidental.

## Consequences

- No operation's authorization may be expressed as a single flat check ("is the caller of role X"); every operation must be expressible as the composition above, even when some factors are trivially satisfied.
- Capability must never be read from, or influenced by, tenant-specific data — any apparent need to do so is a signal that the requested change belongs in Scope, not Capability.
- Scope is not a value an actor can set for itself; it is established through a traceable act of authority, formalized in ADR-0008.
- Relationship-based authorization (self-ownership and similar) must be modeled as a first-class, independent path — not folded into, or treated as a special case of, administrative Capability.
- State-based restrictions must be evaluated at decision time against the resource's current condition, never assumed from a snapshot taken earlier (e.g., at login).
- This model supersedes no prior ADR; it fills a gap. ADR-0002 remains the accepted decision for how identity is established — this ADR governs what happens once identity is known.
- ADR-0006 (ubiquitous language), ADR-0007 (authorization as a supporting subdomain), ADR-0008 (authority chain and delegation) and ADR-0009 (domain invariants) refine and formalize parts of this model; none of them may contradict the structure defined here without superseding this ADR explicitly.

## Amendment

The decision formula originally published here expressed Capability as an unconditional top-level requirement (`Capability AND [Scope OR Relationship] AND State`), which contradicted this same document's own prose describing Relationship as authority granted "on its own, without requiring any administrative capability." Implementation work on the first Relationship-based checks surfaced the contradiction directly: applying Capability as a blanket prerequisite would have denied actors authority over their own resources whenever their role held no matching administrative Capability, which is precisely the case self-service exists to cover.

The formula is corrected above to state the administrative path and the self-service path as two independent, OR-combined sources of authority, each evaluated on its own terms, with State applying to whichever path succeeds. No other part of this ADR changes.
