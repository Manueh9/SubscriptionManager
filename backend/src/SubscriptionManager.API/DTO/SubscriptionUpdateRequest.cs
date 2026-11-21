// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Manuel Garcia Rodriguez

using System.ComponentModel.DataAnnotations;
using SubscriptionManager.API.Domain.Enum;

namespace SubscriptionManager.API.DTO;

public record SubscriptionUpdateRequest(
    string Name,
    string Description,
    [EnumDataType(typeof(BillingCycle))] BillingCycle BillingCycle,
    DateTime StartDate,
    DateTime? EndDate,
    int CategoryId);
