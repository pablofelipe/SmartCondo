# Functional validation walkthrough

A practical, end-to-end pass through the system's main flows: registering users under different roles, authenticating, creating resources, receiving a real-time notification, and confirming error responses. Assumes the stack is already running — see [getting-started.md](getting-started.md) — and that you've already applied migrations and created the admin account.

Exact request/response schemas are in Swagger (`/swagger`) or [docs/api/rest-api.md](../api/rest-api.md); this walkthrough shows the DTO fields that matter to keep each step working, not the full schema.

## 1. Log in as the system administrator

```bash
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"user": "<ADMIN_EMAIL>", "secret": "<ADMIN_PASSWORD>"}'
```

Save the returned `token`. Every authenticated request below sends it as `Authorization: Bearer <token>`.

## 2. Create a condominium

```bash
curl -X POST http://localhost:5000/api/v1/condominium \
  -H "Authorization: Bearer <ADMIN_TOKEN>" -H "Content-Type: application/json" \
  -d '{"name": "Sunset Towers", "address": "1 Main St", "towerCount": 1, "maxUsers": 50, "enabled": true}'
```

Note the returned `id` (`<CONDOMINIUM_ID>`).

## 3. Create a tower

```bash
curl -X POST http://localhost:5000/api/v1/tower \
  -H "Authorization: Bearer <ADMIN_TOKEN>" -H "Content-Type: application/json" \
  -d '{"number": 1, "name": "Tower A", "condominiumId": <CONDOMINIUM_ID>, "floorCount": 10}'
```

## 4. Register a condominium administrator (second role)

```bash
curl -X POST http://localhost:5000/api/v1/userprofile \
  -H "Authorization: Bearer <ADMIN_TOKEN>" -H "Content-Type: application/json" \
  -d '{
    "name": "Alice Manager", "address": "1 Main St", "phone1": "555-0100",
    "registrationNumber": "REG-001", "userTypeId": 2, "condominiumId": <CONDOMINIUM_ID>,
    "user": {"email": "alice@example.com", "password": "Passw0rd!", "expiration": "2030-01-01T00:00:00Z", "enabled": true}
  }'
```

`userTypeId: 2` is `CondominiumAdministrator` (the seeded `UserType` catalog — `SystemAdministrator=1`, `CondominiumAdministrator=2`, `Resident=3`, ...; see `Models/SmartCondoContext.cs` for the full list). Which `userTypeId` values an actor is allowed to register is enforced by `Models/Permissions/RolePermissions.cs` — a disallowed combination returns 403.

## 5. Confirm the new user's e-mail

New accounts start with `EmailConfirmed = false` and can't log in until confirmed. `UserProfileController.AddUser` both e-mails the confirmation link (via SMTP — real credentials required for actual delivery) **and** logs `userProfile.Id` and the raw confirmation token at `Information` level, so local testing doesn't require a working mailbox:

```bash
docker compose logs backend | grep "userProfile.Id"
# userProfile.Id: 2, token: CfDJ8...
```

Confirm using those two values:

```bash
curl http://localhost:5000/api/v1/userprofile/confirm-email/2/CfDJ8...
```

## 6. Log in as the condominium administrator

Same as step 1, with Alice's credentials. Save `<ALICE_TOKEN>`.

## 7. Register a resident (third role) and confirm

Repeat steps 4–5 as Alice (`Authorization: Bearer <ALICE_TOKEN>`), with `userTypeId: 3` (`Resident`) and `condominiumId`/`towerId` set to the tower from step 3. This exercises a different actor in the role hierarchy performing the same registration action — `RolePermissions` governs what Alice (a condominium administrator) can do differently from the system administrator in step 4.

Log in as the resident once confirmed; save `<RESIDENT_TOKEN>`.

## 8. Register a vehicle (GraphQL)

```bash
curl -X POST http://localhost:5000/graphql \
  -H "Authorization: Bearer <RESIDENT_TOKEN>" -H "Content-Type: application/json" \
  -d '{"query": "mutation { createVehicle(input: {type: CAR, licensePlate: \"ABC1234\", brand: \"Honda\", model: \"Civic\", color: \"Black\", enabled: true, userId: <RESIDENT_USER_ID>}) { id licensePlate } }"}'
```

Then query it back:

```bash
curl -X POST http://localhost:5000/graphql \
  -H "Authorization: Bearer <RESIDENT_TOKEN>" -H "Content-Type: application/json" \
  -d '{"query": "{ vehicles(filter: {licensePlate: \"ABC1234\"}) { id licensePlate model color } }"}'
```

## 9. Send a message and watch it arrive over WebSocket

Open two browser sessions (or two tabs in a private window) against http://localhost:3000: one signed in as Alice, one as the resident. Both establish a WebSocket connection automatically on login (`frontend/src/pages/hooks/useWebSocket.js` → `wss://…/ws?token=<jwt>` in production, `ws://…/ws?token=<jwt>` locally).

As Alice, send a condominium-wide message:

```bash
curl -X POST http://localhost:5000/api/v1/messages \
  -H "Authorization: Bearer <ALICE_TOKEN>" -H "Content-Type: application/json" \
  -d '{"content": "Water shutoff tomorrow 9am-11am", "scope": "Condominium", "condominiumId": <CONDOMINIUM_ID>}'
```

The resident's browser session should show the notification arrive without a page refresh — this is `NativeWebSocketNotificationService` resolving recipients by scope (`MessageRecipientResolver`) and pushing over the in-process WebSocket connection, not polling.

To confirm delivery from the command line instead of a browser, open a raw WebSocket from the resident's session before sending the message:

```bash
# any WebSocket CLI works; example with websocat
websocat "ws://localhost:5000/ws?token=<RESIDENT_TOKEN>"
```

## 10. Validate error responses

All of these return `{ "message": "…" }` (see [error contract](../api/rest-api.md#error-contract)):

```bash
# 401 - no token
curl -i http://localhost:5000/api/v1/condominium

# 403 - resident attempting an admin-only action (e.g. registering another user)
curl -i -X POST http://localhost:5000/api/v1/userprofile \
  -H "Authorization: Bearer <RESIDENT_TOKEN>" -H "Content-Type: application/json" -d '{}'

# 404 - condominium that doesn't exist
curl -i -X GET http://localhost:5000/api/v1/condominium/999999 \
  -H "Authorization: Bearer <ADMIN_TOKEN>"
```

A 400 from malformed JSON or a missing required field instead returns ASP.NET Core's automatic `ValidationProblemDetails` body (`{ "errors": {...}, "title", "status" }`), not `{ "message": ... }` — both are documented, see the error contract link above.
