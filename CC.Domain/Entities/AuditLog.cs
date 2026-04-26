using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CC.Domain.Enums;

namespace CC.Domain.Entities;

/// <summary>
/// Registro de auditoría con información completa de cambios en entidades
/// </summary>
public class AuditLog
{
    /// <summary>
    /// Identificador único del registro de auditoría
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Nombre de la entidad afectada (ej: "Schedule", "User", "Workstation")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// ID del registro afectado (PK de la entidad)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de operación: Create, Update, Delete, SoftDelete
    /// </summary>
    [Required]
    public AuditOperationType OperationType { get; set; }

    /// <summary>
    /// JSON con los valores anteriores (null en Create)
    /// Ejemplo: {"StartTime":"09:00:00","EndTime":"17:00:00"}
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? OldValues { get; set; }

    /// <summary>
    /// JSON con los valores nuevos (null en Delete físico)
    /// Ejemplo: {"StartTime":"10:00:00","EndTime":"18:00:00"}
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? NewValues { get; set; }

    /// <summary>
    /// Lista de columnas que cambiaron, separadas por coma
    /// Ejemplo: "StartTime,EndTime,Observation"
    /// </summary>
    [MaxLength(1000)]
    public string? ChangedColumns { get; set; }

    /// <summary>
    /// ID del usuario que realizó la acción
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Nombre de usuario para referencia rápida (desnormalizado)
    /// </summary>
    [MaxLength(100)]
    public string? UserName { get; set; }

    /// <summary>
    /// Dirección IP del cliente
    /// </summary>
    [MaxLength(50)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User Agent del navegador/cliente
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Ruta de la petición HTTP
    /// </summary>
    [MaxLength(500)]
    public string? RequestPath { get; set; }

    /// <summary>
    /// Método HTTP (GET, POST, PUT, DELETE)
    /// </summary>
    [MaxLength(10)]
    public string? HttpMethod { get; set; }

    /// <summary>
    /// Timestamp exacto de la operación
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Si la operación fue exitosa
    /// </summary>
    public bool IsSuccess { get; set; } = true;

    /// <summary>
    /// Mensaje de error si la operación falló
    /// </summary>
    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }
}
