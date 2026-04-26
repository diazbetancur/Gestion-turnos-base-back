namespace CC.Domain.Entities;

public class ScheduleSuggestion : EntityBase<Guid>
{
    public string Token { get; set; }
    public DateOnly WeekStart { get; set; }
    public DateOnly WeekEnd { get; set; }
    public string Tipo { get; set; }
    public string Prioridad { get; set; }
    public string Titulo { get; set; }
    public string? Descripcion { get; set; }
    public string? ImpactoEsperado { get; set; }
    public decimal? MejoraEstimada { get; set; }
    public string? Detalles { get; set; }
    public string? EmpleadosInvolucrados { get; set; }
    public string? WorkstationsAfectadas { get; set; }
    public string? DiasAfectados { get; set; }
    public string? AccionesConcretas { get; set; }
    public bool? Implementada { get; set; }
    public DateTime? FechaImplementacion { get; set; }
    public bool IsPostAi { get; set; }
}
