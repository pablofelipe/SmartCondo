# Changelog

All notable changes to this project are documented in this file.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/) —
note the API is still pre-1.0 (see §4 of the SemVer spec) and MAY change
between minor releases.

## [0.3.0] - 2026-07-23

### Fixed
- Condominium deletion could report success in the UI when the request had
  actually failed.
- The dashboard could be left showing a corrupted state after a failed
  request.
- The user type list could get stuck in a loading state indefinitely.
- Notifications could stop reconnecting reliably after a dropped WebSocket
  connection.
- Signed-in users could briefly see a logged-out state on page load.

### Security
- Removed console logging that exposed runtime configuration and the raw
  login response in the browser.

## [0.2.0] - 2026-07-22

### Added
- Structured logging with a correlation ID threaded through every request
  (including honoring an inbound `X-Correlation-Id` header).
- Liveness/readiness health checks wired into the Docker image.
- A working rate limiter on login (previously an unenforced attribute).
- WebSocket handshake authentication via a validated JWT (previously a
  raw, unauthenticated `userId` query parameter).
- Terraform for both Azure and AWS, closing ADR-0011's deferred
  infrastructure-as-code decision; both clouds validated end-to-end from
  `terraform apply` alone.
- A container-native WebSocket notification path for Kestrel/container
  hosting, alongside the existing AWS API Gateway path for Lambda hosting.

### Changed
- Tenant-scoped authorization extended to condominium, tower and profile
  access; capability/relationship composition corrected in the
  authorization model.
- Outbound email moved from AWS SES to generic SMTP.
- Error responses unified on a single `{message}` contract.

### Fixed
- Exception details no longer leak to clients on REST or GraphQL error
  paths (guarded by environment; logged instead).
- N+1 query/save pattern in condominium-wide notification broadcast.
- Race condition on the `MaxUsers` invariant, now enforced by an atomic
  counter.

### Security
- Migration auth key comparison switched to constant-time.
- GraphQL endpoint now requires authentication.
- Tenant `State` checked on every request, not only at login.

## [0.1.0] - 2026-07-13

### Added
- First public milestone: ASP.NET Core 8 REST + GraphQL (HotChocolate) API,
  React 19 + TypeScript PWA, hierarchical role-based permissions,
  PostgreSQL/EF Core, WebSocket notifications, container or AWS Lambda
  hosting, CI pipeline.
