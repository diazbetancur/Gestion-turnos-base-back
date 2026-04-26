using AutoMapper;
using CC.Domain.Dtos;
using CC.Domain.Entities;
using CC.Domain.Interfaces.Repositories;
using CC.Domain.Interfaces.Services;
using CC.Domain.Services;

namespace CC.Application.Services;

public class ScheduleQualityShiftScoreService : ServiceBase<ScheduleQualityShiftScore, ScheduleQualityShiftScoreDto>, IScheduleQualityShiftScoreService
{
    public ScheduleQualityShiftScoreService(IScheduleQualityShiftScoreRepository repository, IMapper mapper) : base(repository, mapper)
    {
    }

    public async Task<IEnumerable<ScheduleQualityShiftScoreDto>> GetByDateUsingLatestTokenAsync(DateOnly date)
    {
        var latestForDate = (await GetAllAsync(
            x => x.Date == date,
            orderBy: query => query.OrderByDescending(x => x.DateCreated))
            .ConfigureAwait(false))
            .FirstOrDefault();

        if (latestForDate == null)
            return Enumerable.Empty<ScheduleQualityShiftScoreDto>();

        return await GetAllAsync(
            x => x.Token == latestForDate.Token,
            orderBy: query => query.OrderBy(x => x.Date).ThenBy(x => x.StartTime)).ConfigureAwait(false);
    }
}
