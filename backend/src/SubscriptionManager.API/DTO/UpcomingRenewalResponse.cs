// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Manuel Garcia Rodriguez

using SubscriptionManager.API.Domain.Enum;

namespace SubscriptionManager.API.DTO;

public record UpcomingRenewalResponse(
    int SubscriptionId,
    string Name,
    decimal Price,
    BillingCycle BillingCycle,
    DateTime NextRenewalDate,
    int CategoryId);
