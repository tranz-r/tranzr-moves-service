// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace TranzrMoves.Application.Contracts;

public sealed class InventoryGoodDto : InventoryGoodImportDto
{
    [JsonPropertyName("volume_m3")]
    public decimal VolumeM3 { get; init; }

    [JsonPropertyName("category_name")]
    public string CategoryName { get; init; } = null!;
}
