# 📜 Changelog

🇬🇧 **English** | [🇪🇸 Español](./CHANGELOG.es.md)

All notable changes to this project are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versioning follows [Semantic Versioning](https://semver.org/).

**Versioning scheme for this project**: while there's no end-to-end usable product yet (no frontend), versions stay `0.x.y`. **`1.0.0` is reserved for the first version a person can actually use** — see [Roadmap](docs/Roadmap/Roadmap.en.md) for what's still needed to get there. Within `0.x`, a new `0.x.0` marks a meaningful milestone (e.g. a Roadmap phase shipping); `0.x.y` patch bumps are for smaller fixes.

---

## [Unreleased]

Nothing yet — see the [Roadmap](docs/Roadmap/Roadmap.en.md) for what's next.

## [0.1.0] — 2026-11-20 — Backend MVP

First versioned snapshot. This isn't "everything built on this date" — it's the baseline this changelog starts tracking from, covering everything built since the project started. **The app is backend-only at this point: a REST API with no user interface.** It can be exercised via the `.http` files, the automated test suite, or any HTTP client — not yet by a person through a UI.

### Added

**Categories** (`/api/Categories`)
- Create, list, get by id, rename, delete.
- Names are unique, case-insensitive, within the whole app.
- Deleting a category that still has subscriptions is rejected (`409`), not silently allowed or crashed on.

**Subscriptions** (`/api/Subscriptions`)
- Create, list, get by id, update, delete.
- Each subscription tracks: name, description, price, billing cycle (`Day` / `Monthly` / `Yearly`), start date, optional end date, status (`Active` / `Paused` / `Cancelled`), and its category.
- Names are unique per category (case-insensitive), not globally — the same service name is allowed in two different categories.
- `price` and `status` are **not** editable through the general update — they go through their own dedicated endpoints below, so every change is deliberate and (for price) recorded.
- `PUT /api/Subscriptions/{id}/price` — changes the price and automatically records the change in the price history. Rejects a negative price or "changing" to the same price.
- `PUT /api/Subscriptions/{id}/status` — changes the status. Rejects changing to the status it's already in.
- `GET /api/Subscriptions/upcoming` — active subscriptions whose next renewal falls within a configurable window (`withinDays`, default 30 days), ordered soonest-first. Correctly handles edge cases like a subscription starting on the 31st of a month, or on February 29th, without the renewal date drifting over time.

**Price Histories** (`/api/PriceHistories`, read-only)
- List (optionally filtered to one subscription) and get by id.
- Records are created automatically by the price-change endpoint above — there's no direct way to create, edit or delete one.
- Deleted automatically when their subscription is deleted.

**Expenses** (`/api/Expenses/summary`, read-only)
- Total monthly cost, total yearly cost, and count of active subscriptions, across every active subscription.
- Paused and cancelled subscriptions don't count towards it.

**Data integrity & error handling**
- Every enum field (`billingCycle`, `status`) rejects undefined values with a `400`, before the request ever reaches the domain logic.
- Domain validation failures (an empty name, a negative price, a redundant price/status change) consistently return `400 Bad Request`.
- A genuine data conflict (a duplicate name, or deleting a category still in use) consistently returns `409 Conflict` — including when it only shows up as a database-level race between two near-simultaneous requests, not just the common case.
- Any other, truly unexpected server-side failure stays a real `500` — it's never disguised as a `400`/`409` it isn't.

**Under the hood**
- ASP.NET Core Web API (.NET 10) + Entity Framework Core + SQLite, entirely local — no cloud dependency, no authentication (by design, see [Project Scope](docs/ProjectScope/ProjectScope.en.md)).
- 56 automated tests (xUnit) covering each endpoint's documented edge cases, plus the trickier pieces (renewal-date math, the exception handlers) in isolation.

### Not included yet

So this version's scope is unambiguous:

- No frontend of any kind — see the [Roadmap](docs/Roadmap/Roadmap.en.md).
- No authentication — intentional, not a gap (this is a local-first, single-user app).
- No CORS configuration — not needed until a frontend exists to call the API from a browser.

---

<p align="center"><sub>Format: Keep a Changelog · Versioning: Semantic Versioning</sub></p>
