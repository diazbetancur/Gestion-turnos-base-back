using CC.Domain.Entities;
using CC.Domain.Interfaces.Repositories;

namespace CC.Infrastructure.Repositories;

public class ScheduleQualityShiftScoreRepository : ERepositoryBase<ScheduleQualityShiftScore>, IScheduleQualityShiftScoreRepository
{
    public ScheduleQualityShiftScoreRepository(IQueryableUnitOfWork unitOfWork) : base(unitOfWork)
    {
    }
}
