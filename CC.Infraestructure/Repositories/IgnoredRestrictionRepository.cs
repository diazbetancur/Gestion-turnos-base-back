using CC.Domain.Entities;
using CC.Domain.Interfaces.Repositories;

namespace CC.Infrastructure.Repositories;

public class IgnoredRestrictionRepository : ERepositoryBase<IgnoredRestriction>, IIgnoredRestrictionRepository
{
    public IgnoredRestrictionRepository(IQueryableUnitOfWork unitOfWork) : base(unitOfWork)
    {
    }
}
