// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Infrastructure.Configurations;

public sealed class InventoryGoodConfiguration : IEntityTypeConfiguration<InventoryGood>
{
    public void Configure(EntityTypeBuilder<InventoryGood> builder)
    {
        builder.ToTable(Db.Tables.InventoryGoods, Db.SCHEMA, t =>
        {
            t.HasCheckConstraint("CK_inventory_goods_length_cm_positive", "length_cm > 0");
            t.HasCheckConstraint("CK_inventory_goods_width_cm_positive", "width_cm > 0");
            t.HasCheckConstraint("CK_inventory_goods_height_cm_positive", "height_cm > 0");
            t.HasCheckConstraint("CK_inventory_goods_volume_m3_positive", "volume_m3 > 0");
            t.HasCheckConstraint("CK_inventory_goods_popularity_index_non_negative", "popularity_index >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(x => x.CategoryId)
            .IsRequired();

        builder.Property(x => x.PopularityIndex)
            .HasDefaultValue(0);

        builder.Property(x => x.LengthCm)
            .HasColumnType("numeric(10,2)")
            .IsRequired();

        builder.Property(x => x.WidthCm)
            .HasColumnType("numeric(10,2)")
            .IsRequired();

        builder.Property(x => x.HeightCm)
            .HasColumnType("numeric(10,2)")
            .IsRequired();

        builder.Property(x => x.VolumeM3)
            .HasColumnType("numeric(12,6)")
            .IsRequired();

        builder.HasOne(x => x.Category)
            .WithMany(x => x.Goods)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.SearchAliases)
            .HasColumnType("text[]")
            .IsRequired();

        builder.Property(x => x.SearchText)
            .HasMaxLength(2000)
            .IsRequired();

        builder.HasIndex(x => x.CategoryId);

        builder.HasIndex(x => x.Name);

        builder.HasIndex(x => new { x.CategoryId, x.Name });

        builder.HasIndex(x => x.PopularityIndex);
    }
}
