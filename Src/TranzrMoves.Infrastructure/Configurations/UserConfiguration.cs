using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Entities;
using State = AdminClientHandlerService.Domain.Entities.Types_rest_api_v1.AdminClientStates;
using IpAddressKind = AdminClientHandlerService.Domain.Entities.Types_rest_api_v1.IpAddressKind;
using ArchType = AdminClientHandlerService.Domain.Entities.Types_rest_api_v1.DeviceArchitectureTypes;

namespace TranzrMoves.Infrastructure.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable(Db.Tables.User);
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name).IsRequired(false);
            builder.Property(x => x.Alias).IsRequired(false);
            builder.Property(x => x.Address).IsRequired(false);
            builder.Property(x => x.PostalCode).IsRequired(false);
            builder.Property(x => x.City).IsRequired(false);
            builder.Property(x => x.Remark).IsRequired(false);
            builder.Property(x => x.GeoLocation).IsRequired(false);

            builder.Property(x => x.State).IsRequired(false)
                .HasConversion(
                    v => v.ToString(),
                    v => (State)Enum.Parse(typeof(State), v));

            builder.Property(x => x.IpAddress).IsRequired(false);
            builder.Property(x => x.IpAddressKind).IsRequired(false)
                .HasConversion(
                    v => v.ToString(),
                    v => (IpAddressKind)Enum.Parse(typeof(IpAddressKind), v));

            builder.Property(x => x.ArchitectureTypes).IsRequired(false)
                .HasConversion(
                    v => v.ToString(),
                    v => (ArchType)Enum.Parse(typeof(ArchType), v));

            builder.Property(x => x.HardwareVersion).IsRequired(false);
            builder.Property(x => x.SystemTime).IsRequired(false);
            builder.Property(x => x.RTCTime).IsRequired(false);

            builder.Property(x => x.LogLevel).IsRequired(false)
                .HasConversion(
                    v => v.ToString(),
                    v => (Types_rest_api_v1.LogLevelProperty)Enum.Parse(typeof(Types_rest_api_v1.LogLevelProperty), v));

            builder.Property(x => x.Metrics).IsRequired(false)
                .HasConversion(
                    v => v.ToString(),
                    v => (Types_rest_api_v1.MetricsProperty)Enum.Parse(typeof(Types_rest_api_v1.MetricsProperty), v));

            builder.Property(x => x.Traces).IsRequired(false)
                .HasConversion(
                    v => v.ToString(),
                    v => (Types_rest_api_v1.TracesProperty)Enum.Parse(typeof(Types_rest_api_v1.TracesProperty), v));
        }
    }
}