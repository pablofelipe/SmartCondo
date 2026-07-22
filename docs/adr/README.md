# Architecture Decision Records

Index of every ADR, the evolution they trace, and the two structural choices that
run through all of them.

## Index

| ADR | Title | Status |
|---|---|---|
| [0001](0001-consolidate-into-a-monorepo.md) | Consolidate backend, frontend and tests into a monorepo | Accepted |
| [0002](0002-jwt-authentication-with-aspnet-identity.md) | JWT authentication on top of ASP.NET Core Identity | Accepted |
| [0003](0003-graphql-for-the-vehicle-domain.md) | GraphQL (HotChocolate) for the vehicle domain, REST elsewhere | Accepted, amended |
| [0004](0004-environment-driven-configuration.md) | Environment-driven configuration and a guarded migration endpoint | Accepted |
| [0005](0005-authorization-domain-model.md) | Authorization domain model | Accepted, amended |
| [0006](0006-ubiquitous-language-of-authorization.md) | Ubiquitous language of authorization | Accepted |
| [0007](0007-authorization-as-a-supporting-subdomain.md) | Authorization as a supporting subdomain (shared kernel) | Accepted |
| [0008](0008-authority-chain-and-delegation-model.md) | Authority chain and delegation model | Accepted, deferred decisions |
| [0009](0009-authorization-domain-invariants.md) | Authorization domain invariants catalog | Accepted |
| [0010](0010-authenticated-actor-carries-current-state.md) | The authenticated actor carries current account and tenant state, resolved per request | Accepted, deferred decisions |
| [0011](0011-container-first-cloud-agnostic-deployment.md) | Container-first, cloud-agnostic deployment | Accepted, amended |

"Amended" means the original Decision or Context was corrected in place — the ADR's
own convention is to add an `## Amendment` section rather than open a new ADR when a
later finding corrects, rather than supersedes, a prior one. "Deferred decisions"
means the ADR records alternatives that were evaluated and consciously not
implemented, with the reasoning and reopening criteria kept alongside the decision
that made them unnecessary for now.

## Journey

### Era 1 — Architecture and domain

The system started as three separately-versioned repositories before consolidating
into today's monorepo (0001). Early decisions established the foundation: stateless
JWT authentication on ASP.NET Core Identity, required because the API also has to run
on AWS Lambda where server-side session state isn't practical (0002); a GraphQL
endpoint for the vehicle domain's flexible per-screen filtering, REST everywhere else
request shapes are stable (0003); and environment-driven configuration with a
key-protected HTTP migration endpoint instead of shell access, which serverless
hosting doesn't have (0004).

Authorization then received dedicated modeling, in sequence: a domain model defining
authorization as the composition of Capability, Scope, Relationship and State (0005);
a fixed vocabulary so implementation wouldn't invent inconsistent names for the same
concepts (0006); Authorization established as a shared kernel — one stable model
every domain area consumes, none of them own (0007); an authority chain rooted in the
platform itself, not a flat set of independent grants (0008); a catalog of the
invariants that must hold for the model to stay coherent (0009); and closing a gap
where a disabled account or tenant remained usable until its JWT expired, by having
the authenticated actor carry current state resolved fresh per request (0010).

A platform-hardening pass followed, applying the same rigor to the operational
surface around that domain: structured logging with a correlation ID threaded
through every request, liveness/readiness health checks, a working rate limiter on
login (previously an attribute with no policy behind it), WebSocket handshake
authentication (previously trusting a raw, unauthenticated `userId` query
parameter), and exception-detail guarding on both REST and GraphQL error paths
(previously leaking internal exception messages to clients). None of this needed a
dedicated ADR — it's standard ASP.NET Core wiring, not a decision with real
alternatives to weigh.

### Era 2 — Operability and portability

An audit of the deployment story found the widest gap in the system: no
Infrastructure-as-Code, no documented path to production, and a "dual-hosted,
serverless-capable" claim the codebase didn't actually back up (0011). Rather than
picking one deployment target, the decision was to validate a cloud-agnostic claim on
two independent clouds — the container-first path became primary, AWS Lambda hosting
became an explicitly secondary mode, e-mail moved from AWS SES to generic SMTP, and
realtime notification gained a native in-process WebSocket implementation for
container hosting.

Both clouds were deployed and validated end-to-end (health checks, a real migration,
a real login, a real authenticated WebSocket connection) — first manually, to surface
every real gotcha before automating anything, then reproduced entirely from Terraform
(`infra/azure/`, `infra/aws/`) with the manually-created resources torn down and
rebuilt from `terraform apply` alone. The same, unmodified Docker image ran on both,
proving portability empirically rather than only designing for it. See
[`infra/README.md`](../../infra/README.md) for the runbook.

## Why this architecture

Two structural choices run through both eras.

**Authorization is a shared kernel, not scattered per-service logic.** Every domain
area (Condominium, User, Vehicle, Message) asks the same model the same question —
Capability, Scope, Relationship, State — instead of reimplementing authorization
checks on its own terms. A shared, stable surface every consumer reads identically
beats each area inventing its own partial answer.

**Portability is achieved by substitution behind existing seams, not by new
abstraction layers.** `IEmailService` and `INotificationService` already existed with
real, independent consumers before the deployment work started; closing the AWS
coupling meant swapping and adding implementations behind those interfaces, not
inventing new ports for email or realtime transport. A "hosting port" was considered
and rejected for the same reason — the dual entry points (`Program.cs` /
`LambdaEntryPoint.cs`) already are the adaptation point; wrapping them in another
layer would have added structure with nothing new depending on it.

Both follow the same underlying rule: introduce structure only where there's a
concrete, current need for it — two real consumers, or a coupling to remove — never
by symmetry with a sibling concept, never ahead of that need. See
[Trade-offs](../architecture/overview.md#trade-offs) for the itemized technology
choices (SMTP over SES, Terraform over Bicep/CDK/SAM, hand-rolled GraphQL filtering)
and what's deliberately out of scope.
