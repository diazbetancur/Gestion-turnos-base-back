using CC.Domain.Entities;
using CC.Domain.Enums;
using CC.Domain.Helpers;
using CC.Infrastructure.Configurations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gandarias.Controllers;

/// <summary>
/// Controller para consultar los registros de auditoría del sistema.
/// Solo accesible por administradores.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class AuditController : ControllerBase
{
    private readonly DBContext _context;

    public AuditController(DBContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Obtiene el historial de cambios de una entidad específica.
    /// GET api/Audit/entity/Schedule/abc123-uuid
    /// </summary>
    /// <param name="entityName">Nombre de la entidad (ej: Schedule, User, Workstation)</param>
    /// <param name="entityId">ID del registro</param>
    /// <returns>Lista de cambios ordenados por fecha descendente</returns>
    [HttpGet("entity/{entityName}/{entityId}")]
    public async Task<IActionResult> GetEntityHistory(string entityName, string entityId)
    {
        // Solo administradores pueden ver auditoría
        var userRole = User.GetRoles();
        if (!userRole.Any(x => x == RoleType.Admin.ToString()))
        {
            return Unauthorized("Solo los administradores pueden ver la auditoría.");
        }

        var history = await _context.AuditLogs
            .Where(a => a.EntityName == entityName && a.EntityId == entityId)
            .OrderByDescending(a => a.Timestamp)
            .Select(a => new
            {
                a.Id,
                a.EntityName,
                a.EntityId,
                OperationType = a.OperationType.ToString(),
                a.OldValues,
                a.NewValues,
                a.ChangedColumns,
                a.UserId,
                a.UserName,
                a.Timestamp,
                a.RequestPath,
                a.HttpMethod,
                a.IpAddress
            })
            .ToListAsync();

        return Ok(history);
    }

    /// <summary>
    /// Obtiene la actividad de un usuario específico.
    /// GET api/Audit/user/user-uuid?from=2025-01-01&to=2025-01-31
    /// </summary>
    /// <param name="userId">ID del usuario</param>
    /// <param name="from">Fecha inicio (opcional)</param>
    /// <param name="to">Fecha fin (opcional)</param>
    /// <param name="take">Cantidad de registros (default: 100)</param>
    /// <returns>Lista de actividades del usuario</returns>
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserActivity(
        Guid userId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int take = 100)
    {
        var userRole = User.GetRoles();
        if (!userRole.Any(x => x == RoleType.Admin.ToString()))
        {
            return Unauthorized("Solo los administradores pueden ver la auditoría.");
        }

        var query = _context.AuditLogs.Where(a => a.UserId == userId);

        if (from.HasValue)
            query = query.Where(a => a.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.Timestamp <= to.Value);

        var activity = await query
            .OrderByDescending(a => a.Timestamp)
            .Take(take)
            .Select(a => new
            {
                a.Id,
                a.EntityName,
                a.EntityId,
                OperationType = a.OperationType.ToString(),
                a.ChangedColumns,
                a.Timestamp,
                a.RequestPath,
                a.HttpMethod,
                a.IsSuccess
            })
            .ToListAsync();

        return Ok(activity);
    }

    /// <summary>
    /// Obtiene los últimos registros de auditoría del sistema.
    /// GET api/Audit/recent?take=50
    /// </summary>
    /// <param name="take">Cantidad de registros (default: 50, max: 500)</param>
    /// <param name="entityName">Filtrar por nombre de entidad (opcional)</param>
    /// <returns>Lista de registros recientes</returns>
    [HttpGet("recent")]
    public async Task<IActionResult> GetRecentAuditLogs(
        [FromQuery] int take = 50,
        [FromQuery] string? entityName = null)
    {
        var userRole = User.GetRoles();
        if (!userRole.Any(x => x == RoleType.Admin.ToString()))
        {
            return Unauthorized("Solo los administradores pueden ver la auditoría.");
        }

        take = Math.Min(take, 500); // Limitar a máximo 500

        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityName))
            query = query.Where(a => a.EntityName == entityName);

        var logs = await query
            .OrderByDescending(a => a.Timestamp)
            .Take(take)
            .Select(a => new
            {
                a.Id,
                a.EntityName,
                a.EntityId,
                OperationType = a.OperationType.ToString(),
                a.ChangedColumns,
                a.UserId,
                a.UserName,
                a.Timestamp,
                a.RequestPath,
                a.HttpMethod,
                a.IpAddress,
                a.IsSuccess
            })
            .ToListAsync();

        return Ok(logs);
    }

    /// <summary>
    /// Obtiene el detalle completo de un registro de auditoría.
    /// GET api/Audit/detail/audit-uuid
    /// </summary>
    /// <param name="id">ID del registro de auditoría</param>
    /// <returns>Detalle completo incluyendo OldValues y NewValues</returns>
    [HttpGet("detail/{id}")]
    public async Task<IActionResult> GetAuditDetail(Guid id)
    {
        var userRole = User.GetRoles();
        if (!userRole.Any(x => x == RoleType.Admin.ToString()))
        {
            return Unauthorized("Solo los administradores pueden ver la auditoría.");
        }

        var audit = await _context.AuditLogs
            .Where(a => a.Id == id)
            .Select(a => new
            {
                a.Id,
                a.EntityName,
                a.EntityId,
                OperationType = a.OperationType.ToString(),
                a.OldValues,
                a.NewValues,
                a.ChangedColumns,
                a.UserId,
                a.UserName,
                a.Timestamp,
                a.RequestPath,
                a.HttpMethod,
                a.IpAddress,
                a.UserAgent,
                a.IsSuccess,
                a.ErrorMessage
            })
            .FirstOrDefaultAsync();

        if (audit == null)
            return NotFound("Registro de auditoría no encontrado.");

        return Ok(audit);
    }

    /// <summary>
    /// Obtiene estadísticas de auditoría por entidad.
    /// GET api/Audit/stats?from=2025-01-01&to=2025-01-31
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetAuditStats(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var userRole = User.GetRoles();
        if (!userRole.Any(x => x == RoleType.Admin.ToString()))
        {
            return Unauthorized("Solo los administradores pueden ver la auditoría.");
        }

        var query = _context.AuditLogs.AsQueryable();

        if (from.HasValue)
            query = query.Where(a => a.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.Timestamp <= to.Value);

        var stats = await query
            .GroupBy(a => new { a.EntityName, a.OperationType })
            .Select(g => new
            {
                g.Key.EntityName,
                OperationType = g.Key.OperationType.ToString(),
                Count = g.Count()
            })
            .OrderBy(s => s.EntityName)
            .ThenBy(s => s.OperationType)
            .ToListAsync();

        var totalRecords = await query.CountAsync();

        return Ok(new
        {
            TotalRecords = totalRecords,
            StatsByEntityAndOperation = stats,
            Period = new { From = from, To = to }
        });
    }
}
