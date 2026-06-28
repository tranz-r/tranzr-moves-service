using Riok.Mapperly.Abstractions;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Application.Mapper;

[Mapper]
public partial class BusinessUserMapper
{
    public BusinessUserDto ToBusinessUserDto(BusinessUser source) =>
        new()
        {
            BusinessUserId = source.Id,
            UserId = source.UserId,
            FirstName = source.User?.FirstName,
            LastName = source.User?.LastName,
            Email = source.User?.Email,
            Role = source.Role,
            Status = source.Status,
        };

    public IReadOnlyList<BusinessUserDto> ToBusinessUserDtos(IReadOnlyList<BusinessUser> source) =>
        source.Select(ToBusinessUserDto).ToList();
}
