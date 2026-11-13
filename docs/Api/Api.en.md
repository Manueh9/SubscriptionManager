# 🌐 API Reference

🇬🇧 **English** | [🇪🇸 Español](./Api.es.md)

> HTTP endpoints exposed by the backend (ASP.NET Core Web API), what each one expects and returns, including status codes. See [Database](../Database/Database.en.md) for the underlying schema.

---

## 📋 Table of Contents

1. [General Notes](#general-notes)
2. [Categories](#-categories)
3. [Subscriptions](#-subscriptions)
4. [Price Histories](#-price-histories)
5. [Status Codes Reference](#-status-codes-reference)

---

## ℹ️ General Notes <a id="general-notes"></a>

- Base URL (local dev): `http://localhost:<port>/api` (port comes from `Properties/launchSettings.json`).
- All requests/responses use `Content-Type: application/json`.
- There is currently **no authentication** — the app is local-first and single-tenant (see [Project Scope](../ProjectScope/ProjectScope.en.md)).
- ✅ Two global exception handlers (registered via `app.UseExceptionHandler()`) turn expected failures into proper status codes instead of a raw `500`: `ArgumentExceptionHandler` maps domain guard-clause failures (`ArgumentException`/`ArgumentOutOfRangeException` — an empty `name`, a negative `price`, changing a price to the same value) to `400 Bad Request`; `DbUpdateExceptionHandler` maps database constraint violations (`DbUpdateException` — e.g. deleting a category that still has subscriptions) to `409 Conflict`. Both return a `ProblemDetails` body.
- ✅ Duplicate-name conflicts (`409`) are already handled safely, including under concurrent requests: the controller pre-checks for a duplicate, and the database's unique constraint (caught by `DbUpdateExceptionHandler` if two requests race past that check) is the safety net — either path returns `409`, never a raw `500`.

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

**Response `409 Conflict`** — a category with that `name` already exists (case-insensitive).

**Response `400 Bad Request`** — if `name` is empty/whitespace.

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

**Response `409 Conflict`** — another category with that `name` already exists (case-insensitive).

**Response `400 Bad Request`** — if `name` is empty/whitespace.

---

### `DELETE /api/Categories/{id}`

Deletes a category.

**Response `204 No Content`** — delete succeeded, no body.

**Response `404 Not Found`** — no category exists with that `id`.

**Response `409 Conflict`** — if the category still has subscriptions assigned to it. The database enforces `ON DELETE RESTRICT` on `Subscriptions.CategoryId` (see [Database](../Database/Database.en.md#-relationships)); the resulting `DbUpdateException` is mapped to `409` by `DbUpdateExceptionHandler` (see [General Notes](#general-notes)).

---

## 📁 Subscriptions

### `GET /api/Subscriptions`

Returns all subscriptions.

**Response `200 OK`**
```json
[
  {
    "id": 1,
    "name": "Netflix",
    "description": "Streaming service",
    "price": 12.99,
    "billingCycle": 1,
    "startDate": "2026-01-01T00:00:00Z",
    "endDate": null,
    "createdAt": "2026-11-01T10:00:00Z",
    "updatedAt": "2026-11-01T10:00:00Z",
    "status": 0,
    "categoryId": 1
  }
]
```
> `billingCycle` and `status` are enums serialized as plain integers — see [Database: Enums Reference](../Database/Database.en.md#-enums-reference) for the value mapping.

---

### `GET /api/Subscriptions/{id}`

Returns a single subscription by id.

**Response `200 OK`** — same shape as above (a single object).

**Response `404 Not Found`** — no subscription exists with that `id`.

---

### `POST /api/Subscriptions`

Creates a subscription.

**Request body**
```json
{
  "name": "Netflix",
  "description": "Streaming service",
  "price": 12.99,
  "billingCycle": 1,
  "startDate": "2026-01-01T00:00:00Z",
  "categoryId": 1
}
```

**Response `201 Created`**
Header `Location: /api/Subscriptions/{id}`
```json
{
  "id": 1,
  "name": "Netflix",
  "description": "Streaming service",
  "price": 12.99,
  "billingCycle": 1,
  "startDate": "2026-01-01T00:00:00Z",
  "endDate": null,
  "createdAt": "2026-11-01T10:00:00Z",
  "updatedAt": "2026-11-01T10:00:00Z",
  "status": 0,
  "categoryId": 1
}
```

**Response `400 Bad Request`** — `categoryId` doesn't match an existing category, or `name` is empty/whitespace, or `price` is negative.

**Response `409 Conflict`** — a subscription with that `name` already exists in that category (case-insensitive).

---

### `PUT /api/Subscriptions/{id}`

Updates a subscription's details. **`price` is intentionally not editable here** — it's meant to change only through `PUT /api/Subscriptions/{id}/price` below, so every change gets recorded in [Price Histories](#-price-histories).

**Request body**
```json
{
  "name": "Netflix Premium",
  "description": "Updated plan",
  "billingCycle": 1,
  "startDate": "2026-01-01T00:00:00Z",
  "endDate": null,
  "categoryId": 1
}
```

**Response `204 No Content`** — update succeeded, no body.

**Response `404 Not Found`** — no subscription exists with that `id`.

**Response `400 Bad Request`** — `categoryId` doesn't match an existing category, or `name` is empty/whitespace.

**Response `409 Conflict`** — another subscription with that `name` already exists in that category.

---

### `DELETE /api/Subscriptions/{id}`

Deletes a subscription. Its `PriceHistory` records are deleted along with it (`ON DELETE CASCADE`, see [Database](../Database/Database.en.md#-relationships)).

**Response `204 No Content`** — delete succeeded, no body.

**Response `404 Not Found`** — no subscription exists with that `id`.

---

### `PUT /api/Subscriptions/{id}/price`

Changes a subscription's price and records the change as a new entry in [Price Histories](#-price-histories). This is the **only** way to change `price` — the general `PUT /api/Subscriptions/{id}` above intentionally rejects it.

**Request body**
```json
{
  "newPrice": 14.99
}
```

**Response `200 OK`** — the subscription with its updated `price` and `updatedAt`.
```json
{
  "id": 1,
  "name": "Netflix",
  "description": "Streaming service",
  "price": 14.99,
  "billingCycle": 1,
  "startDate": "2026-01-01T00:00:00Z",
  "endDate": null,
  "createdAt": "2026-11-01T10:00:00Z",
  "updatedAt": "2026-11-04T10:00:00Z",
  "status": 0,
  "categoryId": 1
}
```

**Response `404 Not Found`** — no subscription exists with that `id`.

**Response `400 Bad Request`** — if `newPrice` is negative or equal to the current price.

---

## 📁 Price Histories

**Read-only from the API's point of view.** Records are created automatically by `PUT /api/Subscriptions/{id}/price` above — there's no direct `POST`/`PUT`/`DELETE` on this resource.

### `GET /api/PriceHistories`

Returns all price history records, optionally filtered by subscription.

**Query parameters**

| Param | Type | Description |
|---|---|---|
| `subscriptionId` | `int` (optional) | Only return records belonging to this subscription. |

**Response `200 OK`**
```json
[
  {
    "id": 1,
    "subscriptionId": 1,
    "oldPrice": 9.99,
    "newPrice": 12.99,
    "changedAt": "2026-11-01T10:00:00Z"
  }
]
```

---

### `GET /api/PriceHistories/{id}`

Returns a single price history record by id.

**Response `200 OK`** — same shape as above (a single object).

**Response `404 Not Found`** — no record exists with that `id`.

---

## 🔢 Status Codes Reference

| Code | Meaning | Used when |
|---|---|---|
| `200 OK` | Success, body returned | `GET` requests that find data, and `PUT /api/Subscriptions/{id}/price` (returns the updated subscription instead of `204`, so the client sees the new price without a follow-up `GET`). |
| `201 Created` | Resource created | Successful `POST`. Includes a `Location` header pointing to the new resource. |
| `204 No Content` | Success, no body | Successful `PUT`/`DELETE` (except `PUT .../price`, see above). |
| `400 Bad Request` | Invalid reference or domain guard-clause failure | `POST`/`PUT` on `Subscriptions` with a `categoryId` that doesn't exist, an empty `name`, a negative `price`, or a price change to the same/negative value. |
| `404 Not Found` | Resource doesn't exist | `GET`/`PUT`/`DELETE` with an `id` that isn't in the database. |
| `409 Conflict` | Duplicate resource, or a database constraint violation | `POST`/`PUT` on `Categories`/`Subscriptions` with a name already in use (case-insensitive) within that scope, or `DELETE /api/Categories/{id}` on a category that still has subscriptions. |
| `500 Internal Server Error` | Genuinely unhandled exception | Not expected during normal use — every previously-known gap is now mapped to `400`/`409` by the exception handlers described in [General Notes](#general-notes). |

---

<p align="center"><sub>Last updated: 2026-11-13</sub></p>
