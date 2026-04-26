using CC.Domain.Dtos;
using CC.Domain.Entities;

namespace CC.Domain.Interfaces.Services;

public interface IScheduleGapService : IServiceBase<ScheduleGap, ScheduleGapDto>
{
    Task<IEnumerable<ScheduleGapDto>> GetWeeklyByMondayAsync(DateOnly mondayDate);
}
