# ?? Plan de Mejora: Sistema de Auditoría - UserActivityLogs

## ? Estado: IMPLEMENTADO

> **Fecha de implementación**: 11 de Enero 2026
> **Versión**: 1.0

---

## ?? Qué se Audita y Qué NO

### ? SE AUDITA (Solo operaciones de escritura)

| Operación | Tipo | Cuándo |
|-----------|------|--------|
| **CREATE** | `AuditOperationType.Create` | Al insertar nuevo registro |
| **UPDATE** | `AuditOperationType.Update` | Al modificar campos con cambios reales |
| **SOFT DELETE** | `AuditOperationType.SoftDelete` | Al cambiar IsDeleted de false a true |
| **DELETE** | `AuditOperationType.Delete` | Al eliminar físicamente un registro |

### ? NO SE AUDITA

| Operación | Razón |
|-----------|-------|
| **GET / Lectura** | No hay cambios en datos, evita data basura |
| **Cambios sin diferencias** | Si el valor nuevo es igual al anterior, no se registra |
| **Propiedades de seguridad** | PasswordHash, SecurityStamp, Token, etc. |
| **Propiedades técnicas** | DateCreated, UpdatedAt, AccessFailedCount, etc. |
| **Propiedades de navegación** | Shadow properties, Foreign Keys |

### ?? Propiedades Excluidas de Auditoría

```csharp
// Seguridad
"PasswordHash", "SecurityStamp", "ConcurrencyStamp", 
"PasswordResetToken", "PasswordResetTokenExpiration",
"NormalizedUserName", "NormalizedEmail"

// Timestamps automáticos  
"DateCreated", "DateUpdate", "UpdatedAt", "CreatedAt"

// Técnicas sin valor de negocio
"Token", "IdUpdate", "Notified", "AccessFailedCount",
"LockoutEnd", "LockoutEnabled", "TwoFactorEnabled",
"PhoneNumberConfirmed", "EmailConfirmed"
```

---

## ?? Análisis del Estado Anterior

### Problemas Identificados (YA RESUELTOS)

La implementación actual de auditoría en `UserActivityLogs` tiene las siguientes limitaciones:

| # | Problema | Impacto |
|---|----------|---------|
| 1 | **No se registra el valor anterior (OldValue)** | Imposible saber qué datos se modificaron |
| 2 | **No se registra el valor nuevo (NewValue)** | No hay trazabilidad de cambios |
| 3 | **No se identifica la entidad afectada** | Solo se guarda el endpoint, no la tabla/registro |
| 4 | **No se guarda el ID del registro modificado** | No se puede rastrear qué registro específico cambió |
| 5 | **No hay información del body de la request** | Se pierde el contexto completo de la operación |
| 6 | **No se diferencia entre tipos de operación** | CREATE, UPDATE, DELETE no están claramente separados |

### Código Actual

```csharp
// Entidad actual (muy limitada)
public class UserActivityLog : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public virtual User User { get; set; }
    public string Action { get; set; }      // Solo "POST /api/Schedule"
    public string IpAddress { get; set; }
}

// Middleware actual (no captura datos de cambios)
var log = new UserActivityLog
{
    UserId = new Guid(userId),
    Action = $"{context.Request.Method} {context.Request.Path}",
    IpAddress = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
};
```

---

## ?? Propuesta de Mejora

### Arquitectura Propuesta

```
???????????????????????????????????????????????????????????????????
?                    SISTEMA DE AUDITORÍA                          ?
???????????????????????????????????????????????????????????????????
?                                                                  ?
?  ???????????????    ???????????????    ??????????????????????? ?
?  ?  Controller ????>?   Service   ????>?    Repository       ? ?
?  ?             ?    ?             ?    ?  + AuditInterceptor ? ?
?  ???????????????    ???????????????    ??????????????????????? ?
?                                                    ?            ?
?                                                    ?            ?
?  ????????????????????????????????????????????????????????????????
?  ?                    AuditLog (Nueva tabla)                   ??
?  ?  • EntityName      • EntityId       • OperationType         ??
?  ?  • OldValues       • NewValues      • ChangedColumns        ??
?  ?  • UserId          • UserName       • IpAddress             ??
?  ?  • Timestamp       • RequestPath    • AdditionalInfo        ??
?  ????????????????????????????????????????????????????????????????
?                                                                  ?
???????????????????????????????????????????????????????????????????
```

---

## ?? Plan de Implementación (5 Fases)

### Fase 1: Nueva Entidad de Auditoría

