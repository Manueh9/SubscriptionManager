using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionManager.API.Domain.Entities;

namespace SubscriptionManager.API.Infrastructure.Persistence.Configurations;

public class PriceHistoryConfiguration : IEntityTypeConfiguration<PriceHistory>
{
    public void Configure(EntityTypeBuilder<PriceHistory> builder)
    {
        builder.ToTable("PriceHistories");

        builder.HasKey(priceHistory => priceHistory.Id);

        builder.Property(priceHistory => priceHistory.OldPrice)
            .HasPrecision(10, 2);

        builder.Property(priceHistory => priceHistory.NewPrice)
            .HasPrecision(10, 2);

        builder.HasOne(priceHistory => priceHistory.Subscription)
            .WithMany()
            .HasForeignKey(priceHistory => priceHistory.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
