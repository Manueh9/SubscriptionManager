# 🗺️ Roadmap

🇬🇧 **English** | [🇪🇸 Español](./Roadmap.es.md)

---

> This plan replaces the previous one (card visual identity, frontend scaffold over the `Subscription`/`Category` model). As of 2026-11-21, the reference is the [V1 Specification](../Specification/SpecificationV1.es.md) (Spanish only), which turns the app from a subscription tracker into a tracker for every recurring household expense. The phases below are section 13 of that document.

## 📋 Table of Contents

1. [Status at a Glance](#-status-at-a-glance)
2. [Phase 0 — Base](#-phase-0--base)
3. [Phase 1 — Data Model & Migration](#-phase-1--data-model--migration)
4. [Phase 2 — Calendar & Calculation Engine](#-phase-2--calendar--calculation-engine)
5. [Phase 3 — Catalog](#-phase-3--catalog)
6. [Phase 4 — Importer](#-phase-4--importer)
7. [Phase 5 — ICS Calendar](#-phase-5--ics-calendar)
8. [Phase 6 — Backup & Wallos](#-phase-6--backup--wallos)
9. [Phase 7 — Demo Mode](#-phase-7--demo-mode)
10. [Phase 8 — Frontend](#-phase-8--frontend)
11. [Decisions Already Made](#-decisions-already-made)

---

## 🚦 Status at a Glance

| Phase | What | Status |
|---|---|---|
| 0 | Base: license/CLA, SPDX headers, DB location, startup security, `TimeProvider`, Spanish messages | 🟡 In progress |
| 1 | Data model & migration: `RecurringExpense`, `ChargeRecord`, new enums, end of `Category` | 🔲 Not started |
| 2 | Calendar & calculation engine: summary, forecast, payday, savings bucket, cancellation deadline | 🔲 Not started |
| 3 | Catalog of ~33 Spanish services | 🔲 Not started |
| 4 | Bank statement importer (Excel/CSV) | 🔲 Not started |
| 5 | ICS calendar | 🔲 Not started |
| 6 | Backup (own JSON format) and Wallos import | 🔲 Not started |
| 7 | Demo mode | 🔲 Not started |
| 8 | Frontend (the prototype's 7 screens + the undesigned minimum) | 🔲 Not started |

---

## 🧱 Phase 0 — Base

- [x] `LICENSE` (AGPL-3.0), `CLA.md`, `CONTRIBUTING.md` at the repo root; license section in the README; `THIRD-PARTY-NOTICES.md`.
- [x] SPDX header (`SPDX-License-Identifier` + `Copyright`) on all 42 existing `.cs` files.
- [ ] Configurable database location (`%AppData%/SubscriptionManager/data.db`) + automatic backup before each migration (keeping the 10 most recent).
- [ ] Listen only on `127.0.0.1`, validate the `Host` header, dev-only CORS, 10MB upload limit.
- [ ] Injected `TimeProvider` everywhere "today" matters; Spanish `ProblemDetails` messages.

## 🧩 Phase 1 — Data Model & Migration

Not started. Covers points 1-4 of the spec's section 2.1:

- [ ] New entities and enums: `RecurringExpense` (replaces `Subscription`), `ChargeRecord`, `ImportProfile`, `AppSettings`; enums `ExpenseKind`, `ExpenseStatus`, `IntervalUnit`, `AmountMode`, `NoticeUnit`, `ChargeSource`.
- [ ] Removal of `Category` (replaced by `ExpenseKind`), no uniqueness constraint on `RecurringExpense.Name`.
- [ ] EF Core migration with a prior `.db` backup, conversion of existing data, and default colors/icons per type.
- [ ] Expense CRUD, status changes, manual charges, and `PriceHistory` on amount changes.

## 🧮 Phase 2 — Calendar & Calculation Engine

Not started. Covers points 5-8: charge dates, estimated amount, annual/monthly cost, cancellation deadline, summary, 90-day forecast, payday, and savings bucket.

## 📚 Phase 3 — Catalog

Not started. Embedded catalog of ~33 Spanish services (section 10), with bank-statement recognition patterns and defaults.

## 🏦 Phase 4 — Importer

Not started. Excel/CSV reading, header and column detection, normalization, recurring-charge detection, import sessions (section 9).

## 📅 Phase 5 — ICS Calendar

Not started. `.ics` generator with charges, deadlines, and reminders (section 11.3).

## 💾 Phase 6 — Backup & Wallos

Not started. Export/restore of the own JSON backup, and import from Wallos (sections 11.1 and 11.2).

## 🎭 Phase 7 — Demo Mode

Not started. Demo mode using the test dataset, with dates shifted to "today" (section 11.4).

## 🖥 Phase 8 — Frontend

Not started. The prototype's 7 screens (Welcome, Import, Your Picture, Summary, Detail, Forecast, New Expense) plus what isn't drawn (settings menu, paused/incomplete expenses, demo banner). The [Dashboard design](../Design/Dashboard.es.md) gets adapted here with section 12.3's changes (Expense instead of Subscription, engraving by type instead of by category, etc.) — not discarded.

---

## ✅ Decisions Already Made

- **One branch per phase**: each phase (`fase-0-base`, `fase-1-modelo`, ...) merges to `main` on completion, to protect the `v0.1.0` tag while the rewrite is underway.
- **No name uniqueness on `RecurringExpense`**: more than one expense can share the same name.
- **The 7-screen prototype** is consulted once Phase 8 is reached, not before.
- **Real bank statements and Wallos JSON**: added as fixtures once Manuel provides them; Phases 4 and 6 start with synthetic fixtures.
- **Reference docs stay factual**: `Api.md`/`Database.md` only describe what's implemented; this file documents the plan.

---

<p align="center"><sub>Last updated: 2026-11-21</sub></p>
