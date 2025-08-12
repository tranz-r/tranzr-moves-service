using Riok.Mapperly.Abstractions;
using TranzrMoves.Application.Contracts;
using TranzrMoves.Domain.Entities;

namespace TranzrMoves.Application.Mapper;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class JobMapper
{
    public partial JobDto MapJobToDto(Job job);
    public partial Job MapToJob(JobDto jobDto);
}