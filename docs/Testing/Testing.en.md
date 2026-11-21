# 🧪 Manual API Testing Guide

🇬🇧 **English** | [🇪🇸 Español](./Testing.es.md)

> How to manually exercise the endpoints described in the [API Reference](../Api/Api.en.md), either with the included `.http` file or with a tool like Bruno/Postman/Insomnia.

---

## 📋 Table of Contents

1. [Start the API](#-start-the-api)
2. [Option A — The `.http` File](#-option-a--the-http-file)
3. [Option B — Bruno / Postman / Insomnia](#-option-b--bruno--postman--insomnia)
4. [Option C — Automated Tests](#-option-c--automated-tests)
5. [Suggested Test Sequence](#-suggested-test-sequence)

---

## ▶️ Start the API

From `backend/src/SubscriptionManager.API`:

```
dotnet run
```

The console prints the URL it's listening on, e.g. `http://localhost:5166`. Keep this terminal open — the server stays running until you stop it (`Ctrl+C`). All requests below assume this base URL.

---

## 📄 Option A — The `.http` File

The project already has `backend/src/SubscriptionManager.API/SubscriptionManager.API.http`, organized in sections, with ready-made requests for `Categories`, `Subscriptions` (including the price-change, status-change and upcoming-renewals endpoints), the read-only `PriceHistories`, and the read-only `Expenses` summary — covering the happy path plus the documented `404`/`400`/`409` cases.

There's a second file, `SubscriptionManager.API.UpcomingRenewals.http`, split out on its own because it's long and narrowly focused: it exhaustively exercises `GET /api/Subscriptions/upcoming` — a future start date, the `Day` cycle, a `Monthly` start date on the 31st and a `Yearly` one on Feb 29 (the exact edge cases the renewal-date calculation was fixed for), excluding paused subscriptions, excluding ones whose `EndDate` has passed, and the `withinDays` boundary.

1. Open it in **Rider** or **VS Code** (with the *REST Client* extension).
2. Click "Send Request" (or the ▶️ that appears above each block) on each `###`-separated request, top to bottom.
3. Requests that create something are named (`# @name createCategory`, `# @name createSubscription`, ...), and requests below them reuse the returned `id` automatically (e.g. `{{createCategory.response.body.$.id}}`) — so running a section in order chains create → get → update → delete without you having to copy any id by hand.

---

## 📮 Option B — Bruno / Postman / Insomnia

There's no exported collection yet, so you build requests manually:

1. Create a new request.
2. Set the method (`GET`/`POST`/`PUT`/`DELETE`) and URL, e.g. `http://localhost:5166/api/Categories`.
3. For `POST`/`PUT`, add a header `Content-Type: application/json` and paste the JSON body (see examples in the [API Reference](../Api/Api.en.md)).
4. Send, and check the status code against what's documented there.

Example — create a category:
```
POST http://localhost:5166/api/Categories
Content-Type: application/json

{ "name": "Streaming" }
```

---

## 🤖 Option C — Automated Tests

`backend/tests/SubscriptionManager.API.Tests` is an xUnit project that exercises the same endpoints through `WebApplicationFactory`, against an isolated in-memory SQLite database (no manual server start needed). From `backend/tests/SubscriptionManager.API.Tests`:

```
dotnet test
```

It currently covers the happy paths for `Categories`, `Subscriptions`, `PriceHistories` and `Expenses`, plus the `400`/`404`/`409` edge cases documented in the [API Reference](../Api/Api.en.md) — including regression checks for both global exception handlers (`ArgumentExceptionHandler` for the empty-name/negative-price/same-price/same-status guard clauses, and `DbUpdateExceptionHandler` for deleting a category that still has subscriptions), the daily/monthly/yearly cost conversion used by the expense summary, the renewal-date math (window boundaries, paused subscriptions, ended subscriptions) used by `/api/Subscriptions/upcoming`, and the `[EnumDataType]` validation that rejects an undefined `billingCycle`/`newStatus`.

Some of the test files skip HTTP entirely, because what they cover is better tested as plain input/output on an isolated piece of logic than through a full round trip:

- `SubscriptionRenewalDateTests` calls `Subscription.GetNextRenewalDate` directly — no server, no database — covering every branch (status, a future start date, each billing cycle, the day-31/Feb-29 clipping edge case, the `EndDate` cap) far more exhaustively than would be practical over HTTP.
- `DbUpdateExceptionHandlerTests` calls the handler directly with a crafted `DbUpdateException`, confirming it claims a real `SQLITE_CONSTRAINT` violation (`409`) but leaves a transient error like a locked database unhandled (so it still surfaces as `500`) — a distinction that isn't practical to trigger through a single manual HTTP request.
- `TrySaveChangesTests` exercises the controllers' shared save helper against a real in-memory SQLite database (no HTTP), so it also verifies the assumption the helper rests on: that SQLite really does report a duplicate name, a case-insensitive duplicate, and a `RESTRICT` delete as constraint violations. It deliberately uses a fresh `DbContext` per operation, mirroring the real per-request lifetime — sharing one context would let EF's change tracker pre-empt the `RESTRICT` with its own exception before SQLite ever sees the delete.
- `DatabaseMigrationServiceTests` freezes a real database (in a temp folder) on the first migration only, to confirm that starting up with a pending migration backs up the file before migrating and keeps only the 10 most recent backups — and that nothing happens when no migration is pending.
- `StartupSecurityTests` calls `StartupSecurity.EnsureLoopbackOnly` directly with various URLs, confirming it accepts `localhost`/`127.0.0.1`/`::1` and rejects anything else (`0.0.0.0`, a network IP, `*`, `+`).

`UpcomingRenewalsEndpointsTests` still includes one HTTP-level regression test for the day-31 case, confirming the controller wires the (separately, exhaustively tested) domain calculation through correctly end-to-end.

`HostFilteringTests` is deliberately HTTP-level: it checks that a real request with `Host: evil.example` gets `400` before reaching any controller, and that one with `Host: localhost` still works normally.

`ExpensesEndpointsTests` and `UpcomingRenewalsEndpointsTests` give each test its own `WebApiFactory` (and therefore its own empty database) instead of sharing one via `IClassFixture`, since both endpoints aggregate across every subscription in the database and would otherwise see data left behind by sibling tests.

---

## ✅ Suggested Test Sequence

This mirrors the flow already verified while building the API — a good order to click through manually, copying each `id` from one response into the next request:

1. `POST /api/Categories` → create "Streaming". Expect `201`.
2. `POST /api/Categories` again with the **same name** (try different casing too, e.g. "streaming"). Expect `409`.
3. `POST /api/Subscriptions` → create "Netflix" using the category id from step 1. Expect `201`.
4. `POST /api/Subscriptions` again with the same name **and** category. Expect `409`.
5. `POST /api/Subscriptions` with the same name but a **different** category id (create a second category first). Expect `201` — allowed, since uniqueness is per category.
6. `POST /api/Subscriptions` with `"billingCycle": 999` (not a defined value). Expect `400`.
7. `GET /api/Subscriptions/{id}` with the id from step 3. Expect `200`.
8. `GET /api/Subscriptions/upcoming` → expect `200`; this subscription should appear (it's `Active`, and its `startDate` should be within the default 30-day window unless you picked one further out).
9. `PUT /api/Subscriptions/{id}` → change the name/dates (price is intentionally not editable here). Expect `204`.
10. `PUT /api/Subscriptions/{id}/price` with `{ "newPrice": 14.99 }` → expect `200` with the updated subscription.
11. `PUT /api/Subscriptions/{id}/price` again with the **same** price → expect `400`.
12. `GET /api/PriceHistories?subscriptionId={id}` → expect `200` with **one** record showing the old/new price from step 10.
13. `GET /api/Expenses/summary` → expect `200`; the total should include this subscription's `14.99`/month.
14. `PUT /api/Subscriptions/{id}/status` with `{ "newStatus": 1 }` (Paused) → expect `200` with the updated subscription.
15. `PUT /api/Subscriptions/{id}/status` again with the **same** status → expect `400`.
16. `PUT /api/Subscriptions/{id}/status` with `{ "newStatus": 999 }` (not a defined value) → expect `400`.
17. `GET /api/Expenses/summary` again → expect `200`; the total should no longer include this subscription, since it's now `Paused`.
18. `GET /api/Subscriptions/upcoming` again → expect `200`; this subscription should no longer appear either, for the same reason.
19. `DELETE /api/Categories/{id}` using the id from step 1, while the subscription from step 3 still exists → expect `409`.
20. `DELETE /api/Subscriptions/{id}`. Expect `204`.
21. `DELETE /api/Categories/{id}` (the one from step 1, now with no subscriptions left). Expect `204`.
22. `GET /api/Categories/999999` (an id that doesn't exist). Expect `404`.
23. `POST /api/Categories` with `"name": ""`. Expect `400`.

---

<p align="center"><sub>Last updated: 2026-11-21</sub></p>
