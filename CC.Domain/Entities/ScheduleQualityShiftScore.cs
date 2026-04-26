namespace CC.Domain.Entities;

public class ScheduleQualityShiftScore : EntityBase<Guid>
{
    public string Token { get; set; }
    public Guid? DemandId { get; set; }
    public DateOnly Date { get; set; }
    public string? WorkstationName { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public decimal Demanded { get; set; }
    public decimal Covered { get; set; }
    public decimal Unmet { get; set; }
    public decimal CoverageScore { get; set; }
    public decimal FairnessScore { get; set; }
    public decimal RulesScore { get; set; }
    public decimal Score { get; set; }
    public string Metrics { get; set; }
    public string? ConfigSnapshot { get; set; }
    public bool IsPostAi { get; set; }
}
