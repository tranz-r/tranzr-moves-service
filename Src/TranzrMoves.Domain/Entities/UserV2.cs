using System.ComponentModel.DataAnnotations.Schema;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Domain.Entities;

public class UserV2 : IAuditable
{
    public Guid Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public Guid? SupabaseId { get; set; }

    /// <summary>Profile addresses for this user (billing, residential, commercial), keyed by <see cref="AddressType"/>.</summary>
    public ICollection<AddressV2> Addresses { get; set; } = new List<AddressV2>();

    [NotMapped]
    public AddressV2? BillingAddress => Addresses.FirstOrDefault(a => a.Type == AddressType.Billing);

    [NotMapped]
    public AddressV2? ResidentialAddress => Addresses.FirstOrDefault(a => a.Type == AddressType.Residential);

    [NotMapped]
    public AddressV2? CommercialAddress => Addresses.FirstOrDefault(a => a.Type == AddressType.Commercial);

    /// <summary>Replaces any existing profile row of <paramref name="type"/> and attaches <paramref name="replacement"/> to this user.</summary>
    public void UpsertProfileAddress(AddressType type, AddressV2 replacement)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        replacement.Type = type;
        replacement.UserId = Id;
        replacement.User = this;

        var existing = Addresses.FirstOrDefault(a => a.Type == type);
        if (existing is not null)
            _ = Addresses.Remove(existing);

        Addresses.Add(replacement);
    }

    public Instant CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "System";
    public Instant ModifiedAt { get; set; }
    public string ModifiedBy { get; set; } = "System";

    //Relationship properties
    public ICollection<QuoteV2>? Quotes { get; set; } = [];
}
