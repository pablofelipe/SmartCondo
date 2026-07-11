# ADR 0001 — Consolidate backend, frontend and tests into a monorepo

**Status:** Accepted

## Context

The project started as three independent repositories: the API, the React client and the API test suite. Coordinating changes across them required synchronized commits in three places, the test suite could silently drift from the API it tested, and there was no single place to run the whole system or read its documentation.

## Decision

Merge everything into a single repository with top-level `backend/`, `frontend/`, `docs/` and `docker/` directories. The test project lives inside `backend/tests/` and is part of the backend solution, so `dotnet test` always exercises the code it sits next to.

## Consequences

- One clone, one `docker compose up`, one CI pipeline covering API, tests and frontend build.
- Cross-cutting changes (e.g. a new endpoint plus its client call) are reviewed as a single unit.
- Repository history starts fresh at the consolidation point; the previous split-repository history is intentionally not carried over.
