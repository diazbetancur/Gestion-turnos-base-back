using CC.Domain.Dtos;
using CC.Domain.Entities;

namespace CC.Domain.Interfaces.Services;

public interface IScheduleSuggestionService : IServiceBase<ScheduleSuggestion, ScheduleSuggestionDto>
{
    Task<IEnumerable<ScheduleSuggestionDto>> GetWeeklyByMondayAsync(DateOnly mondayDate);
}
