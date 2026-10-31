using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionManager.API.Domain.Entities;

namespace SubscriptionManager.API.Infrastructure.Persistence.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");

        builder.HasKey(subscription => subscription.Id);

        builder.Property(subscription => subscription.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(subscription => subscription.Description)
            .HasMaxLength(1000);

        builder.Property(subscription => subscription.Price)
            .HasPrecision(10, 2);

        builder.HasOne(subscription => subscription.Category)
            .WithMany()
            .HasForeignKey(subscription => subscription.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
