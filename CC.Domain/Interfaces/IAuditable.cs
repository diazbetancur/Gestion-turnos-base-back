namespace CC.Domain.Interfaces;

/// <summary>
/// Interfaz marcadora que identifica las entidades que deben ser auditadas automáticamente.
/// Las entidades que implementen esta interfaz tendrán sus cambios registrados en AuditLog.
/// </summary>
public interface IAuditable
{
    // Marker interface - las entidades que implementen esto serán auditadas
}
