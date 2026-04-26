using CC.Domain.Dtos;
using CC.Domain.Entities;

namespace CC.Domain.Interfaces.Services;

public interface IScheduleQualityShiftScoreService : IServiceBase<ScheduleQualityShiftScore, ScheduleQualityShiftScoreDto>
{
    Task<IEnumerable<ScheduleQualityShiftScoreDto>> GetByDateUsingLatestTokenAsync(DateOnly date);
}
