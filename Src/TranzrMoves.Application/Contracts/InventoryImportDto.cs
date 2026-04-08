// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace TranzrMoves.Application.Contracts;

public sealed class InventoryImportDto
{
    [JsonPropertyName("categories")]
    public List<InventoryCategoryImportDto> Categories { get; set; } = [];

    [JsonPropertyName("goods")]
    public List<InventoryGoodImportDto> Goods { get; set; } = [];
}

public sealed class InventoryCategoryImportDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }
}

public sealed class InventoryGoodImportDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("category_id")]
    public int CategoryId { get; set; }

    [JsonPropertyName("popularity_index")]
    public int PopularityIndex { get; set; }

    [JsonPropertyName("length")]
    public decimal Length { get; set; }

    [JsonPropertyName("width")]
    public decimal Width { get; set; }

    [JsonPropertyName("height")]
    public decimal Height { get; set; }
}
