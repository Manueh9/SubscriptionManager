// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Manuel Garcia Rodriguez

namespace SubscriptionManager.API.DTO;

public record ExpensesSummaryResponse(decimal MonthlyTotal, decimal YearlyTotal, int ActiveSubscriptionCount);
