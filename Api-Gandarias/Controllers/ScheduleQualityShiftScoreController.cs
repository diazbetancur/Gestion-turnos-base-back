using CC.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gandarias.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ScheduleQualityShiftScoreController : ControllerBase
{
    private readonly IScheduleQualityShiftScoreService _scheduleQualityShiftScoreService;

    public ScheduleQualityShiftScoreController(IScheduleQualityShiftScoreService scheduleQualityShiftScoreService)
    {
        _scheduleQualityShiftScoreService = scheduleQualityShiftScoreService;
    }

    /// <summary>
    /// GET api/ScheduleQualityShiftScore/week?fecha=2026-03-16
    /// </summary>
    [HttpGet("week")]
    public async Task<IActionResult> GetWeekAsync(DateOnly fecha)
    {
        var result = await _scheduleQualityShiftScoreService.GetByDateUsingLatestTokenAsync(fecha).ConfigureAwait(false);
        return Ok(result);
    }
}
