# ADR 0011 — Container-first, cloud-agnostic deployment

**Status:** Accepted

## Context

A second independent architecture audit (2026-07-21) found the widest gap in the system between what is documented and what actually works: SmartCondo advertises a dual-hosted, serverless-capable backend, but there is no Infrastructure-as-Code of any kind and no documented, reproducible path to production. `docker-compose.yml` is dev-only (hardcodes `ASPNETCORE_ENVIRONMENT=Development`); `aws-lambda-tools-defaults.json` is enough to `dotnet lambda deploy-function` but defines none of the surrounding infrastructure (API Gateway REST *and* WebSocket, RDS, IAM, secrets) the Lambda path actually requires to run.

Rather than closing this gap by picking a single deployment target, the goal was reframed: use the deployment story itself to demonstrate multi-cloud architectural competence, motivated by the AWS surface already being well-exercised (Lambda, API Gateway, WebSocket API, SES, RDS, IAM) and Azure being a deliberate learning target.

A real-coupling survey (evidence-based — grep and direct reads, not the audit's prose) found the system's actual AWS coupling is narrower than it looks:
- **Genuinely coupled, live paths:** `EmailService.SendEmailAsync` calls AWS SES directly (`AmazonSimpleEmailServiceClient`, hardcoded `sa-east-1`); `NotificationService` pushes exclusively through `IAmazonApiGatewayManagementApi.PostToConnectionAsync`, a mechanism that only exists because Lambda cannot hold a persistent connection itself.
- **Not coupled, verified:** persistence (Npgsql/PostgreSQL), configuration and secrets (already entirely environment-variable-driven per the project's existing convention), Identity/JWT.
- **A previously undiscovered bug, unrelated to any cloud provider:** `frontend/src/config.ts`'s `apiGatewayUrl` is a hardcoded placeholder string, never wired to an environment variable — the WebSocket notification feature has never actually worked in any deployed environment.

Both `IEmailService` and `INotificationService` already exist as interfaces with real, independent consumers (`UserProfileController`/`ForgotPasswordController` for email; the messaging stack for notifications). Per this project's abstraction discipline — a new port needs either two real consumers or a concrete coupling to remove, never symmetry with a sibling concept — no new abstraction is justified here. The existing interfaces are already the right seam; only what's *behind* them needs to change.

The user confirmed dropping AWS SES entirely in favor of a single generic-SMTP email path (not keeping SES as an AWS-specific option), on the reasoning that a single portable implementation is simpler to maintain than a provider switch nobody will exercise for a single low-volume email feature.

## Decision

**Adopt a canonical, container-first deployment as the system's primary, validated story.** One Docker image (already built by `docker/backend.Dockerfile`) runs, unmodified, on any cloud offering managed containers plus managed PostgreSQL. This is the deployment mode the "cloud-agnostic" claim applies to.

**AWS Lambda hosting becomes an explicitly secondary, documented mode — not part of the portability claim.** `LambdaEntryPoint.cs`, `WebSocketFunctions`/`Connect`/`DisconnectFunction`, and `aws-lambda-tools-defaults.json` remain in the codebase and keep working as they do today (including Sprint A's WebSocket JWT validation), but receive no further multi-cloud investment. The system is not required to behave identically in both modes going forward — deliberately, not by oversight.

**Email moves from AWS SES to generic SMTP.** `EmailService`'s live implementation switches from `AmazonSimpleEmailServiceClient` to the SMTP path (today's dead `SendGmailAsync`, generalized: config-driven server/port/credentials, no hardcoded "Gmail" framing, made the only path). The `AWSSDK.SimpleEmail` package dependency is removed. No new interface — `IEmailService` is unchanged; only its implementation changes.

**Realtime notification gets a second, container-native implementation.** A new `INotificationService` implementation uses ASP.NET Core's built-in `UseWebSockets()` to hold connections in-process and push directly — no cloud SDK involved. This becomes the implementation wired for Kestrel/container hosting. The existing AWS API-Gateway-based `NotificationService` is retained and used exclusively by the Lambda-hosted mode (already isolated behind `LambdaServiceProvider`'s separate DI container, so this doesn't require unifying that container — it now has a principled reason to stay separate: it is genuinely a different, secondary hosting mode). Selection between the two implementations is by hosting mode, decided once in DI registration, not by a runtime abstraction layer.

**The frontend's WebSocket URL becomes environment-driven**, as a direct consequence of wiring the new native path — `config.ts`'s `apiGatewayUrl` placeholder is replaced with a real, `REACT_APP_*`-driven value, incidentally fixing the previously undiscovered bug.

**Two deployments validate the portability claim, neither required to run permanently:**
- **Azure** — Container Apps or App Service for Containers, Azure Database for PostgreSQL, Key Vault-backed secrets.
- **AWS** — the *same* image, unmodified, on App Runner or ECS/Fargate, RDS PostgreSQL, Secrets Manager.

**Success criterion:** a developer can clone the repository and have it running on either cloud in under 30 minutes, following a documented runbook. The system does not need to stay online between demonstrations.

## Consequences

- The only two genuine AWS runtime couplings identified by the survey (SES, API-Gateway-Management push) are both closed for the primary deployment path — the container image has zero AWS SDK dependency in its live code paths.
- Choosing Azure vs. AWS for the primary path becomes purely an infrastructure and configuration difference (container host, managed Postgres, secrets injection) — provable by literally redeploying the same image, not by a code branch or feature flag.
- Lambda mode's previously-flagged "dual DI container" concern is downgraded in priority by construction: it now only affects a secondary, explicitly-non-portable mode, not the system's primary demonstrated story.
- No IaC tool is chosen by this ADR — that's a deferred, implementation-level decision (see below).
- This does not attempt feature parity between hosting modes going forward. If Lambda mode drifts from the container path over time, that is an accepted, named tradeoff of this decision, not an oversight to flag later.

## Deferred decisions

**IaC tooling (Terraform, Bicep, CDK, or a manual runbook).** ~~Not chosen yet.~~ **Resolved (2026-07-22):** both deploys were proven manually first, per this section's own reopening criterion. Azure Container Apps + PostgreSQL Flexible Server, and AWS ECS/Fargate + RDS, were each provisioned by hand, validated end to end (health checks, migration, login, an authenticated WebSocket round trip), and only then codified — closing exactly the risk this section originally flagged, since every resource, parameter, and gotcha (missing CloudWatch log-group permission, RDS security-group ingress, Container Apps' automatic Log Analytics workspace) was already known before a line of Terraform was written.

Terraform was chosen over Bicep/CDK/SAM specifically because it is the only option that covers both providers with one tool — Bicep is Azure-only, CDK/SAM is AWS-only, and using a single tool across both clouds reinforces the portability story this ADR is about rather than undermining it with two unrelated toolchains. Two independent root modules, `infra/azure/` and `infra/aws/`, each with their own state — not one cross-cloud abstraction — matching the two-consumer/concrete-coupling discipline: the two providers' resource models are different enough that a unifying abstraction would only be complexity for its own sake.

The IaC was validated the same way the manual deploys were: the manually-created resources were destroyed, then recreated from `terraform apply` alone, and re-validated (health checks, migration, login) before being torn down again — proving the Terraform is sufficient by itself, not merely a written record of what was done by hand.

**CI/CD automation of the deploy.** Out of scope. The near-term goal is a documented, reproducible *manual* path under 30 minutes, not push-button automation — revisit only if the manual path becomes a repeated friction point.

**Support for additional cloud providers (GCP, etc.).** Explicitly not a goal. The two-consumer/concrete-coupling rule already argues against speculative extra adapters; only Azure and AWS are validated targets, by design, not by omission.

**Retiring AWS Lambda hosting entirely.** Not decided now. It remains a documented secondary path. Revisit if it becomes a maintenance burden that nobody exercises, or if a future need re-elevates it.
