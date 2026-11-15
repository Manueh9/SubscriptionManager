# 📦 Subscription Manager — Project Scope

🇬🇧 **English** | [🇪🇸 Español](./ProjectScope.es.md)

> Local-first subscription management application to track recurring payments, manage subscriptions, monitor price changes and visualize monthly and yearly expenses. Built with a REST API and a modern web interface, running entirely on your local machine with no cloud infrastructure required.

---

## 📋 Table of Contents

1. [Overview](#-overview)
2. [Objectives](#-objectives)
3. [Scope — MVP v1.0](#-scope--mvp-v10)
4. [Tech Stack](#-tech-stack)
5. [Architecture](#-architecture)
6. [Project Structure](#-project-structure)
7. [Out of Scope (MVP)](#-out-of-scope-mvp)

---

## 🔎 Overview

**Subscription Manager** is a local-first web application that helps users keep track of their recurring subscriptions (streaming services, software, memberships, etc.).

It provides a centralized overview of:

- Which services the user is subscribed to.
- How much each one costs and how often it's billed.
- When the next payment or renewal is due.
- How spending evolves over time (monthly / yearly).

**Local-first** means the entire application — API and data — runs on the user's own machine. There is no external cloud backend, no third-party server, and no account creation required to use the core functionality. The user's financial data never leaves their computer.

---

## 🎯 Objectives

- [x] Centralize all subscriptions in a single application.
- [x] Track recurring subscription expenses.
- [x] Monitor upcoming payments and renewal dates.
- [x] Detect and track price changes over time.
- [x] Organize subscriptions into categories.
- [x] Keep track of subscription status (active, paused, cancelled).
- [x] Provide a clear overview of monthly and yearly expenses.
- [ ] Let the user do all of the above through a web interface — not by calling the API directly.

> The first seven are implemented on the backend, each exposed as a REST endpoint (see the [API Reference](../Api/Api.en.md)). The last one is what's actually missing for the app to be usable by a person end to end — that's the frontend's job, and it hasn't started yet (see [Project Status](../../README.md#-project-status)).

---

## 📐 Scope — MVP v1.0

The first version focuses on the core domain needed to manage subscriptions. These three entities define the MVP — see [Database](../Database/Database.en.md) for their exact attributes, constraints and relationships.

| Entity | Purpose |
|---|---|
| **Subscription** | A recurring payment the user wants to track (service name, cost, billing cycle, renewal date, status). |
| **Category** | Groups subscriptions by type (e.g. Streaming, Software, Fitness) to organize and filter expenses. |
| **PriceHistory** | Records each price change of a subscription (old price, new price, date) so changes over time can be tracked. |

> The application is single-tenant and local-only, so a `User` entity is not needed for the MVP — there's no login and no multi-user data to distinguish. It may be introduced later if cloud sync or multi-user support is added (see [Out of Scope](#-out-of-scope-mvp)).

---

## 🛠 Tech Stack

| Layer | Technology |
|---|---|
| Backend / API | ASP.NET Core Web API (.NET 10), C# |
| Persistence | Entity Framework Core + SQLite |
| Frontend | React, TypeScript, Vite, Tailwind CSS |
| Testing | xUnit |

---

## 🏗 Architecture

Everything runs locally: the web interface talks to the REST API, and the API persists data to a local SQLite database. No cloud services are involved. See [Architecture & Request Lifecycle](../Architecture/Architecture.en.md) for how a request actually travels through the backend, and what each file is responsible for.

```mermaid
flowchart LR
    subgraph "User's Machine"
        FE["Frontend<br/>React + TypeScript + Vite"]
        API["Backend<br/>ASP.NET Core Web API"]
        DB[("SQLite database")]

        FE -- "REST (HTTP/JSON)" --> API
        API -- "EF Core" --> DB
    end
```

---

## 📁 Project Structure

```
SubscriptionManager/
├── backend/
│   ├── src/
│   │   └── SubscriptionManager.API/
│   │       ├── Controllers/            # One per resource: Categories, Subscriptions, PriceHistories, Expenses
│   │       ├── DTO/                    # Request/response records exposed by the controllers
│   │       ├── Domain/
│   │       │   ├── Entities/           # Category, Subscription, PriceHistory
│   │       │   └── enum/               # BillingCycle, SubscriptionStatus
│   │       ├── Infrastructure/
│   │       │   ├── ArgumentExceptionHandler.cs    # Domain guard-clause failures -> 400
│   │       │   ├── DbUpdateExceptionHandler.cs     # DB constraint violations -> 409
│   │       │   └── Persistence/
│   │       │       ├── Configurations/ # EF Core Fluent API per entity
│   │       │       └── SubscriptionManagerDbContext.cs
│   │       ├── Migrations/             # EF Core migrations (SQLite schema history)
│   │       └── Program.cs
│   └── tests/                          # xUnit test project(s)
├── frontend/                           # React + TypeScript + Vite app
└── docs/                               # Project documentation
```

---

## 🚫 Out of Scope (MVP)

The following are **not** part of v1.0. They may be considered for future iterations:

- Multi-user support / authentication across devices.
- Push notifications or email reminders.
- Direct bank or payment provider integrations.
- Mobile application.
- Cloud sync / backup.

---

<p align="center"><sub>Last updated: 2026-11-15</sub></p>
