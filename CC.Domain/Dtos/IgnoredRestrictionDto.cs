namespace CC.Domain.Dtos;

public class IgnoredRestrictionDto : BaseDto<Guid>
{
    public DateOnly Date { get; set; }
    public Guid UserId { get; set; }
    public string Observation { get; set; }
}
