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

The project already has `backend/src/SubscriptionManager.API/SubscriptionManager.API.http`, organized in sections, with ready-made requests for `Categories`, `Subscriptions` (including the price-change endpoint) and the read-only `PriceHistories` — covering the happy path plus the documented `404`/`400`/`409` cases.

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

It currently covers the happy paths for `Categories`, `Subscriptions` and `PriceHistories`, plus the `400`/`404`/`409` edge cases documented in the [API Reference](../Api/Api.en.md) — including regression checks for both global exception handlers (`ArgumentExceptionHandler` for the empty-name/negative-price/same-price guard clauses, and `DbUpdateExceptionHandler` for deleting a category that still has subscriptions). See [General Notes](../Api/Api.en.md#general-notes) in the API Reference for details.

---

## ✅ Suggested Test Sequence

This mirrors the flow already verified while building the API — a good order to click through manually, copying each `id` from one response into the next request:

1. `POST /api/Categories` → create "Streaming". Expect `201`.
2. `POST /api/Categories` again with the **same name** (try different casing too, e.g. "streaming"). Expect `409`.
3. `POST /api/Subscriptions` → create "Netflix" using the category id from step 1. Expect `201`.
4. `POST /api/Subscriptions` again with the same name **and** category. Expect `409`.
5. `POST /api/Subscriptions` with the same name but a **different** category id (create a second category first). Expect `201` — allowed, since uniqueness is per category.
6. `GET /api/Subscriptions/{id}` with the id from step 3. Expect `200`.
7. `PUT /api/Subscriptions/{id}` → change the name/dates (price is intentionally not editable here). Expect `204`.
8. `PUT /api/Subscriptions/{id}/price` with `{ "newPrice": 14.99 }` → expect `200` with the updated subscription.
9. `PUT /api/Subscriptions/{id}/price` again with the **same** price → expect `400`.
10. `GET /api/PriceHistories?subscriptionId={id}` → expect `200` with **one** record showing the old/new price from step 8.
11. `DELETE /api/Categories/{id}` using the id from step 1, while the subscription from step 3 still exists → expect `409`.
12. `DELETE /api/Subscriptions/{id}`. Expect `204`.
13. `DELETE /api/Categories/{id}` (the one from step 1, now with no subscriptions left). Expect `204`.
14. `GET /api/Categories/999999` (an id that doesn't exist). Expect `404`.
15. `POST /api/Categories` with `"name": ""`. Expect `400`.

---

<p align="center"><sub>Last updated: 2026-11-13</sub></p>
