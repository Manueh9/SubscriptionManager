# 🧪 Manual API Testing Guide

🇬🇧 **English** | [🇪🇸 Español](./Testing.es.md)

> How to manually exercise the endpoints described in the [API Reference](../Api/Api.en.md), either with the included `.http` file or with a tool like Bruno/Postman/Insomnia.

---

## 📋 Table of Contents

1. [Start the API](#-start-the-api)
2. [Option A — The `.http` File](#-option-a--the-http-file)
3. [Option B — Bruno / Postman / Insomnia](#-option-b--bruno--postman--insomnia)
4. [Suggested Test Sequence](#-suggested-test-sequence)

---

## ▶️ Start the API

From `backend/src/SubscriptionManager.API`:

```
dotnet run
```

The console prints the URL it's listening on, e.g. `http://localhost:5166`. Keep this terminal open — the server stays running until you stop it (`Ctrl+C`). All requests below assume this base URL.

---

## 📄 Option A — The `.http` File

The project already has `backend/src/SubscriptionManager.API/SubscriptionManager.API.http` with ready-made requests for the `Categories` endpoints (list, create, get, update, delete, a 404 case and the known empty-name 500 case).

1. Open it in **Rider** or **VS Code** (with the *REST Client* extension).
2. Click "Send Request" (or the ▶️ that appears above each block) on each `###`-separated request, top to bottom.
3. The `createCategory` request is named (`# @name createCategory`), and the requests below it reuse its returned `id` automatically (`{{createCategory.response.body.$.id}}`) — so running them in order chains create → get → update → delete without you having to copy any id by hand.

This file currently only covers `Categories`. Feel free to add your own blocks for `Subscriptions` and `PriceHistories` following the same pattern — see the [API Reference](../Api/Api.en.md) for the exact request/response shapes.

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

## ✅ Suggested Test Sequence

This mirrors the flow already verified while building the API — a good order to click through manually, copying each `id` from one response into the next request:

1. `POST /api/Categories` → create "Streaming". Expect `201`.
2. `POST /api/Categories` again with the **same name** (try different casing too, e.g. "streaming"). Expect `409`.
3. `POST /api/Subscriptions` → create "Netflix" using the category id from step 1. Expect `201`.
4. `POST /api/Subscriptions` again with the same name **and** category. Expect `409`.
5. `POST /api/Subscriptions` with the same name but a **different** category id (create a second category first). Expect `201` — allowed, since uniqueness is per category.
6. `GET /api/Subscriptions/{id}` with the id from step 3. Expect `200`.
7. `PUT /api/Subscriptions/{id}` → change the name/dates (price is intentionally not editable here). Expect `204`.
8. `GET /api/PriceHistories?subscriptionId={id}` → expect `200` with an empty list (no price changes exist yet — that feature isn't implemented).
9. `DELETE /api/Subscriptions/{id}`. Expect `204`.
10. `DELETE /api/Categories/{id}` (the one from step 1, now with no subscriptions left). Expect `204`.
11. `GET /api/Categories/999999` (an id that doesn't exist). Expect `404`.
12. `POST /api/Categories` with `"name": ""`. Expect `500` — this is the documented [known gap](../Api/Api.en.md#known-gap), not a bug you need to fix.

---

<p align="center"><sub>Last updated: 2026-11-01</sub></p>
