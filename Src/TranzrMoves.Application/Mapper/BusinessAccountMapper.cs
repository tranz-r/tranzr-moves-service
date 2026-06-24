using Riok.Mapperly.Abstractions;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Application.Mapper;

[Mapper]
public partial class BusinessAccountMapper
{
    public partial BillingAddress ToBillingAddress(AddressDto source);

    public partial AddressDto ToAddressDto(BillingAddress source);

    public BusinessAccountDto ToBusinessAccountDto(TranzrMoves.Domain.Entities.BusinessAccount source) =>
        new()
        {
            BusinessAccountId = source.Id,
            BusinessName = source.BusinessName,
            TradingName = source.TradingName,
            BusinessEmail = source.BusinessEmail,
            BusinessPhone = source.BusinessPhone,
            CompanyRegistrationNumber = source.CompanyRegistrationNumber,
            VatNumber = source.VatNumber,
            Status = source.Status,
            BillingAddress = ToAddressDto(source.BillingAddress),
            CreatedAt = source.CreatedAt,
            ModifiedAt = source.ModifiedAt,
        };
}
