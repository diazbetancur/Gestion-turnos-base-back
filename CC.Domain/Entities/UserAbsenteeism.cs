using CC.Domain.Interfaces;

namespace CC.Domain.Entities;

public class UserAbsenteeism : EntityBase<Guid>, IAuditable
{
    public Guid UserId { get; set; }
    public virtual User User { get; set; }

    public Guid AbsenteeismTypeId { get; set; }
    public virtual AbsenteeismType AbsenteeismType { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Observation { get; set; }
}