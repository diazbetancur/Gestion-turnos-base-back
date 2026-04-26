using System.Text.Json;
using System.Text.Json.Serialization;
using CC.Domain.Entities;
using CC.Domain.Enums;
using CC.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CC.Infrastructure.Interceptors;

/// <summary>
/// Interceptor de EF Core que captura automáticamente los cambios en entidades auditables
/// y los registra en la tabla AuditLogs con valores anteriores y nuevos.
/// </summary>
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    /// <summary>
    /// Propiedades que se excluyen de la auditoría por seguridad o irrelevancia
    /// </summary>
    private static readonly HashSet<string> ExcludedProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        // Propiedades de seguridad (nunca auditar)
        "PasswordHash",
        "SecurityStamp",
        "ConcurrencyStamp",
        "PasswordResetToken",
        "PasswordResetTokenExpiration",
        "NormalizedUserName",
        "NormalizedEmail",
        
        // Propiedades de auditoría/timestamp automático
        "DateCreated",
        "DateUpdate",
        "UpdatedAt",
        "CreatedAt",
        
        // Propiedades técnicas que no aportan valor de negocio
        "Token",
        "IdUpdate",
        "Notified",
        "AccessFailedCount",
        "LockoutEnd",
        "LockoutEnabled",
        "TwoFactorEnabled",
        "PhoneNumberConfirmed",
        "EmailConfirmed"
    };

    /// <summary>
    /// Opciones de serialización JSON para los valores de auditoría
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = null,
        Converters = { new JsonStringEnumConverter() }
    };

    public AuditSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        try
        {
            var auditLogs = GenerateAuditLogs(eventData.Context);

            if (auditLogs.Count > 0)
            {
                eventData.Context.Set<AuditLog>().AddRange(auditLogs);
            }
        }
        catch (Exception ex)
        {
            // Log error pero no interrumpir la operación principal
            Console.WriteLine($"[AuditInterceptor] Error generating audit logs: {ex.Message}");
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is null)
            return base.SavingChanges(eventData, result);

        try
        {
            var auditLogs = GenerateAuditLogs(eventData.Context);

            if (auditLogs.Count > 0)
            {
                eventData.Context.Set<AuditLog>().AddRange(auditLogs);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuditInterceptor] Error generating audit logs: {ex.Message}");
        }

        return base.SavingChanges(eventData, result);
    }

    private List<AuditLog> GenerateAuditLogs(DbContext context)
    {
        var auditLogs = new List<AuditLog>();
        var httpContext = _httpContextAccessor.HttpContext;

        // Obtener información del contexto HTTP actual
        var userId = GetCurrentUserId(httpContext);
        var userName = GetCurrentUserName(httpContext);
        var ipAddress = GetIpAddress(httpContext);
        var userAgent = httpContext?.Request.Headers["User-Agent"].ToString();
        var requestPath = httpContext?.Request.Path.ToString();
        var httpMethod = httpContext?.Request.Method;
        var timestamp = DateTime.UtcNow;

        context.ChangeTracker.DetectChanges();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            // Solo auditar entidades que implementen IAuditable
            if (entry.Entity is not IAuditable)
                continue;

            // No auditar la propia tabla de auditoría (evitar recursión)
            if (entry.Entity is AuditLog)
                continue;

            // Solo auditar cambios relevantes
            if (entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            var auditLog = CreateAuditLog(entry, userId, userName, ipAddress, userAgent, requestPath, httpMethod, timestamp);
            
            if (auditLog != null)
            {
                auditLogs.Add(auditLog);
            }
        }

        return auditLogs;
    }

    private AuditLog? CreateAuditLog(
        EntityEntry entry,
        Guid? userId,
        string? userName,
        string? ipAddress,
        string? userAgent,
        string? requestPath,
        string? httpMethod,
        DateTime timestamp)
    {
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = entry.Entity.GetType().Name,
            EntityId = GetPrimaryKeyValue(entry),
            UserId = userId,
            UserName = userName,
            IpAddress = ipAddress,
            UserAgent = userAgent?.Length > 500 ? userAgent[..500] : userAgent,
            RequestPath = requestPath?.Length > 500 ? requestPath[..500] : requestPath,
            HttpMethod = httpMethod,
            Timestamp = timestamp,
            IsSuccess = true
        };

        switch (entry.State)
        {
            case EntityState.Added:
                auditLog.OperationType = AuditOperationType.Create;
                auditLog.NewValues = SerializeCurrentValues(entry);
                auditLog.ChangedColumns = GetAllPropertyNames(entry);
                break;

            case EntityState.Modified:
                // Detectar si es SoftDelete
                if (IsSoftDelete(entry))
                {
                    auditLog.OperationType = AuditOperationType.SoftDelete;
                }
                else
                {
                    auditLog.OperationType = AuditOperationType.Update;
                }

                auditLog.OldValues = SerializeOriginalValues(entry);
                auditLog.NewValues = SerializeModifiedCurrentValues(entry);
                auditLog.ChangedColumns = GetChangedPropertyNames(entry);
                
                // Si no hay cambios reales, no crear log
                if (string.IsNullOrEmpty(auditLog.ChangedColumns))
                    return null;
                break;

            case EntityState.Deleted:
                auditLog.OperationType = AuditOperationType.Delete;
                auditLog.OldValues = SerializeOriginalValuesForDelete(entry);
                break;

            default:
                return null;
        }

        return auditLog;
    }

    private static bool IsSoftDelete(EntityEntry entry)
    {
        // Buscar propiedades comunes de soft delete
        var softDeleteProperties = new[] { "IsDeleted", "IsDelete" };

        foreach (var propName in softDeleteProperties)
        {
            var property = entry.Properties.FirstOrDefault(p => 
                p.Metadata.Name.Equals(propName, StringComparison.OrdinalIgnoreCase));

            if (property != null && 
                property.IsModified && 
                property.CurrentValue is bool currentValue && 
                currentValue == true &&
                property.OriginalValue is bool originalValue && 
                originalValue == false)
            {
                return true;
            }
        }

        return false;
    }

    private static string GetPrimaryKeyValue(EntityEntry entry)
    {
        var keyProperties = entry.Properties
            .Where(p => p.Metadata.IsPrimaryKey())
            .ToList();

        if (keyProperties.Count == 0)
            return "Unknown";

        if (keyProperties.Count == 1)
            return keyProperties[0].CurrentValue?.ToString() ?? "Unknown";

        // Clave compuesta
        var keyValues = keyProperties
            .Select(p => $"{p.Metadata.Name}={p.CurrentValue}")
            .ToArray();

        return string.Join(";", keyValues);
    }

    private static string? SerializeCurrentValues(EntityEntry entry)
    {
        var dict = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            // Excluir propiedades de navegación (FK shadow properties)
            if (property.Metadata.IsShadowProperty())
                continue;
                
            // Excluir claves foráneas (se pueden ver por el ID en la entidad)
            if (property.Metadata.IsForeignKey())
                continue;
            
            if (ShouldExcludeProperty(property.Metadata.Name))
                continue;

            var value = property.CurrentValue;
            dict[property.Metadata.Name] = ConvertValue(value);
        }

        return dict.Count > 0 ? JsonSerializer.Serialize(dict, JsonOptions) : null;
    }

    private static string? SerializeOriginalValues(EntityEntry entry)
    {
        var dict = new Dictionary<string, object?>();

        foreach (var property in entry.Properties.Where(p => p.IsModified))
        {
            // Excluir propiedades de navegación (FK shadow properties)
            if (property.Metadata.IsShadowProperty())
                continue;
                
            // Excluir propiedades de navegación
            if (property.Metadata.IsForeignKey())
                continue;
            
            if (ShouldExcludeProperty(property.Metadata.Name))
                continue;

            // Solo incluir si el valor realmente cambió
            if (ValuesAreEqual(property.OriginalValue, property.CurrentValue))
                continue;

            var value = property.OriginalValue;
            dict[property.Metadata.Name] = ConvertValue(value);
        }

        return dict.Count > 0 ? JsonSerializer.Serialize(dict, JsonOptions) : null;
    }

    private static string? SerializeModifiedCurrentValues(EntityEntry entry)
    {
        var dict = new Dictionary<string, object?>();

        foreach (var property in entry.Properties.Where(p => p.IsModified))
        {
            // Excluir propiedades de navegación (FK shadow properties)
            if (property.Metadata.IsShadowProperty())
                continue;
                
            // Excluir propiedades de navegación
            if (property.Metadata.IsForeignKey())
                continue;
            
            if (ShouldExcludeProperty(property.Metadata.Name))
                continue;

            // Solo incluir si el valor realmente cambió
            if (ValuesAreEqual(property.OriginalValue, property.CurrentValue))
                continue;

            var value = property.CurrentValue;
            dict[property.Metadata.Name] = ConvertValue(value);
        }

        return dict.Count > 0 ? JsonSerializer.Serialize(dict, JsonOptions) : null;
    }

    private static string? SerializeOriginalValuesForDelete(EntityEntry entry)
    {
        var dict = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            // Excluir propiedades de navegación
            if (property.Metadata.IsShadowProperty())
                continue;
                
            if (property.Metadata.IsForeignKey())
                continue;
            
            if (ShouldExcludeProperty(property.Metadata.Name))
                continue;

            var value = property.OriginalValue;
            dict[property.Metadata.Name] = ConvertValue(value);
        }

        return dict.Count > 0 ? JsonSerializer.Serialize(dict, JsonOptions) : null;
    }

    private static object? ConvertValue(object? value)
    {
        return value switch
        {
            null => null,
            DateTime dt => dt.ToString("O"), // ISO 8601
            DateOnly d => d.ToString("yyyy-MM-dd"),
            TimeOnly t => t.ToString("HH:mm:ss"),
            TimeSpan ts => ts.ToString(@"hh\:mm\:ss"),
            Enum e => e.ToString(),
            Guid g => g.ToString(),
            _ => value
        };
    }

    private static string? GetAllPropertyNames(EntityEntry entry)
    {
        var names = entry.Properties
            .Where(p => !ShouldExcludeProperty(p.Metadata.Name) &&
                       !p.Metadata.IsShadowProperty() &&
                       !p.Metadata.IsForeignKey())
            .Select(p => p.Metadata.Name)
            .ToList();

        return names.Count > 0 ? string.Join(",", names) : null;
    }

    private static string? GetChangedPropertyNames(EntityEntry entry)
    {
        var names = entry.Properties
            .Where(p => p.IsModified && 
                       !ShouldExcludeProperty(p.Metadata.Name) &&
                       !p.Metadata.IsShadowProperty() &&
                       !p.Metadata.IsForeignKey() &&
                       !ValuesAreEqual(p.OriginalValue, p.CurrentValue))
            .Select(p => p.Metadata.Name)
            .ToList();

        return names.Count > 0 ? string.Join(",", names) : null;
    }

    private static bool ShouldExcludeProperty(string propertyName)
    {
        return ExcludedProperties.Contains(propertyName);
    }

    /// <summary>
    /// Compara dos valores para determinar si son iguales.
    /// Maneja correctamente nulls, tipos primitivos, fechas, guids, etc.
    /// </summary>
    private static bool ValuesAreEqual(object? originalValue, object? currentValue)
    {
        if (originalValue == null && currentValue == null)
            return true;
        
        if (originalValue == null || currentValue == null)
            return false;

        // Comparación especial para tipos comunes
        return originalValue switch
        {
            string s => s.Equals(currentValue as string, StringComparison.Ordinal),
            DateTime dt => dt.Equals(currentValue),
            DateOnly d => d.Equals(currentValue),
            TimeOnly t => t.Equals(currentValue),
            TimeSpan ts => ts.Equals(currentValue),
            Guid g => g.Equals(currentValue),
            _ => originalValue.Equals(currentValue)
        };
    }

    private static Guid? GetCurrentUserId(HttpContext? httpContext)
    {
        var userIdClaim = httpContext?.User.Claims
            .FirstOrDefault(c => c.Type == "UserId")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private static string? GetCurrentUserName(HttpContext? httpContext)
    {
        return httpContext?.User.Claims
            .FirstOrDefault(c => c.Type == "UserName")?.Value;
    }

    private static string? GetIpAddress(HttpContext? httpContext)
    {
        if (httpContext == null) return null;

        // Intentar obtener IP real si está detrás de un proxy/load balancer
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        return httpContext.Connection.RemoteIpAddress?.ToString();
    }
}
