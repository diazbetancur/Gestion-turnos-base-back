using AutoMapper;
using CC.Domain.Dtos;
using CC.Domain.Entities;
using CC.Domain.Interfaces.Repositories;
using CC.Domain.Interfaces.Services;
using CC.Domain.Services;

namespace CC.Application.Services;

public class IgnoredRestrictionService : ServiceBase<IgnoredRestriction, IgnoredRestrictionDto>, IIgnoredRestrictionService
{
    public IgnoredRestrictionService(IIgnoredRestrictionRepository repository, IMapper mapper) : base(repository, mapper)
    {
    }
}
