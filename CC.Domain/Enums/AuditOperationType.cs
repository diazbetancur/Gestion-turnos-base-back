namespace CC.Domain.Enums;

/// <summary>
/// Tipos de operación para el sistema de auditoría
/// </summary>
public enum AuditOperationType
{
    /// <summary>
    /// Creación de un nuevo registro
    /// </summary>
    Create = 1,

    /// <summary>
    /// Actualización de un registro existente
    /// </summary>
    Update = 2,

    /// <summary>
    /// Eliminación física de un registro
    /// </summary>
    Delete = 3,

    /// <summary>
    /// Eliminación lógica (soft delete)
    /// </summary>
    SoftDelete = 4,

    /// <summary>
    /// Inicio de sesión
    /// </summary>
    Login = 5,

    /// <summary>
    /// Cierre de sesión
    /// </summary>
    Logout = 6,

    /// <summary>
    /// Cambio de contraseña
    /// </summary>
    PasswordChange = 7,

    /// <summary>
    /// Restablecimiento de contraseña
    /// </summary>
    PasswordReset = 8
}
