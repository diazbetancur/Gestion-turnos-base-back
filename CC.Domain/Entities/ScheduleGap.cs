namespace CC.Domain.Entities;

public class ScheduleGap : EntityBase<Guid>
{
    public DateOnly Date { get; set; }
    public Guid WorkstationId { get; set; }
    public virtual Workstation Workstation { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? GapExplanation { get; set; }
    public string? GapCategory { get; set; }
    public string? Token { get; set; }
    public bool IsPostAi { get; set; }
}
