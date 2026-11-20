# ✨ What the App Actually Does, Plainly

🇬🇧 **English** | [🇪🇸 Español](./Features.es.md)

> This document isn't about endpoints, status codes, or database tables — that's already in the [Changelog](../../CHANGELOG.md) and the [API Reference](../Api/Api.en.md), for whoever builds the thing. This is **what the app can do today, explained the way you'd explain it to someone who's just going to use it.**

---

## ⚠️ Before anything else: how this is actually used today

Everything below **already works**, but there's no screen to click through yet — no buttons, no forms, nothing visual. Right now, all of this can only be exercised through direct HTTP requests or the automated tests; an ordinary person can't open the app and use it yet. That's exactly what's missing — see the [Roadmap](../Roadmap/Roadmap.en.md) for the plan to get there.

Think of it like this: the engine is already built and tested, but there's no body or steering wheel yet.

---

## 📂 Organize your subscriptions into categories

You can create your own categories to group what you pay for — "Streaming," "Software," "Gym," "Home," whatever makes sense to you. There's no fixed, predefined list of categories.

- You can't create two categories with the same name (even with different capitalization) — so you never end up with "Streaming" and "streaming" as two different things by accident.
- You can rename a category later without losing the subscriptions inside it.
- You can't delete a category while it still has subscriptions assigned to it — you have to move or delete them first, so you never end up with an "orphaned" subscription with no category.

## 📝 Keep track of every subscription's details

For each subscription you track, the app keeps:

- **Its name** (Netflix, Spotify, your gym, whatever it is).
- **A free-text description**, if you want to jot something down.
- **The price** you pay.
- **How often you're billed**: daily, monthly, or yearly.
- **How long you've had it**, and, if you've ended it, **when it stopped**.
- **Its category**.
- **Its status**: active, paused, or cancelled (more on that below).

The same subscription name can't be repeated twice within the same category (so you never end up with two "Netflix" entries in "Streaming" by accident), but you *can* have a subscription with the same name in two different categories if you ever needed to.

## 💶 Change the price and see how it's evolved

When a service raises (or lowers) its price on you, you update it in the app — and it automatically **saves forever** when that happened, what the price was before, and what it is now. Over time, that gives you a full history of how much each service has crept up (or dropped).

You can't "change" the price to the exact same price it already had, or set it negative — the app will stop you if you try.

## ⏸️ Pause, cancel or reactivate without losing the history

A subscription can be:

- **Active** — you're currently paying for it.
- **Paused** — for whatever reason, it's not being charged right now, but you haven't deleted it entirely.
- **Cancelled** — you've ended it.

You can switch between these whenever you want (even reactivating one that was paused or cancelled). None of this erases the price history or the subscription's details — it just changes its status.

## 📅 See what's about to be charged

The app knows how to work out, for every active subscription, when its next charge falls due — whether it's billed daily, monthly, or yearly — and can show you just the ones coming up in however many days you choose (e.g. "what's getting charged in the next 30 days"), soonest first.

This holds up even in tricky calendar cases — for example, a monthly subscription that started on the 31st still correctly works out its next charge even in months that don't have 31 days.

Paused or cancelled subscriptions never show up here, since nothing's actually going to be charged for them.

## 📊 See how much you're spending overall

At a glance, the app can tell you:

- How much it's costing you **per month**, in total, across every active subscription (converting yearly or daily ones to their monthly equivalent).
- How much it's costing you **per year**.
- How many active subscriptions you have in total.

Paused and cancelled subscriptions don't count towards this — only what you're actually paying right now.

## 🔒 Everything stays on your own computer

No accounts, no login, no cloud involved anywhere. Everything you save lives in a single file on your own machine. Nobody else can see your data, and the app works without an internet connection (other than for whatever you look up yourself).

---

## 🚫 What the app does **not** do (yet, or ever)

So there are no surprises:

- **No screen or buttons yet** — see the warning at the top. That's the very next thing being built.
- **No reminders or notifications** that something's about to be charged — you can *check* what's coming up, but the app won't proactively alert you.
- **No connection to your bank or any payment provider** — you enter the data by hand; the app knows nothing about your actual bank account.
- **Not multi-user** — it's built for one person to use, not to be shared across separate accounts with a partner or family.
- **No cloud backup** — if you want to back up your data, you'll need to copy the database file yourself.
- **No mobile app** — it's a web application that runs on your computer.

---

<p align="center"><sub>For the technical detail behind all of this (endpoints, validation, error codes), see the <a href="../../CHANGELOG.md">Changelog</a> and the <a href="../Api/Api.en.md">API Reference</a>.</sub></p>
