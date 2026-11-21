# 📦 Subscription Manager

🇬🇧 **English** | [🇪🇸 Español](./README.es.md)

> Local-first subscription management application to track recurring payments, manage subscriptions, monitor price changes and visualize monthly and yearly expenses. Runs entirely on your own machine — no cloud infrastructure required.

[Application Features](docs/Features/Features.en.md) details the app's current capabilities in plain language.

---

## 🛠 Tech Stack

| Layer | Technology |
|---|---|
| Backend / API | ASP.NET Core Web API (.NET 10), C# |
| Persistence | Entity Framework Core + SQLite |
| Frontend | React, TypeScript, Vite, Tailwind CSS, Framer Motion |
| Testing | xUnit |

## 🚦 Project Status

**Current version: `0.1.0`** — see the [Changelog](CHANGELOG.md) for exactly what that includes. `1.0.0` is reserved for the first version usable by the user.

- ✅ **Backend API** — `Categories` and `Subscriptions` CRUD, price changes with history (`PriceHistories`), subscription status changes, an upcoming-renewals view, and a monthly/yearly expense summary. See the [API Reference](docs/Api/Api.en.md) for the full endpoint list.
- 🚧 **Frontend** — not started yet. See the [Roadmap](docs/Roadmap/Roadmap.en.md) for what's planned, in order, and the chosen [dashboard design](docs/Design/Dashboard.es.md).

## ▶️ Getting Started

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

**Run the API** (from `backend/src/SubscriptionManager.API`):
```
dotnet run
```
The console prints the URL it's listening on (e.g. `http://localhost:5166`).

**Run the automated tests** (from `backend/tests/SubscriptionManager.API.Tests`):
```
dotnet test
```
See the [Testing Guide](docs/Testing/Testing.en.md) for how to exercise the endpoints manually.

## 📚 Documentation

| Doc | 🇬🇧 English | 🇪🇸 Español |
|---|---|---|
| Application Features | [English](docs/Features/Features.en.md) | [Español](docs/Features/Features.es.md) |
| V1 Specification | — | [Español](docs/Specification/SpecificationV1.es.md) |
| V1 Features (Summary) | [English](docs/Features/FeaturesV1.en.md) | [Español](docs/Features/FeaturesV1.es.md) |
| Changelog | [English](CHANGELOG.md) | [Español](CHANGELOG.es.md) |
| Project Scope | [English](docs/ProjectScope/ProjectScope.en.md) | [Español](docs/ProjectScope/ProjectScope.es.md) |
| Roadmap | [English](docs/Roadmap/Roadmap.en.md) | [Español](docs/Roadmap/Roadmap.es.md) |
| Architecture & Request Lifecycle | [English](docs/Architecture/Architecture.en.md) | [Español](docs/Architecture/Architecture.es.md) |
| Database | [English](docs/Database/Database.en.md) | [Español](docs/Database/Database.es.md) |
| API Reference | [English](docs/Api/Api.en.md) | [Español](docs/Api/Api.es.md) |
| Testing Guide | [English](docs/Testing/Testing.en.md) | [Español](docs/Testing/Testing.es.md) |
| Design — Dashboard | — | [Español](docs/Design/Dashboard.es.md) |
