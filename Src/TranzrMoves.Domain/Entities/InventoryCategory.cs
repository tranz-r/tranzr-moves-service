// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace TranzrMoves.Domain.Entities;

public sealed class InventoryCategory
{
    public int Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string? Icon { get; private set; }

    public ICollection<InventoryGood> Goods { get; private set; } = new List<InventoryGood>();

    private InventoryCategory()
    {
        // EF Core
    }

    public InventoryCategory(int id, string name, string? icon)
    {
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        Id = id;
        Name = name.Trim();
        Icon = string.IsNullOrWhiteSpace(icon) ? null : icon.Trim();
    }
}
