using System.Linq.Expressions;
using Concertable.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.Kernel;

public static class EntityTypeBuilderExtensions
{
    private const string CountyColumn = "County";
    private const string TownColumn = "Town";

    /// <summary>
    /// Maps the owned <see cref="Address"/> value object to flat <c>County</c>/<c>Town</c> columns on
    /// the owner's table (no <c>Address_</c> prefix), so every owner persists it identically.
    /// Pass <paramref name="required"/> = false for owners whose address is optional (nullable).
    /// </summary>
    public static EntityTypeBuilder<TOwner> OwnsAddress<TOwner>(
        this EntityTypeBuilder<TOwner> builder,
        Expression<Func<TOwner, Address?>> navigation,
        bool required = true)
        where TOwner : class
    {
        builder.OwnsOne(navigation, address =>
        {
            address.Property(a => a.County).HasColumnName(CountyColumn);
            address.Property(a => a.Town).HasColumnName(TownColumn);
        });
        builder.Navigation(navigation).IsRequired(required);
        return builder;
    }
}
