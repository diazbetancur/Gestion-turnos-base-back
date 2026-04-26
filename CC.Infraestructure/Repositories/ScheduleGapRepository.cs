using CC.Domain.Entities;
using CC.Domain.Interfaces.Repositories;

namespace CC.Infrastructure.Repositories;

public class ScheduleGapRepository : ERepositoryBase<ScheduleGap>, IScheduleGapRepository
{
    public ScheduleGapRepository(IQueryableUnitOfWork unitOfWork) : base(unitOfWork)
    {
    }
}
