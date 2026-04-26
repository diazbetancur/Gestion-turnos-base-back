using CC.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gandarias.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ScheduleGapController : ControllerBase
{
    private readonly IScheduleGapService _scheduleGapService;

    public ScheduleGapController(IScheduleGapService scheduleGapService)
    {
        _scheduleGapService = scheduleGapService;
    }

    /// <summary>
    /// GET api/ScheduleGap/week?fechaIni=2025-07-07
    /// </summary>
    [HttpGet("week")]
    public async Task<IActionResult> GetWeekAsync(DateOnly fechaIni)
    {
        if (fechaIni.DayOfWeek != DayOfWeek.Monday)
            return BadRequest("La fecha inicial debe ser lunes para retornar el rango lunes-domingo.");

        var result = await _scheduleGapService.GetWeeklyByMondayAsync(fechaIni).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// DELETE api/ScheduleGap/week/2025-07-07
    /// </summary>
    [HttpDelete("week/{fechaIni}")]
    public async Task<IActionResult> DeleteWeekAsync(DateOnly fechaIni)
    {
        if (fechaIni.DayOfWeek != DayOfWeek.Monday)
            return BadRequest("La fecha inicial debe ser lunes para eliminar el rango lunes-domingo.");

        var fechaFin = fechaIni.AddDays(6);
        var records = await _scheduleGapService.GetAllAsync(x => x.Date >= fechaIni && x.Date <= fechaFin).ConfigureAwait(false);

        if (!records.Any())
            return BadRequest("No hay registros de ScheduleGaps para eliminar en el rango indicado.");

        await _scheduleGapService.DeleteRangeAsync(records).ConfigureAwait(false);
        return Ok("Registros de ScheduleGaps eliminados correctamente.");
    }
}
