# Troubleshooting

Problems actually encountered running this stack locally and provisioning the Terraform deployments, not hypothetical ones. Each entry: symptom, cause, how to identify it, how to resolve it.

## Local stack (docker-compose)

### API container fails at startup with "JWT_KEY is missing" or a `FormatException`

- **Symptom:** the `backend` container exits immediately; logs show either `InvalidOperationException: JWT_KEY is missing` or a raw `System.FormatException` from `Convert.FromBase64String`.
- **Cause:** `JWT_KEY` is unset, or it's set to a plain string instead of base64 (`Startup.cs` calls `Convert.FromBase64String` directly — a non-base64 value throws the SDK's own exception, not a friendly message).
- **How to identify:** check the exception type in `docker compose logs backend`. `FormatException` means the value isn't valid base64 at all; the custom `InvalidOperationException` means it decodes but is under 32 bytes, or is missing entirely.
- **How to resolve:** generate a proper key and put it in `.env`: `openssl rand -base64 32`.

### CORS errors in the browser console, requests blocked

- **Symptom:** the frontend loads but every API call fails in the browser console with a CORS error.
- **Cause:** `ALLOWED_ORIGINS` is empty or doesn't include the origin the frontend is actually served from. `Startup.cs` falls back to `http://localhost:3000` and logs `"Nenhuma origem configurada em ALLOWED_ORIGINS. Usando fallback para localhost."` when unset — if you're accessing the frontend from a different host/port (e.g. a LAN IP, a different port), the fallback won't cover it.
- **How to identify:** grep `docker compose logs backend` for that fallback message; check the browser's Network tab for the exact `Origin` header being blocked.
- **How to resolve:** set `ALLOWED_ORIGINS` to a comma-separated list including every origin you actually use.

### `docker compose up --build` is slow, or the build context looks huge

- **Symptom:** the build step takes minutes even for a one-line code change; Docker reports a large build context size.
- **Cause:** without a `.dockerignore`, the build context includes `node_modules/`, `bin/`, `obj/` — hundreds of MB that don't belong in the image. (This was a real bug in this repository, fixed by adding `.dockerignore`.)
- **How to identify:** `docker compose build` prints the context size early in its output.
- **How to resolve:** confirm `.dockerignore` exists at the repo root and excludes `**/node_modules`, `**/bin`, `**/obj`. If you've added a new top-level directory with build artifacts, extend it.

### Frontend or backend container starts but immediately gets traffic errors / connection refused

- **Symptom:** the frontend container serves a blank page or the API is unreachable right after `docker compose up`.
- **Cause:** `docker-compose.yml` orders startup with `depends_on: condition: service_healthy` — `backend` waits for `db`'s healthcheck (`pg_isready`), `frontend` waits for `backend`'s (`/health/live`). If a healthcheck never passes, the dependent container never starts serving.
- **How to identify:** `docker compose ps` shows a container stuck in `starting` or `unhealthy`.
- **How to resolve:** check the unhealthy container's logs directly (`docker compose logs db` or `backend`) — the healthcheck is a symptom, not the root cause; find why the underlying service isn't coming up (e.g. bad `DB_PASSWORD` mismatch between `db` and `backend`).

### `POST /api/v1/migration/migrate` returns 401

- **Symptom:** the migration endpoint rejects the request.
- **Cause:** the `X-Migration-Auth` header doesn't match `MIGRATION_AUTH_KEY` (compared in constant time by `MigrationController`), or the header is missing entirely.
- **How to resolve:** confirm the header value matches `.env` exactly — copy-paste it rather than retyping.

## Cloud deployment (Terraform, Windows/git-bash)

These are specific to provisioning `infra/azure` or `infra/aws` from a Windows machine using git-bash (MSYS) as the shell — they don't apply to Linux/macOS or to PowerShell.

### `terraform apply` against Azure fails to find `az`, even though `az --version` works in the same shell

- **Symptom:** Terraform's `azurerm` provider (or a `local-exec` calling `az`) can't find the Azure CLI, despite `az` working fine when typed directly.
- **Cause:** a bash-resolvable `az` (e.g. a wrapper script under `/usr/bin`) works for *bash* subprocesses, but `terraform.exe` is a native Windows process — it resolves commands through the Windows `PATH`, not MSYS's.
- **How to resolve:** export the real Windows path in the same shell invocation before running Terraform: `export PATH="$PATH:/c/Program Files/Microsoft SDKs/Azure/CLI2/wbin"`.

### A Terraform argument that looks like a resource ID gets silently mangled

- **Symptom:** a Terraform variable or CLI argument containing something like `/subscriptions/...` ends up with an unexpected Windows path prefix (e.g. `C:/Program Files/Git/subscriptions/...`).
- **Cause:** MSYS's automatic path conversion rewrites any argument starting with a bare `/` into a Windows path, assuming it's a Unix path.
- **How to resolve:** prefix the command with `MSYS_NO_PATHCONV=1` when passing resource IDs or other `/`-prefixed strings as arguments.

### Azure resource group stuck in `Deleting` for 20+ minutes

- **Symptom:** `terraform destroy` (or `az group delete`) reports the resource group is still `Deleting` long after the command returned.
- **Cause:** observed with Container Apps' automatically-created Log Analytics workspace, which can take longer than the rest of the resource group to tear down.
- **How to identify:** `az group show --name <rg> --query properties.provisioningState`.
- **How to resolve:** wait it out — it's not usually a sign of a stuck/failed deletion, just a slow one. Don't retry the delete or force-remove the group while it's still `Deleting`.

### `terraform plan` shows a diff on the PostgreSQL `zone` even though nothing changed

- **Symptom:** repeated `terraform plan` runs show a change to the Postgres Flexible Server's availability zone with no configuration edit.
- **Cause:** Azure can auto-assign or shift the zone outside of Terraform's control; the provider then reports drift against the last-known state.
- **How to resolve:** if the zone isn't a value you deliberately pin, treat this as expected drift rather than a bug — don't chase it by re-applying repeatedly.

### An Azure API call times out or drops the HTTP response, but the resource is created anyway

- **Symptom:** `terraform apply` fails with a network/timeout error on a specific resource, but a subsequent `terraform plan` or checking the Azure Portal shows the resource actually exists.
- **Cause:** the resource was created server-side; only the HTTP response confirming it back to Terraform was lost (network flakiness).
- **How to resolve:** don't retry `apply` blindly (it may conflict with the resource that already exists). Verify in the portal/CLI first, then bring it under Terraform's management with `terraform import <resource_address> <resource_id>` instead of recreating it.
