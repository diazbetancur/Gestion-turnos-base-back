using CC.Domain.Entities;
using CC.Domain.Interfaces.Repositories;

namespace CC.Infrastructure.Repositories;

public class ScheduleSuggestionRepository : ERepositoryBase<ScheduleSuggestion>, IScheduleSuggestionRepository
{
    public ScheduleSuggestionRepository(IQueryableUnitOfWork unitOfWork) : base(unitOfWork)
    {
    }
}
