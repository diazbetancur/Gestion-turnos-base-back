using AutoMapper;
using CC.Domain.Dtos;
using CC.Domain.Entities;
using CC.Domain.Interfaces.Repositories;
using CC.Domain.Interfaces.Services;
using CC.Domain.Services;

namespace CC.Application.Services;

public class ScheduleSuggestionService : ServiceBase<ScheduleSuggestion, ScheduleSuggestionDto>, IScheduleSuggestionService
{
    public ScheduleSuggestionService(IScheduleSuggestionRepository repository, IMapper mapper) : base(repository, mapper)
    {
    }

    public async Task<IEnumerable<ScheduleSuggestionDto>> GetWeeklyByMondayAsync(DateOnly mondayDate)
    {
        if (mondayDate.DayOfWeek != DayOfWeek.Monday)
            throw new ArgumentException("La fecha de inicio debe ser lunes.", nameof(mondayDate));

        var sundayDate = mondayDate.AddDays(6);

        return await GetAllAsync(
            x => x.WeekStart >= mondayDate && x.WeekStart <= sundayDate,
            orderBy: query => query.OrderBy(x => x.WeekStart).ThenBy(x => x.Prioridad).ThenBy(x => x.Titulo)).ConfigureAwait(false);
    }
}
