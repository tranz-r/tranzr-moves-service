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

    public string[] SearchAliases { get; private set; } = [];

    public string SearchText { get; private set; } = null!;

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
        decimal heightCm,
        IEnumerable<string>? searchAliases = null)
    {
        Id = id;
        Rename(name);
        SetCategory(categoryId);
        SetPopularityIndex(popularityIndex);
        SetDimensions(lengthCm, widthCm, heightCm);
        SetSearchAliases(searchAliases ?? []);
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        Name = name.Trim();
        RebuildSearchText();
    }

    public void SetCategory(int categoryId)
    {
        if (categoryId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(categoryId), "CategoryId must be greater than zero.");
        }

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

    public void SetSearchAliases(IEnumerable<string> aliases)
    {
        if (aliases is null)
            throw new ArgumentNullException(nameof(aliases));

        SearchAliases = aliases
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        RebuildSearchText();
    }

    private void RebuildSearchText()
    {
        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var token in Tokenize(Name))
            tokens.Add(token);

        foreach (var alias in SearchAliases)
        {
            foreach (var token in Tokenize(alias))
                tokens.Add(token);
        }

        SearchText = string.Join(' ', tokens);
    }

    private static IEnumerable<string> Tokenize(string input)
    {
        return input
            .Trim()
            .ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static decimal CalculateVolumeM3(decimal lengthCm, decimal widthCm, decimal heightCm)
    {
        var volume = (lengthCm * widthCm * heightCm) / 1_000_000m;
        return decimal.Round(volume, 6, MidpointRounding.AwayFromZero);
    }
}
