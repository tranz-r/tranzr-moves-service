using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Domain.Entities;

/// <summary>Feature text bullets per service level (e.g., “Priority scheduling”).</summary>
public sealed class ServiceFeature : IAuditable
{
    public Guid Id { get; set; }
    public ServiceLevel ServiceLevel { get; set; }
    public string Text { get; set; } = default!;
    public int DisplayOrder { get; set; } = 1;           // 1..N

    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "System";
    public DateTimeOffset ModifiedAt { get; set; }
    public string ModifiedBy { get; set; } = "System";
    public uint Version { get; set; }
}