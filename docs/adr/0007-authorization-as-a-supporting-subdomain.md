# ADR 0007 — Authorization as a supporting subdomain (shared kernel)

**Status:** Accepted

## Context

The concepts formalized in ADR-0005 and ADR-0008 do not currently have a home. The types that carry them live inside the user-facing part of the codebase, and are reached into directly by at least one unrelated area that needed an authorization answer and had nowhere else to get it. Before Phase 1 touches Condominium, Vehicle, or User, it needs to be settled whether Authorization is a bounded context of its own — with its own model, boundary, and potentially independent ownership — or something else, because that choice determines whether other areas are allowed to reach into its internals or must only consume it from the outside.

## Decision

Authorization is a **supporting (generic) subdomain**, not a bounded context of its own, realized as a **shared kernel**: a small, deliberately stable model — the vocabulary fixed by ADR-0006, the decision function fixed by ADR-0005, and the authority chain fixed by ADR-0008 — depended upon by every other part of the domain (Condominium, User, Vehicle, Message, and anything added later), owned by none of them individually.

The distinction matters: a subdomain is a category of what the business does — nobody adopts SmartCondo to "have authorization," it exists to support administering condominiums, housing residents, and exchanging messages. A bounded context is a boundary in the solution, with its own consistent model and language. Treating Authorization as a full bounded context would imply it deserves its own persistence, lifecycle, and ownership boundary — none of which this domain currently justifies. Treating it as a shared kernel gives it the property that actually matters here: a single, stable surface every other part of the domain consumes identically, instead of reimplementing.

Concretely: Condominium, User, Vehicle and Message each define their own resources, operations, and internal rules — but none of them compute an authorization decision on their own terms. Each asks the shared kernel whether a given Actor may perform a given Operation on a given Resource, using the vocabulary of ADR-0006, and acts on the answer. None of them read Capability data directly, branch on a Role name, or reimplement any part of the ADR-0005 decision formula inside their own logic — that pattern is exactly what produced the inconsistency this ADR exists to end.

## Consequences

- No context-specific service may contain its own copy of authorization logic, however small — a single conditional branching on a Role name inside a use case is a violation of this boundary, not a convenience.
- Because a shared kernel changes rarely and deliberately, unlike an internal implementation detail of any one context, any change to the vocabulary or decision function requires updating ADR-0005, ADR-0006 or ADR-0008 first; code changes follow, they do not lead.
- This ADR does not mandate a particular code-level shape — that is an implementation decision for Phase 1. What is fixed is that exactly one shared surface exists, and every context consumes it, never reimplements it.
- The messaging area currently reimplements this logic internally. Per the Phase 1 roadmap, it is migrated to consume the shared kernel last, once every other context has proven the shared surface against real use cases — not because it is exempt from this ADR, but because it is the one place the logic already works, kept as a reference until the shared surface is trustworthy enough to replace it.
