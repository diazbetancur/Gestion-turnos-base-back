namespace CC.Domain.Dtos;

public class ScheduleGapDto : BaseDto<Guid>
{
    public DateOnly Date { get; set; }
    public Guid WorkstationId { get; set; }
    public string? WorkstationName { get; set; }
    public Guid? WorkstationWorkAreaId { get; set; }
    public string? WorkstationWorkAreaName { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? GapExplanation { get; set; }
    public string? GapCategory { get; set; }
    public string? Token { get; set; }
    public bool IsPostAi { get; set; }
}
