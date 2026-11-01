# 🌐 API Reference

🇬🇧 **English** | [🇪🇸 Español](./Api.es.md)

> HTTP endpoints exposed by the backend (ASP.NET Core Web API), what each one expects and returns, including status codes. See [Database](../Database/Database.en.md) for the underlying schema.

---

## 📋 Table of Contents

1. [General Notes](#known-gap)
2. [Categories](#-categories)
3. [Subscriptions](#-subscriptions)
4. [Price Histories](#-price-histories)
5. [Status Codes Reference](#-status-codes-reference)

---

## ℹ️ General Notes <a id="known-gap"></a>

- Base URL (local dev): `http://localhost:<port>/api` (port comes from `Properties/launchSettings.json`).
- All requests/responses use `Content-Type: application/json`.
- There is currently **no authentication** — the app is local-first and single-tenant (see [Project Scope](../ProjectScope/ProjectScope.en.md)).
- ⚠️ **Known gap:** there is no global exception-handling middleware or DTO validation attributes yet. Domain guard clauses (e.g. an empty `name`) throw a plain `ArgumentException`, which is unhandled and currently surfaces as `500 Internal Server Error` instead of a proper `400 Bad Request`. This is called out per-endpoint below and should be fixed later (validation attributes + an exception-handling middleware returning `ProblemDetails`).

---

## 📁 Categories

### `GET /api/Categories`

Returns all categories.

**Response `200 OK`**
```json
[
  {
    "id": 1,
    "name": "Streaming",
    "createdAt": "2026-11-01T10:00:00Z"
  }
]
```

---

### `GET /api/Categories/{id}`

Returns a single category by id.

**Response `200 OK`**
```json
{
  "id": 1,
  "name": "Streaming",
  "createdAt": "2026-11-01T10:00:00Z"
}
```

**Response `404 Not Found`** — no category exists with that `id` (empty body).

---

### `POST /api/Categories`

Creates a category.

**Request body**
```json
{
  "name": "Streaming"
}
```

**Response `201 Created`**
Header `Location: /api/Categories/{id}`
```json
{
  "id": 1,
  "name": "Streaming",
  "createdAt": "2026-11-01T10:00:00Z"
}
```

**Response `500 Internal Server Error`** — if `name` is empty/whitespace (see [known gap](#known-gap) above; should be `400 Bad Request`).

---

### `PUT /api/Categories/{id}`

Renames an existing category.

**Request body**
```json
{
  "name": "Streaming & VOD"
}
```

**Response `204 No Content`** — update succeeded, no body.

**Response `404 Not Found`** — no category exists with that `id`.

**Response `500 Internal Server Error`** — if `name` is empty/whitespace (see [known gap](#known-gap) above).

---

### `DELETE /api/Categories/{id}`

Deletes a category.

**Response `204 No Content`** — delete succeeded, no body.

**Response `404 Not Found`** — no category exists with that `id`.

**Response `500 Internal Server Error`** — if the category still has subscriptions assigned to it. The database enforces `ON DELETE RESTRICT` on `Subscriptions.CategoryId` (see [Database](../Database/Database.en.md#-relationships)), and the controller doesn't catch that yet — it surfaces as an unhandled `DbUpdateException` instead of a clean `409 Conflict`.

---

## 📁 Subscriptions

🚧 **Pending implementation.** `SubscriptionsController` exists as an empty file — no endpoints yet.

---

## 📁 Price Histories

🚧 **Pending implementation.** `PriceHistoriesController` exists as an empty file — no endpoints yet.

---

## 🔢 Status Codes Reference

| Code | Meaning | Used when |
|---|---|---|
| `200 OK` | Success, body returned | `GET` requests that find data. |
| `201 Created` | Resource created | Successful `POST`. Includes a `Location` header pointing to the new resource. |
| `204 No Content` | Success, no body | Successful `PUT`/`DELETE`. |
| `404 Not Found` | Resource doesn't exist | `GET`/`PUT`/`DELETE` with an `id` that isn't in the database. |
| `500 Internal Server Error` | Unhandled exception | Currently also covers validation and FK-constraint failures — see the [known gap](#known-gap). |

---

<p align="center"><sub>Last updated: 2026-11-01</sub></p>
