namespace CC.Domain.Entities;

public class IgnoredRestriction : EntityBase<Guid>
{
    public DateOnly Date { get; set; }
    public Guid UserId { get; set; }
    public virtual User User { get; set; }
    public string Observation { get; set; }
}
