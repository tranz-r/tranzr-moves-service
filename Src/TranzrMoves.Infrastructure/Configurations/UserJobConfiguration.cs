using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Infrastructure.Configurations;

public class UserJobConfiguration : IEntityTypeConfiguration<UserJob>
{
    public void Configure(EntityTypeBuilder<UserJob> builder)
    {
        builder.ToTable(Db.Tables.UserJobs);
        builder.HasKey(x => x.Id);
    }
}