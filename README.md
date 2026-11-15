# 📦 Subscription Manager

🇬🇧 **English** | [🇪🇸 Español](./README.es.md)

> Local-first subscription management application to track recurring payments, manage subscriptions, monitor price changes and visualize monthly and yearly expenses. Runs entirely on your own machine — no cloud infrastructure required.

---

## 🛠 Tech Stack

| Layer | Technology |
|---|---|
| Backend / API | ASP.NET Core Web API (.NET 10), C# |
| Persistence | Entity Framework Core + SQLite |
| Frontend | React, TypeScript, Vite, Tailwind CSS |
| Testing | xUnit |

## 🚦 Project Status

- ✅ **Backend API** — complete for the MVP scope: `Categories` and `Subscriptions` CRUD, price changes with history (`PriceHistories`), subscription status changes, an upcoming-renewals view, and a monthly/yearly expense summary. See the [API Reference](docs/Api/Api.en.md) for the full endpoint list.
- 🚧 **Frontend** — not started yet.

## ▶️ Getting Started

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

**Run the API** (from `backend/src/SubscriptionManager.API`):
```
dotnet run
```
The console prints the URL it's listening on (e.g. `http://localhost:5166`). See the [Testing Guide](docs/Testing/Testing.en.md) for how to exercise the endpoints manually.

**Run the automated tests** (from `backend/tests/SubscriptionManager.API.Tests`):
```
dotnet test
```

## 📚 Documentation

| Doc | 🇬🇧 English | 🇪🇸 Español |
|---|---|---|
| Project Scope | [English](docs/ProjectScope/ProjectScope.en.md) | [Español](docs/ProjectScope/ProjectScope.es.md) |
| Database | [English](docs/Database/Database.en.md) | [Español](docs/Database/Database.es.md) |
| API Reference | [English](docs/Api/Api.en.md) | [Español](docs/Api/Api.es.md) |
| Testing Guide | [English](docs/Testing/Testing.en.md) | [Español](docs/Testing/Testing.es.md) |
