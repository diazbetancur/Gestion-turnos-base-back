using AutoMapper;
using CC.Domain.Dtos;
using CC.Domain.Entities;
using CC.Domain.Interfaces.Repositories;
using CC.Domain.Interfaces.Services;
using CC.Domain.Services;

namespace CC.Application.Services;

public class ScheduleGapService : ServiceBase<ScheduleGap, ScheduleGapDto>, IScheduleGapService
{
    public ScheduleGapService(IScheduleGapRepository repository, IMapper mapper) : base(repository, mapper)
    {
    }

    public async Task<IEnumerable<ScheduleGapDto>> GetWeeklyByMondayAsync(DateOnly mondayDate)
    {
        if (mondayDate.DayOfWeek != DayOfWeek.Monday)
            throw new ArgumentException("La fecha de inicio debe ser lunes.", nameof(mondayDate));

        var sundayDate = mondayDate.AddDays(6);

        return await GetAllAsync(
            x => x.Date >= mondayDate && x.Date <= sundayDate,
            orderBy: query => query.OrderBy(x => x.Date).ThenBy(x => x.StartTime),
            includeProperties: "Workstation,Workstation.WorkArea").ConfigureAwait(false);
    }
}
