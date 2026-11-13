using System.ComponentModel.DataAnnotations;
using SubscriptionManager.API.Domain.Enum;

namespace SubscriptionManager.API.DTO;

public record ChangeStatusRequest([EnumDataType(typeof(SubscriptionStatus))] SubscriptionStatus NewStatus);