**Archivo:** `CC.Domain/Entities/AuditLog.cs`

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CC.Domain.Entities;

/// <summary>
/// Registro de auditoría con información completa de cambios
/// </summary>
public class AuditLog
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Nombre de la entidad afectada (ej: "Schedule", "User", "Workstation")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string EntityName { get; set; }

    /// <summary>
    /// ID del registro afectado (PK de la entidad)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string EntityId { get; set; }

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
    [MaxLength(500)]
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
    [MaxLength(200)]
    public string? RequestPath { get; set; }

    /// <summary>
    /// Método HTTP (GET, POST, PUT, DELETE)
    /// </summary>
    [MaxLength(10)]
    public string? HttpMethod { get; set; }

    /// <summary>
    /// Información adicional en formato JSON (correlationId, etc.)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? AdditionalInfo { get; set; }

    /// <summary>
    /// Timestamp exacto de la operación
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Duración de la operación en milisegundos
    /// </summary>
    public int? DurationMs { get; set; }

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

/// <summary>
/// Tipos de operación de auditoría
/// </summary>
public enum AuditOperationType
{
    Create = 1,
    Update = 2,
    Delete = 3,
    SoftDelete = 4,
    Read = 5,       // Opcional: para auditar lecturas sensibles
    Login = 6,
    Logout = 7,
    PasswordChange = 8,
    PasswordReset = 9
}
```

---

### Fase 2: Interfaz para Entidades Auditables

**Archivo:** `CC.Domain/Interfaces/IAuditable.cs`

```csharp
namespace CC.Domain.Interfaces;

/// <summary>
/// Interfaz que identifica las entidades que deben ser auditadas
/// </summary>
public interface IAuditable
{
    // Marker interface - las entidades que implementen esto serán auditadas
}

/// <summary>
/// Interfaz para excluir propiedades específicas de la auditoría
/// </summary>
public interface IAuditableWithExclusions : IAuditable
{
    /// <summary>
    /// Propiedades que no deben ser auditadas (ej: Token, PasswordHash)
    /// </summary>
    static abstract string[] ExcludedProperties { get; }
}
```

**Actualizar entidades para implementar IAuditable:**

```csharp
// CC.Domain/Entities/Schedule.cs
public class Schedule : EntityBase<Guid>, IAuditable
{
    // ... propiedades existentes
}

// CC.Domain/Entities/User.cs
public class User : IdentityUser<Guid>, IAuditableWithExclusions
{
    // ... propiedades existentes
    
    public static string[] ExcludedProperties => new[] 
    { 
        "PasswordHash", 
        "SecurityStamp", 
        "ConcurrencyStamp",
        "PasswordResetToken"
    };
}

// CC.Domain/Entities/UserAbsenteeism.cs
public class UserAbsenteeism : EntityBase<Guid>, IAuditable
{
    // ... propiedades existentes
}
```

---

### Fase 3: Servicio de Auditoría

**Archivo:** `CC.Domain/Interfaces/Services/IAuditService.cs`

```csharp
using CC.Domain.Entities;

namespace CC.Domain.Interfaces.Services;

public interface IAuditService
{
    /// <summary>
    /// Registra un cambio en la auditoría
    /// </summary>
    Task LogAsync(AuditLog auditLog);
    
    /// <summary>
    /// Registra múltiples cambios en una sola transacción
    /// </summary>
    Task LogRangeAsync(IEnumerable<AuditLog> auditLogs);
    
    /// <summary>
    /// Obtiene el historial de cambios de una entidad específica
    /// </summary>
    Task<IEnumerable<AuditLog>> GetEntityHistoryAsync(string entityName, string entityId);
    
    /// <summary>
    /// Obtiene los cambios realizados por un usuario
    /// </summary>
    Task<IEnumerable<AuditLog>> GetUserActivityAsync(Guid userId, DateTime? from = null, DateTime? to = null);
}
```

**Archivo:** `CC.Application/Services/AuditService.cs`

```csharp
using CC.Domain.Entities;
using CC.Domain.Interfaces.Services;
using CC.Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;

namespace CC.Application.Services;

public class AuditService : IAuditService
{
    private readonly DBContext _context;

    public AuditService(DBContext context)
    {
        _context = context;
    }

    public async Task LogAsync(AuditLog auditLog)
    {
        auditLog.Id = Guid.NewGuid();
        auditLog.Timestamp = DateTime.UtcNow;
        
        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
    }

