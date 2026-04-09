// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mediator;
using TranzrMoves.Application.Contracts;

namespace TranzrMoves.Application.Features.Inventory.Goods;

public sealed record GoodsByCategoryIdQuery(int CategoryId) : IQuery<ErrorOr<List<InventoryGoodImportDto>>>;
