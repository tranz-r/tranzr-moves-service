using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Application.Contracts;

public class ServiceFeatureDto
{
    public Guid Id { get; set; }
    public ServiceLevel ServiceLevel { get; set; }
    public required string Text { get; set; }
    public int DisplayOrder { get; set; }

    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
    public uint Version { get; set; }
}