    public async Task LogRangeAsync(IEnumerable<AuditLog> auditLogs)
    {
        foreach (var log in auditLogs)
        {
            log.Id = Guid.NewGuid();
            log.Timestamp = DateTime.UtcNow;
        }
        
        _context.AuditLogs.AddRange(auditLogs);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetEntityHistoryAsync(string entityName, string entityId)
    {
        return await _context.AuditLogs
            .Where(a => a.EntityName == entityName && a.EntityId == entityId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetUserActivityAsync(Guid userId, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.AuditLogs.Where(a => a.UserId == userId);
        
        if (from.HasValue)
            query = query.Where(a => a.Timestamp >= from.Value);
        
        if (to.HasValue)
            query = query.Where(a => a.Timestamp <= to.Value);
        
        return await query.OrderByDescending(a => a.Timestamp).ToListAsync();
    }
}
```

---

### Fase 4: Interceptor de EF Core para Auditoría Automática

**Archivo:** `CC.Infrastructure/Interceptors/AuditSaveChangesInterceptor.cs`

```csharp
using System.Text.Json;
using CC.Domain.Entities;
using CC.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CC.Infrastructure.Interceptors;

public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
            return await base.SavingChangesAsync(eventData, result, cancellationToken);

        var auditLogs = GenerateAuditLogs(eventData.Context);
        
        if (auditLogs.Any())
        {
            eventData.Context.Set<AuditLog>().AddRange(auditLogs);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private List<AuditLog> GenerateAuditLogs(DbContext context)
    {
        var auditLogs = new List<AuditLog>();
        var httpContext = _httpContextAccessor.HttpContext;
        
        // Obtener información del usuario actual
        var userId = GetCurrentUserId(httpContext);
        var userName = GetCurrentUserName(httpContext);
        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();
        var userAgent = httpContext?.Request.Headers["User-Agent"].ToString();
        var requestPath = httpContext?.Request.Path.ToString();
        var httpMethod = httpContext?.Request.Method;

        context.ChangeTracker.DetectChanges();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            // Solo auditar entidades que implementen IAuditable
            if (entry.Entity is not IAuditable)
                continue;

            // No auditar la propia tabla de auditoría
            if (entry.Entity is AuditLog)
                continue;

            // Solo auditar cambios relevantes
            if (entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                EntityName = entry.Entity.GetType().Name,
                EntityId = GetPrimaryKeyValue(entry),
                UserId = userId,
                UserName = userName,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                RequestPath = requestPath,
                HttpMethod = httpMethod,
                Timestamp = DateTime.UtcNow,
                IsSuccess = true
            };

            // Obtener propiedades a excluir
            var excludedProperties = GetExcludedProperties(entry.Entity);

            switch (entry.State)
            {
                case EntityState.Added:
                    auditLog.OperationType = AuditOperationType.Create;
                    auditLog.NewValues = SerializeValues(entry.CurrentValues, excludedProperties);
                    auditLog.ChangedColumns = GetPropertyNames(entry.CurrentValues, excludedProperties);
                    break;

                case EntityState.Modified:
                    // Detectar si es SoftDelete
                    var isDeletedProperty = entry.Properties
                        .FirstOrDefault(p => p.Metadata.Name == "IsDeleted" || p.Metadata.Name == "IsDelete");
                    
                    if (isDeletedProperty != null && 
                        isDeletedProperty.IsModified && 
                        (bool)isDeletedProperty.CurrentValue! == true)
                    {
                        auditLog.OperationType = AuditOperationType.SoftDelete;
                    }
                    else
                    {
                        auditLog.OperationType = AuditOperationType.Update;
                    }
                    
                    auditLog.OldValues = SerializeOriginalValues(entry, excludedProperties);
                    auditLog.NewValues = SerializeModifiedValues(entry, excludedProperties);
                    auditLog.ChangedColumns = GetChangedPropertyNames(entry, excludedProperties);
                    break;

                case EntityState.Deleted:
                    auditLog.OperationType = AuditOperationType.Delete;
                    auditLog.OldValues = SerializeValues(entry.OriginalValues, excludedProperties);
                    break;
            }

            auditLogs.Add(auditLog);
        }

        return auditLogs;
    }

    private static string GetPrimaryKeyValue(EntityEntry entry)
    {
        var keyProperty = entry.Properties
            .FirstOrDefault(p => p.Metadata.IsPrimaryKey());
        
        return keyProperty?.CurrentValue?.ToString() ?? "Unknown";
    }

    private static string[] GetExcludedProperties(object entity)
    {
        if (entity is IAuditableWithExclusions auditableWithExclusions)
        {
            // Usar reflection para obtener la propiedad estática
            var type = entity.GetType();
            var property = type.GetProperty("ExcludedProperties", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            
            if (property != null)
            {
                return (string[])property.GetValue(null)!;
            }
        }
        
        // Propiedades comunes a excluir siempre
        return new[] { "DateCreated", "DateUpdate", "ConcurrencyStamp", "SecurityStamp" };
    }

    private static string SerializeValues(PropertyValues values, string[] excludedProperties)
    {
        var dict = new Dictionary<string, object?>();
        
        foreach (var property in values.Properties)
        {
            if (excludedProperties.Contains(property.Name))
                continue;
                
            dict[property.Name] = values[property];
        }
        
        return JsonSerializer.Serialize(dict, new JsonSerializerOptions 
        { 
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
    }

    private static string SerializeOriginalValues(EntityEntry entry, string[] excludedProperties)
    {
        var dict = new Dictionary<string, object?>();
        
        foreach (var property in entry.Properties.Where(p => p.IsModified))
        {
            if (excludedProperties.Contains(property.Metadata.Name))
                continue;
                
            dict[property.Metadata.Name] = property.OriginalValue;
        }
        
        return JsonSerializer.Serialize(dict, new JsonSerializerOptions 
        { 
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
    }

    private static string SerializeModifiedValues(EntityEntry entry, string[] excludedProperties)
    {
        var dict = new Dictionary<string, object?>();
        
        foreach (var property in entry.Properties.Where(p => p.IsModified))
        {
            if (excludedProperties.Contains(property.Metadata.Name))
                continue;
                
            dict[property.Metadata.Name] = property.CurrentValue;
        }
        
        return JsonSerializer.Serialize(dict, new JsonSerializerOptions 
        { 
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
    }

    private static string GetPropertyNames(PropertyValues values, string[] excludedProperties)
    {
        return string.Join(",", values.Properties
            .Where(p => !excludedProperties.Contains(p.Name))
            .Select(p => p.Name));
    }

    private static string GetChangedPropertyNames(EntityEntry entry, string[] excludedProperties)
    {
        return string.Join(",", entry.Properties
            .Where(p => p.IsModified && !excludedProperties.Contains(p.Metadata.Name))
            .Select(p => p.Metadata.Name));
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
}
```

---

### Fase 5: Configuración e Integración

#### 5.1 Actualizar DBContext

**Archivo:** `CC.Infrastructure/Configurations/DBContext.cs` (agregar al archivo existente)

```csharp
// Agregar el DbSet
public DbSet<AuditLog> AuditLogs { get; set; }

// En OnModelCreating, agregar:
modelBuilder.Entity<AuditLog>().HasKey(c => c.Id);
modelBuilder.Entity<AuditLog>().Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
modelBuilder.Entity<AuditLog>().Property(e => e.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP");

// Índices para mejorar consultas
modelBuilder.Entity<AuditLog>().HasIndex(a => a.EntityName);
modelBuilder.Entity<AuditLog>().HasIndex(a => a.EntityId);
modelBuilder.Entity<AuditLog>().HasIndex(a => a.UserId);
modelBuilder.Entity<AuditLog>().HasIndex(a => a.Timestamp);
modelBuilder.Entity<AuditLog>().HasIndex(a => new { a.EntityName, a.EntityId });
```

#### 5.2 Registrar el Interceptor en DI

**Archivo:** `Api-Gandarias/Handlers/DependencyInyectionHandler.cs` (modificar)

```csharp
// Agregar HttpContextAccessor
services.AddHttpContextAccessor();

// Modificar la configuración de DbContext
services.AddDbContext<DBContext>((serviceProvider, opt) =>
{
    var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
    
    opt.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
        npgsqlOptions.CommandTimeout(300);
        npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    });

    // Agregar el interceptor de auditoría
    opt.AddInterceptors(new AuditSaveChangesInterceptor(httpContextAccessor));

    opt.EnableSensitiveDataLogging(environment == "Development");
    opt.EnableDetailedErrors(environment == "Development");
    opt.UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);
});

// Registrar el servicio de auditoría
services.AddScoped<IAuditService, AuditService>();
```

#### 5.3 Crear Migración

```bash
dotnet ef migrations add AddAuditLogTable --project CC.Infraestructure --startup-project Api-Gandarias
dotnet ef database update --project CC.Infraestructure --startup-project Api-Gandarias
```

---

## ?? Ejemplo de Datos Auditados

### Antes (Sistema Actual)

```json
{
  "Id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "UserId": "user-uuid",
  "Action": "PUT /api/Schedule/abc123",
  "IpAddress": "192.168.1.100",
  "DateCreated": "2025-01-13T10:30:00Z"
}
```

**Problemas:** No sabemos qué cambió, cuáles eran los valores anteriores, ni cuáles son los nuevos.

### Después (Sistema Mejorado)

```json
{
  "Id": "audit-uuid",
  "EntityName": "Schedule",
  "EntityId": "abc123",
  "OperationType": "Update",
  "OldValues": {
    "StartTime": "09:00:00",
    "EndTime": "17:00:00",
    "WorkstationId": "ws-001",
    "Observation": null
  },
  "NewValues": {
    "StartTime": "10:00:00",
    "EndTime": "18:00:00",
    "WorkstationId": "ws-002",
    "Observation": "Cambio de turno solicitado"
  },
  "ChangedColumns": "StartTime,EndTime,WorkstationId,Observation",
  "UserId": "admin-uuid",
  "UserName": "Admin",
  "IpAddress": "192.168.1.100",
  "UserAgent": "Mozilla/5.0...",
  "RequestPath": "/api/Schedule/abc123",
  "HttpMethod": "PUT",
  "Timestamp": "2025-01-13T10:30:00Z",
  "DurationMs": 45,
  "IsSuccess": true
}
```

---

## ?? API de Consulta de Auditoría

### Nuevo Controller para Auditoría

**Archivo:** `Api-Gandarias/Controllers/AuditController.cs`

```csharp
using CC.Domain.Entities;
using CC.Domain.Enums;
using CC.Domain.Helpers;
using CC.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gandarias.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    /// <summary>
    /// Obtiene el historial de cambios de una entidad específica
    /// GET api/Audit/entity/Schedule/abc123
    /// </summary>
    [HttpGet("entity/{entityName}/{entityId}")]
    public async Task<IActionResult> GetEntityHistory(string entityName, string entityId)
    {
        // Solo administradores pueden ver auditoría
        var userRole = User.GetRoles();
        if (!userRole.Any(x => x == RoleType.Admin.ToString()))
        {
            return Unauthorized("Solo los administradores pueden ver la auditoría.");
        }

        var history = await _auditService.GetEntityHistoryAsync(entityName, entityId);
        return Ok(history);
    }

    /// <summary>
    /// Obtiene la actividad de un usuario específico
    /// GET api/Audit/user/user-uuid?from=2025-01-01&to=2025-01-31
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserActivity(
        Guid userId, 
        [FromQuery] DateTime? from = null, 
        [FromQuery] DateTime? to = null)
    {
        var userRole = User.GetRoles();
        if (!userRole.Any(x => x == RoleType.Admin.ToString()))
        {
            return Unauthorized("Solo los administradores pueden ver la auditoría.");
        }

        var activity = await _auditService.GetUserActivityAsync(userId, from, to);
        return Ok(activity);
    }
}
```

---

## ?? Checklist de Implementación

### Fase 1: Entidad de Auditoría
- [x] Crear `AuditLog.cs` en `CC.Domain/Entities` ?
- [x] Crear enum `AuditOperationType` en `CC.Domain/Enums` ?
- [x] Agregar `DbSet<AuditLog>` al `DBContext` ?
- [x] Configurar índices en `OnModelCreating` ?

### Fase 2: Interfaces
- [x] Crear `IAuditable.cs` en `CC.Domain/Interfaces` ?
- [x] Implementar `IAuditable` en entidades principales ?
  - Schedule, User, UserAbsenteeism, Signing, Workstation, WorkArea

### Fase 3: Servicio de Auditoría
- [x] Consultas implementadas directamente en `AuditController` ?
- [x] No se requiere servicio adicional (reutiliza DBContext) ?

### Fase 4: Interceptor
- [x] Crear `AuditSaveChangesInterceptor.cs` ?
- [x] Configurar en el DbContext (via DependencyInyectionHandler) ?
- [x] Agregar `IHttpContextAccessor` ?

### Fase 5: Integración
- [x] Crear migración de base de datos (`AddAuditLogTable`) ?
- [x] Aplicar migración a la base de datos ?
- [x] Crear `AuditController.cs` ?
- [x] Middleware existente se mantiene como backup ?

### Testing (Pendiente de validación manual)
- [ ] Test de Create: verificar que se registra la creación
- [ ] Test de Update: verificar OldValues y NewValues
- [ ] Test de SoftDelete: verificar que detecta IsDeleted
- [ ] Test de Delete: verificar que registra eliminación física
- [ ] Test de exclusión de propiedades sensibles

---

## ? Archivos Creados/Modificados

### Archivos Nuevos
| Archivo | Descripción |
|---------|-------------|
| `CC.Domain/Enums/AuditOperationType.cs` | Enum con tipos de operación |
| `CC.Domain/Entities/AuditLog.cs` | Entidad de auditoría |
| `CC.Domain/Interfaces/IAuditable.cs` | Interface marcadora |
| `CC.Infrastructure/Interceptors/AuditSaveChangesInterceptor.cs` | Interceptor EF Core |
| `Api-Gandarias/Controllers/AuditController.cs` | API de consulta |
| `CC.Infrastructure/Migrations/AddAuditLogTable.cs` | Migración BD |

### Archivos Modificados
| Archivo | Cambio |
|---------|--------|
| `CC.Infrastructure/Configurations/DBContext.cs` | Agregado DbSet y configuración AuditLog |
| `Api-Gandarias/Handlers/DependencyInyectionHandler.cs` | Agregado HttpContextAccessor e Interceptor |
| `CC.Domain/Entities/Schedule.cs` | Implementa IAuditable |
| `CC.Domain/Entities/User.cs` | Implementa IAuditable |
| `CC.Domain/Entities/UserAbsenteeism.cs` | Implementa IAuditable |
| `CC.Domain/Entities/Signing.cs` | Implementa IAuditable |
| `CC.Domain/Entities/Workstation.cs` | Implementa IAuditable |
| `CC.Domain/Entities/WorkArea.cs` | Implementa IAuditable |

---

## ?? Beneficios Esperados

| Aspecto | Antes | Después |
|---------|-------|---------|
| **Trazabilidad** | Solo endpoint | Entidad + ID + Valores |
| **Valores anteriores** | ? No | ? JSON completo |
| **Valores nuevos** | ? No | ? JSON completo |
| **Campos modificados** | ? No | ? Lista específica |
| **Tipo de operación** | Implícito | Explícito (Create/Update/Delete) |
| **Seguridad** | ? Registra todo | ? Excluye campos sensibles |
| **Consultas** | Por endpoint | Por entidad/usuario/fecha |
| **Automatización** | Manual en middleware | Automático con interceptor |

---

## ?? Consideraciones de Rendimiento

1. **Índices**: Crear índices en `EntityName`, `EntityId`, `UserId`, `Timestamp`
2. **Particionamiento**: Considerar particionar por fecha para tablas grandes
3. **Archivado**: Implementar política de retención (mover logs > 1 año a tabla histórica)
4. **Async**: Todas las operaciones son asíncronas para no bloquear
5. **Tamaño JSON**: Limitar tamaño de `OldValues`/`NewValues` a campos relevantes

---

## ?? Migración de Datos Existentes

Para mantener compatibilidad con los logs existentes:

```sql
-- Migrar datos de UserActivityLogs a AuditLogs (opcional)
INSERT INTO "Management"."AuditLogs" (
    "Id", "EntityName", "EntityId", "OperationType", 
    "UserId", "IpAddress", "RequestPath", "HttpMethod", "Timestamp"
)
SELECT 
    "Id",
    CASE 
        WHEN "Action" LIKE '%Schedule%' THEN 'Schedule'
        WHEN "Action" LIKE '%User%' THEN 'User'
        ELSE 'Unknown'
    END as "EntityName",
    'legacy' as "EntityId",
    CASE 
        WHEN "Action" LIKE 'POST%' THEN 1
        WHEN "Action" LIKE 'PUT%' THEN 2
        WHEN "Action" LIKE 'DELETE%' THEN 3
        ELSE 2
    END as "OperationType",
    "UserId",
    "IpAddress",
    split_part("Action", ' ', 2) as "RequestPath",
    split_part("Action", ' ', 1) as "HttpMethod",
    "DateCreated"
FROM "Management"."UserActivityLogs";
```

---

## ?? Próximos Pasos

1. **Revisar y aprobar** este plan de mejora
2. **Priorizar** qué entidades deben ser auditables primero
3. **Implementar** en el orden de las fases
4. **Testing** exhaustivo antes de producción
5. **Documentar** API de auditoría en Swagger

¿Deseas que proceda con la implementación de alguna fase específica?
