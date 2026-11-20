# 🗺️ Roadmap

🇬🇧 **English** | [🇪🇸 Español](./Roadmap.es.md)

---

## 📋 Table of Contents

1. [Status at a Glance](#-status-at-a-glance)
2. [Phase 0 — Backend (MVP)](#-phase-0--backend-mvp)
3. [Phase 1 — Backend: Card Visual Identity](#-phase-1--backend-card-visual-identity)
4. [Phase 2 — Frontend Scaffold](#-phase-2--frontend-scaffold)
5. [Phase 3 — Static Dashboard](#-phase-3--static-dashboard)
6. [Phase 4 — Animation & Interaction Layer](#-phase-4--animation--interaction-layer)
7. [Phase 5 — Remaining Screens](#-phase-5--remaining-screens)
8. [Phase 6 — CORS](#-phase-6--cors)
9. [Decisions Already Made](#-decisions-already-made)
10. [Open Questions](#-open-questions)

---

## 🚦 Status at a Glance

| Phase | What | Status |
|---|---|---|
| 0 | Backend MVP — `Categories`/`Subscriptions` CRUD, price changes with history, status changes, upcoming-renewals view, monthly/yearly expense summary, centralized error handling, 56 automated tests (see [Project Status](../../README.md#-project-status) and the [API Reference](../Api/Api.en.md)) | ✅ Done |
| 1 | Backend additions for the card design (`AccentColor`, `Icon`, `Pattern`, computed equivalents) | 🔲 Not started |
| 2 | Frontend project scaffold (Vite + React + TS + Tailwind + Framer Motion) | 🔲 Not started |
| 3 | Dashboard with real data, no animation yet | 🔲 Not started |
| 4 | Animation & interaction layer on the dashboard | 🔲 Not started |
| 5 | Subscriptions list, detail/edit, create flow, category management | 🔲 Not started |
| 6 | CORS, once the real Vite dev port is known | 🔲 Not started |

---

## 🧱 Phase 0 — Backend (MVP)

Everything below is already built, tested and documented — kept here (instead of trimmed to the one-line summary in the table above) so the roadmap shows the full picture, not just what's left.

**Domain & endpoints** — 16 endpoints across 4 controllers:
- [x] `Categories` — list, get by id, create, rename, delete.
- [x] `Subscriptions` — list, get by id, create, update, delete.
- [x] `PUT /api/Subscriptions/{id}/price` — changes the price and records a `PriceHistory` entry; `PUT /api/Subscriptions/{id}` intentionally can't touch price.
- [x] `PUT /api/Subscriptions/{id}/status` — Active/Paused/Cancelled, rejecting a no-op change.
- [x] `GET /api/Subscriptions/upcoming` — active subscriptions whose next renewal falls within a window, computed with correct date math (no drift across short months or leap years).
- [x] `PriceHistories` — read-only, list (optionally filtered by subscription) + get by id.
- [x] `GET /api/Expenses/summary` — monthly/yearly totals and active count.
- [x] Case-insensitive unique names: per category globally, per subscription within its category (SQLite `NOCASE` collation).
- [x] `BillingCycle`/`SubscriptionStatus` validated against their defined enum values (`[EnumDataType]`) — an undefined integer is rejected before it reaches the domain layer.

**Robustness**:
- [x] Two global `IExceptionHandler`s: domain guard-clause failures → `400`; genuine database constraint violations → `409`. A transient DB error (e.g. a lock) is deliberately *not* reported as a conflict — it surfaces as a real `500` instead of lying to the client.
- [x] That same constraint-violation check is shared between the global handler and the controllers' own duplicate-name race handling (`TrySaveChangesAsync`), so there's one definition of "this is a real conflict," not four copies.
- [x] Read-only queries use `AsNoTracking()`.

**Persistence**:
- [x] EF Core + SQLite, 2 migrations applied, schema fully documented.

**Testing**:
- [x] 56 automated tests (xUnit) across 8 files: HTTP-level integration tests per controller, plus pure unit tests for the renewal-date math and the exception handlers.
- [x] Two `.http` files for manual testing (the main one, plus a dedicated one exhaustively covering `/upcoming`'s edge cases).

## 🧩 Phase 1 — Backend: Card Visual Identity

The chosen dashboard design (see [Design: Dashboard](../Design/Dashboard.es.md)) needs three fields the current domain model doesn't have, plus two values worth computing server-side instead of duplicating the formula in the frontend.

- [ ] `Subscription.AccentColor` (hex string, e.g. `"#E12D37"`) — the card's per-service accent color, chosen by the user.
- [ ] `Subscription.Icon` (new C# enum, mirroring `BillingCycle`/`SubscriptionStatus`) — a small closed set of generic icons (`Play`, `Music`, `Terminal`, `Cloud`, `Gym`, `Wifi`, `Code`, `Box`, ...).
- [ ] `Category.Pattern` (new C# enum) — the card's background engraving pattern. Auto-assigned round-robin when a category is created, editable afterward via the existing `PUT /api/Categories/{id}`.
- [ ] Add `monthlyEquivalent` / `yearlyEquivalent` to `SubscriptionResponse` (and `UpcomingRenewalResponse`), computed with the exact same formula `ExpensesController` already uses — so the wallet's totals can never drift from `GET /api/Expenses/summary`.
- [ ] New EF Core migration for the two new columns + the `Pattern` column.
- [ ] Update `SubscriptionCreateRequest`/`SubscriptionUpdateRequest` to accept `accentColor`/`icon`; update `CategoryRequest` to accept an optional `pattern`.
- [ ] Update [API Reference](../Api/Api.en.md) and [Database](../Database/Database.en.md) once these ship — not before (they document what exists, not what's planned).

No new aggregation endpoint is needed: the dashboard's category chart and per-category totals are computed client-side from `GET /api/Subscriptions` + `GET /api/Categories` — the data volume (one person's subscriptions) makes that trivial.

## 🏗 Phase 2 — Frontend Scaffold

- [ ] `npm create vite@latest frontend -- --template react-ts` inside the existing `frontend/` folder.
- [ ] Tailwind CSS + the design tokens from [Design: Dashboard](../Design/Dashboard.es.md) §2 (the "medianoche" theme).
- [ ] Framer Motion.
- [ ] A typed API client for the existing backend endpoints.

## 🖥 Phase 3 — Static Dashboard

Build the dashboard with real data end to end, animation deferred — header, period toggle (no sliding pill yet), total, the 3-stat grid, the category chart (static, no hover), the stacked wallet (static, no tilt/flip). The goal: a dashboard that's functionally and visually complete, just not yet "alive."

## 🎬 Phase 4 — Animation & Interaction Layer

Layered in on top of Phase 3, roughly in order of payoff: entrance stagger → toggle slide + animated counters → card flip on click → hover tilt/glare/wallet-expand → chart hover dimming + center label swap. Full timing/easing spec in [Design: Dashboard](../Design/Dashboard.es.md) §5.

## 📱 Phase 5 — Remaining Screens

- [ ] Subscriptions list — filterable/sortable by category and status.
- [ ] Subscription detail/edit — including its price-history timeline, status change (pause/cancel/reactivate), and price change.
- [ ] Create-subscription flow — with a frontend-only "known services" preset catalog (name → default color/icon, e.g. Netflix → red/play) that grows over time without touching the backend; the user can always override the suggestion.
- [ ] Category management — list/create/rename/delete, with the pattern picker.

None of these screens are designed yet — each one gets its own file under `docs/Design/` once it is, same as [Dashboard.es.md](../Design/Dashboard.es.md).

## 🌐 Phase 6 — CORS

`Program.cs` has no `AddCors`/`UseCors` yet. Added once Phase 2 is underway and the real Vite dev server port is known (defaults to `5173`, but confirm).

---

## ✅ Decisions Already Made

Judgment calls made while planning this, so they don't get re-litigated later:

- **Color storage**: hex string (`"#E12D37"`), not the design prototype's raw RGB-triplet strings — hex is the web standard and works directly with `<input type="color">`.
- **Icon**: a closed C# enum, not a free string — consistent with how `BillingCycle`/`SubscriptionStatus` are already modeled, and validated the same way.
- **Category pattern**: a closed C# enum, auto-assigned round-robin on creation — the user never has to think about it, but can change it.
- **Monthly/yearly equivalents**: computed server-side and included in the subscription response, not duplicated as a formula in the frontend — guarantees the dashboard and `/api/Expenses/summary` can never disagree.
- **`BillingCycle.Day`**: handled with the same conversion `ExpensesController` already uses (`× 365` yearly), not a new one invented for the dashboard.
- **Wallet "no window cap"**: solved by calling `GET /api/Subscriptions/upcoming` with a very large `withinDays` from the frontend — no backend change, simplest option for now.
- **"Known services" preset catalog**: lives entirely in the frontend as a static lookup table — a starter list of ~20 common services, each with a suggested icon and accent color, is already defined in [Design: Dashboard](../Design/Dashboard.es.md) §7. Growing it over time never requires a backend change or deployment.
- **Theme**: building only "medianoche" (dark) for v1. The other three themes from the design canvas (`esmeralda`, `burdeos`, `marfil`) are documented in [Design: Dashboard](../Design/Dashboard.es.md) §9 in case a theme switcher is added later, but are out of scope for now.
- **Reference docs stay factual**: `Api.md`/`Database.md` only describe what's implemented, never what's planned — that's what this file and `docs/Design/` are for.

## ❓ Open Questions

Not blocking Phase 1, but will need an answer before the relevant component is built:

- Category chart color scheme for more than 4 categories (cycle the palette? generate shades? group the rest into "Other"?) — see [Design: Dashboard](../Design/Dashboard.es.md) §4.5.
- How paused/cancelled subscriptions are presented — not covered by the current dashboard design at all.
- What happens once the wallet grows past ~12 cards (the stack currently just keeps growing by 60px each).

---

<p align="center"><sub>Last updated: 2026-11-15</sub></p>
