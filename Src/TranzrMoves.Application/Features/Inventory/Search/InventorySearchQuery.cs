// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mediator;
using TranzrMoves.Application.Contracts;

namespace TranzrMoves.Application.Features.Inventory.Search;

public sealed record InventorySearchQuery(string Query = "", int Limit = 10) : IQuery<ErrorOr<List<InventoryGoodDto>>>;
