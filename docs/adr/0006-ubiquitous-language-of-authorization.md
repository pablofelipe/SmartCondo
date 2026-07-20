# ADR 0006 — Ubiquitous language of authorization

**Status:** Accepted

## Context

ADR-0005 fixed the structural model of authorization (Capability, Scope, Relationship, State) but deliberately left several terms unresolved, because they are naming questions, not structural ones. The current codebase uses multiple, inconsistent names for concepts ADR-0005 already treats as single ideas, and at least one concept — a constraint narrowing a Capability to specific counterpart Roles — exists three times under three different names with two different encodings. Starting implementation work before this is settled would let each new piece of code pick its own name, compounding the inconsistency instead of resolving it.

## Decision

**"Capability" replaces "Permission."** The per-operation flags in `RolePermissions` and the identity-layer claims currently labelled `Permission:*` describe exactly the concept ADR-0005 calls Capability. "Permission" is retired from the domain's vocabulary — it is naming inherited from the identity/claims layer, not a domain term.

**"Role" replaces "UserType."** The classification that determines an actor's Capability set is called Role throughout ADR-0005 and every authorization document built on it. "UserType" names the same concept after its persistence origin rather than its domain meaning, and today coexists with a second, unrelated technical notion of "Role" borrowed from the identity layer. Once that identity-layer infrastructure is retired (Phase 1, Capability-source consolidation), "Role" is free to mean exactly one thing in this system.

**"Counterpart Constraint" replaces `RegisterableUserTypes`, `AllowedRecipientTypes` and `BlockedUserTypes`.** Some Capabilities are not simply yes/no for the acting Role — they are qualified by which other Role is on the other side of the operation (who is being registered, who is being messaged). This qualification is the same kind of fact in every case, today expressed as three unrelated fields with two incompatible encodings. Going forward, any Capability that needs this qualification is described by a Counterpart Constraint, in exactly one of two forms:
- **Inclusion** — the operation is allowed only toward the listed Roles (appropriate when the reachable set is small).
- **Exclusion** — the operation is allowed toward every Role except the listed ones (appropriate when the reachable set is nearly the whole catalog).

A given (Role, Capability) pair uses exactly one form, never both. This is a naming and modeling decision, not a data-migration decision: registering someone and messaging someone remain separate per-Capability facts, not required to reach the same counterparts — what is unified is the concept they both instantiate, not the underlying fields.

**Actor vs. Subject is decided by a test, not a list.** ADR-0005 introduced the distinction without a rule for applying it. The rule: a Role is an **Actor** if at least one of its Capabilities allows it to initiate an operation; otherwise it is a **Subject**. Applying this test to the current Role catalog yields exactly one Subject — the Role whose entire Capability set is false — and classifies every other Role, including the narrowest external collaborators, as an Actor, since each holds at least the capability to initiate a message. This corrects an earlier, looser reading during the modeling discussion that had also flagged some narrow external-collaborator Roles as possible Subjects; the precise test does not support that. Actor/Subject is a consequence of a Role's Capabilities, not a separately maintained label, and reclassifies automatically if a Role's Capabilities change.

## Consequences

- Any code, document, or field introduced from Phase 1 onward uses Capability, Role, Counterpart Constraint (Inclusion/Exclusion), Actor and Subject exclusively; "Permission," "UserType" as a name, and the three-field constraint pattern are legacy vocabulary to retire, not extend.
- The "Role" naming decision is only safe because the competing identity-layer Role infrastructure is retired in the same implementation phase (Capability-source consolidation) — the two are sequenced together deliberately.
- Because Actor/Subject is derived from Capability rather than stored separately, no new field is needed to track it — but it also means granting an initiating Capability to today's single Subject Role would silently reclassify it as an Actor, which is correct behavior but should be a deliberate choice when it happens, not an accident.
- The current registration flow issues login credentials uniformly to every Role, including the one classified here as a Subject. This ADR does not mandate changing that; whether Subjects should receive credentials at all is an implementation-shaped question for Phase 1, not resolved here.
- This ADR changes no running code; it fixes the vocabulary that Phase 1 work must use when it does.
