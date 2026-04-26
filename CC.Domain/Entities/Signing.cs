using CC.Domain.Enums;
using CC.Domain.Interfaces;

namespace CC.Domain.Entities
{
    public class Signing : EntityBase<Guid>, IAuditable
    {
        public Guid UserId { get; set; }
        public virtual User User { get; set; }
        public DateOnly Date { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public SigningType TipoFichaje { get; set; }
        public string Observaciones { get; set; }
        public Guid? LastUpdateUserId { get; set; }

        public virtual User LastUpdateUser
        {
            get; set;
        }

        public DateTime? UpdatedAt { get; set; }
    }
}