// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace TranzrMoves.Domain.Entities;

public sealed class InventoryGood
{
    public int Id { get; private set; }

    public string Name { get; private set; } = null!;

    public int CategoryId { get; private set; }

    public int PopularityIndex { get; private set; }

    public decimal LengthCm { get; private set; }

    public decimal WidthCm { get; private set; }

    public decimal HeightCm { get; private set; }

    public decimal VolumeM3 { get; private set; }

    public InventoryCategory Category { get; private set; } = null!;

    private InventoryGood()
    {
        // EF Core
    }

    public InventoryGood(
        int id,
        string name,
        int categoryId,
        int popularityIndex,
        decimal lengthCm,
        decimal widthCm,
        decimal heightCm)
    {
        Id = id;
        Rename(name);
        SetCategory(categoryId);
        SetPopularityIndex(popularityIndex);
        SetDimensions(lengthCm, widthCm, heightCm);
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        Name = name.Trim();
    }

    public void SetCategory(int categoryId)
    {
        if (categoryId <= 0)
            throw new ArgumentOutOfRangeException(nameof(categoryId), "CategoryId must be greater than zero.");

        CategoryId = categoryId;
    }

    public void SetPopularityIndex(int popularityIndex)
    {
        if (popularityIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(popularityIndex), "PopularityIndex cannot be negative.");

        PopularityIndex = popularityIndex;
    }

    public void SetDimensions(decimal lengthCm, decimal widthCm, decimal heightCm)
    {
        if (lengthCm <= 0)
            throw new ArgumentOutOfRangeException(nameof(lengthCm), "Length must be greater than zero.");

        if (widthCm <= 0)
            throw new ArgumentOutOfRangeException(nameof(widthCm), "Width must be greater than zero.");

        if (heightCm <= 0)
            throw new ArgumentOutOfRangeException(nameof(heightCm), "Height must be greater than zero.");

        LengthCm = decimal.Round(lengthCm, 2, MidpointRounding.AwayFromZero);
        WidthCm = decimal.Round(widthCm, 2, MidpointRounding.AwayFromZero);
        HeightCm = decimal.Round(heightCm, 2, MidpointRounding.AwayFromZero);

        VolumeM3 = CalculateVolumeM3(LengthCm, WidthCm, HeightCm);
    }

    private static decimal CalculateVolumeM3(decimal lengthCm, decimal widthCm, decimal heightCm)
    {
        var volume = (lengthCm * widthCm * heightCm) / 1_000_000m;
        return decimal.Round(volume, 6, MidpointRounding.AwayFromZero);
    }
}
