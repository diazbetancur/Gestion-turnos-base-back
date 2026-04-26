using Amazon.Lambda;
using Amazon.Lambda.Model;
using CC.Domain.Dtos;
using CC.Domain.Entities;
using CC.Domain.Enums;
using CC.Domain.Helpers;
using CC.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Gandarias.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ScheduleController : ControllerBase
{
    private readonly IScheduleService _scheduleService;
    private readonly IUserWorkstationService _userWorkstationService;
    private readonly IEmployeeScheduleExceptionService _employeeScheduleExceptionService;
    private readonly IEmployeeScheduleRestrictionService _employeeScheduleRestrictionService;
    private readonly IEmployeeShiftTypeRestrictionService _employeeShiftTypeRestrictionService;
    private readonly IShiftTypeService _shiftTypeService;
    private readonly IUserAbsenteeismService _userAbsenteeismService;
    private readonly IIgnoredRestrictionService _ignoredRestrictionService;
    private readonly IEmailService _emailService;
    private readonly IQrCodeService _qrCodeService;
    private readonly IScheduleGapService _scheduleGapService;
    private readonly IScheduleSuggestionService _scheduleSuggestionService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public ScheduleController(IScheduleService scheduleService,
        IUserWorkstationService userWorkstationService,
        IEmployeeScheduleExceptionService employeeScheduleExceptionService,
        IEmployeeScheduleRestrictionService employeeScheduleRestrictionService,
        IEmployeeShiftTypeRestrictionService employeeShiftTypeRestrictionService,
        IShiftTypeService shiftTypeService,
        IUserAbsenteeismService userAbsenteeismService,
        IIgnoredRestrictionService ignoredRestrictionService,
        IEmailService emailService, IQrCodeService qrCodeService,
        IScheduleGapService scheduleGapService,
        IScheduleSuggestionService scheduleSuggestionService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _scheduleService = scheduleService;
        _userWorkstationService = userWorkstationService;
        _employeeScheduleExceptionService = employeeScheduleExceptionService;
        _employeeScheduleRestrictionService = employeeScheduleRestrictionService;
        _employeeShiftTypeRestrictionService = employeeShiftTypeRestrictionService;
        _shiftTypeService = shiftTypeService;
        _userAbsenteeismService = userAbsenteeismService;
        _ignoredRestrictionService = ignoredRestrictionService;
        _emailService = emailService;
        _qrCodeService = qrCodeService;
        _scheduleGapService = scheduleGapService;
        _scheduleSuggestionService = scheduleSuggestionService;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    /// <summary>
    /// GET api/Schedule?fechaIni=2025-07-01
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<IActionResult> GetAllAsync(DateOnly fechaIni)
    {
        try
        {
            var userRole = User.GetRoles();
            Guid? userId = null;
            if (!userRole.Any(x => x == RoleType.Admin.ToString()))
            {
                userId = User.GetUserId();
            }

            DateOnly fechaFin = fechaIni.AddDays(6);
            var results = await _scheduleService.GetAllAsync(x => !x.IsDeleted &&
            x.Date >= fechaIni &&
            x.Date <= fechaFin &&
            (!userId.HasValue || x.UserId == userId), includeProperties: "User,Workstation.WorkArea").ConfigureAwait(false);

            return Ok(results);

            //if (!results.Any() && userRole.Any(x => x == RoleType.Admin.ToString()))
            //{
            //    results = await AutomaticSchedule(fechaIni);
            //}

            //return Ok(results);
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// GET api/Schedule?fechaIni=2025-07-01
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("ByUserId")]
    public async Task<IActionResult> GetByIdAsync(DateOnly fechaIni, Guid UserId)
    {
        DateOnly fechaFin = fechaIni.AddDays(6);
        return Ok(await _scheduleService.GetAllAsync(x => !x.IsDeleted &&
        x.Date >= fechaIni &&
        x.Date <= fechaFin &&
        x.UserId == UserId, includeProperties: "User,Workstation.WorkArea").ConfigureAwait(false));
    }

    /// <summary>
    /// POST api/Schedule
    /// </summary>
    /// <param name="ScheduleDto"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> Post(ScheduleDto scheduleDto)
    {
        if (scheduleDto.StartTime >= scheduleDto.EndTime)
            return BadRequest("La hora de inicio debe ser menor que la hora de fin.");

        var absSchedules = await _scheduleService.GetAllAsync(x =>
            x.UserId == scheduleDto.UserId &&
            x.Date == scheduleDto.Date &&
            x.Observation != null &&
            x.Observation.ToUpper().Contains("ABS")).ConfigureAwait(false);

        if (absSchedules.Any())
            await _scheduleService.DeleteRangeAsync(absSchedules).ConfigureAwait(false);

        var conflict = await _scheduleService.GetAllAsync(x =>
            !x.IsDeleted &&
            x.Date == scheduleDto.Date &&
            x.UserId == scheduleDto.UserId &&
            x.WorkstationId == scheduleDto.WorkstationId
            &&
            (
                (scheduleDto.StartTime >= x.StartTime && scheduleDto.StartTime < x.EndTime) ||
                (scheduleDto.EndTime > x.StartTime && scheduleDto.EndTime <= x.EndTime) ||
                (scheduleDto.StartTime <= x.StartTime && scheduleDto.EndTime >= x.EndTime)
            )
        ).ConfigureAwait(false);

        if (conflict.Any())
            return BadRequest("Ya existe un horario para este usuario con ese puesto de trabajo en ese rango de tiempo.");

        await RegisterIgnoredRestrictionsAsync(scheduleDto).ConfigureAwait(false);

        await _scheduleService.AddAsync(scheduleDto).ConfigureAwait(false);
        return Ok(scheduleDto);
    }

    private async Task RegisterIgnoredRestrictionsAsync(ScheduleDto scheduleDto)
    {
        try
        {
            await ValidateUserWorkstationAsync(scheduleDto).ConfigureAwait(false);
        }
        catch
        {
        }

        try
        {
            await ValidateScheduleExceptionsAsync(scheduleDto).ConfigureAwait(false);
        }
        catch
        {
        }

        try
        {
            await ValidateScheduleRestrictionsAsync(scheduleDto).ConfigureAwait(false);
        }
        catch
        {
        }

        try
        {
            await ValidateShiftTypeRestrictionsAsync(scheduleDto).ConfigureAwait(false);
        }
        catch
        {
        }

        try
        {
            await ValidateUserAbsenteeismAsync(scheduleDto).ConfigureAwait(false);
        }
        catch
        {
        }
    }

    private async Task ValidateUserWorkstationAsync(ScheduleDto scheduleDto)
    {
        var userWorkstations = (await _userWorkstationService
            .GetAllAsync(x => x.UserId == scheduleDto.UserId && !x.IsDelete, includeProperties: "Workstation")
            .ConfigureAwait(false)).ToList();

        if (userWorkstations.Any(x => x.WorkstationId == scheduleDto.WorkstationId))
            return;

        var workstationData = userWorkstations.Select(x => new
        {
            x.WorkstationId,
            x.WorkstationName
        }).ToList();

        await SaveIgnoredRestrictionAsync(
            scheduleDto.Date,
            scheduleDto.UserId,
            "Usuario no tiene ese puesto de trabajo",
            "UserWorkstation",
            workstationData).ConfigureAwait(false);
    }

    private async Task ValidateScheduleExceptionsAsync(ScheduleDto scheduleDto)
    {
        var exceptions = await _employeeScheduleExceptionService
            .GetAllAsync(x => x.UserId == scheduleDto.UserId && x.Date == scheduleDto.Date)
            .ConfigureAwait(false);

        foreach (var exception in exceptions)
        {
            if (!TryParseRestrictionType(exception.RestrictionType, out var restrictionType))
                continue;

            if (IsRestrictionViolated(scheduleDto, restrictionType, exception.AvailableFrom, exception.AvailableUntil, exception.Block1Start, exception.Block1End, exception.Block2Start, exception.Block2End))
            {
                await SaveIgnoredRestrictionAsync(
                    scheduleDto.Date,
                    scheduleDto.UserId,
                    "Usuario con novedad",
                    "EmployeeScheduleException",
                    exception).ConfigureAwait(false);
            }
        }
    }

    private async Task ValidateScheduleRestrictionsAsync(ScheduleDto scheduleDto)
    {
        var dayOfWeek = scheduleDto.Date.DayOfWeek;
        var restrictions = await _employeeScheduleRestrictionService
            .GetAllAsync(x => x.UserId == scheduleDto.UserId && x.DayOfWeek == dayOfWeek)
            .ConfigureAwait(false);

        foreach (var restriction in restrictions)
        {
            if (!TryParseRestrictionType(restriction.RestrictionType, out var restrictionType))
                continue;

            if (IsRestrictionViolated(scheduleDto, restrictionType, restriction.AvailableFrom, restriction.AvailableUntil, restriction.Block1Start, restriction.Block1End, restriction.Block2Start, restriction.Block2End))
            {
                await SaveIgnoredRestrictionAsync(
                    scheduleDto.Date,
                    scheduleDto.UserId,
                    "Usuario con restriccion semanal",
                    "EmployeeScheduleRestriction",
                    restriction).ConfigureAwait(false);
            }
        }
    }

    private async Task ValidateShiftTypeRestrictionsAsync(ScheduleDto scheduleDto)
    {
        var dayOfWeek = scheduleDto.Date.DayOfWeek;
        var shiftRestrictions = (await _employeeShiftTypeRestrictionService
            .GetAllAsync(x => x.UserId == scheduleDto.UserId && x.DayOfWeek == dayOfWeek, includeProperties: "ShiftType")
            .ConfigureAwait(false)).ToList();

        if (!shiftRestrictions.Any())
            return;

        var isWithinAnyShift = false;
        foreach (var shiftRestriction in shiftRestrictions)
        {
            var shiftType = await _shiftTypeService.FindByIdAsync(shiftRestriction.ShiftTypeId).ConfigureAwait(false);
            if (shiftType == null)
                continue;

            if (IsWithinShiftTypeRange(scheduleDto, shiftType))
            {
                isWithinAnyShift = true;
                break;
            }
        }

        if (isWithinAnyShift)
            return;

        await SaveIgnoredRestrictionAsync(
            scheduleDto.Date,
            scheduleDto.UserId,
            "Usuario con restriccion de tipo de turno",
            "EmployeeShiftTypeRestriction",
            shiftRestrictions).ConfigureAwait(false);
    }

    private async Task ValidateUserAbsenteeismAsync(ScheduleDto scheduleDto)
    {
        var absenteeismList = await _userAbsenteeismService
            .GetAllAsync(x => x.UserId == scheduleDto.UserId && x.StartDate <= scheduleDto.Date && (x.EndDate == null || x.EndDate >= scheduleDto.Date))
            .ConfigureAwait(false);

        foreach (var absenteeism in absenteeismList)
        {
            await SaveIgnoredRestrictionAsync(
                scheduleDto.Date,
                scheduleDto.UserId,
                "Usuario con ausentismo",
                "UserAbsenteeism",
                absenteeism).ConfigureAwait(false);
        }
    }

    private async Task SaveIgnoredRestrictionAsync(DateOnly date, Guid userId, string message, string source, object data)
    {
        var payload = JsonSerializer.Serialize(new
        {
            source,
            message,
            data
        });

        await _ignoredRestrictionService.ExecuteSqlCommandAsync(
            "INSERT INTO \"Management\".\"IgnoredRestrictions\" (\"Date\", \"Observation\", \"UserId\") VALUES ({0}, {1}, {2})",
            date,
            payload,
            userId).ConfigureAwait(false);
    }

    private static bool IsRestrictionViolated(
        ScheduleDto scheduleDto,
        RestrictionType restrictionType,
        TimeSpan? availableFrom,
        TimeSpan? availableUntil,
        TimeSpan? block1Start,
        TimeSpan? block1End,
        TimeSpan? block2Start,
        TimeSpan? block2End)
    {
        if (!scheduleDto.StartTime.HasValue || !scheduleDto.EndTime.HasValue)
            return false;

        var scheduleStart = scheduleDto.StartTime.Value;
        var scheduleEnd = scheduleDto.EndTime.Value;

        switch (restrictionType)
        {
            case RestrictionType.NotWorking:
                return true;

            case RestrictionType.AvailableFrom:
                return availableFrom.HasValue && scheduleStart < availableFrom.Value;

            case RestrictionType.AvailableUntil:
                return availableUntil.HasValue && scheduleEnd > availableUntil.Value;

            case RestrictionType.AvailableBetween:
                var insideBlock1 = IsWithinRange(scheduleStart, scheduleEnd, block1Start, block1End);
                var insideBlock2 = IsWithinRange(scheduleStart, scheduleEnd, block2Start, block2End);
                return !insideBlock1 && !insideBlock2;

            case RestrictionType.NotAvailableBetween:
                var overlapsBlock1 = OverlapsRange(scheduleStart, scheduleEnd, block1Start, block1End);
                var overlapsBlock2 = OverlapsRange(scheduleStart, scheduleEnd, block2Start, block2End);
                return overlapsBlock1 || overlapsBlock2;

            default:
                return false;
        }
    }

    private static bool IsWithinShiftTypeRange(ScheduleDto scheduleDto, ShiftTypeDto shiftType)
    {
        if (!scheduleDto.StartTime.HasValue || !scheduleDto.EndTime.HasValue)
            return false;

        var scheduleStart = scheduleDto.StartTime.Value;
        var scheduleEnd = scheduleDto.EndTime.Value;

        var block1Ok = IsWithinRange(scheduleStart, scheduleEnd, shiftType.Block1Start, shiftType.Block1lastStart);
        var block2Ok = IsWithinRange(scheduleStart, scheduleEnd, shiftType.Block2Start, shiftType.Block2lastStart);

        return block1Ok || block2Ok;
    }

    private static bool IsWithinRange(TimeSpan start, TimeSpan end, TimeSpan? rangeStart, TimeSpan? rangeEnd)
    {
        if (!rangeStart.HasValue || !rangeEnd.HasValue)
            return false;

        return start >= rangeStart.Value && end <= rangeEnd.Value;
    }

    private static bool OverlapsRange(TimeSpan start, TimeSpan end, TimeSpan? rangeStart, TimeSpan? rangeEnd)
    {
        if (!rangeStart.HasValue || !rangeEnd.HasValue)
            return false;

        return start < rangeEnd.Value && end > rangeStart.Value;
    }

    private static bool TryParseRestrictionType(string? restrictionTypeValue, out RestrictionType restrictionType)
    {
        restrictionType = RestrictionType.FullyAvailable;

        if (string.IsNullOrWhiteSpace(restrictionTypeValue))
            return false;

        return Enum.TryParse(restrictionTypeValue, true, out restrictionType);
    }

    /// <summary>
    /// PUT api/Schedule/c5b257e0-e73f-4f34-a30c-c0e139ad8e58
    /// </summary>
    /// <param name="id"></param>
    /// <param name="ScheduleDto"></param>
    /// <returns></returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(Guid id, ScheduleDto scheduleDto)
    {
        if (scheduleDto.StartTime >= scheduleDto.EndTime)
            return BadRequest("La hora de inicio debe ser menor que la hora de fin.");

        var conflict = await _scheduleService.GetAllAsync(x =>
            !x.IsDeleted &&
            x.Id != id &&
            x.Date == scheduleDto.Date &&
            x.UserId == scheduleDto.UserId &&
            x.WorkstationId == scheduleDto.WorkstationId &&
            (
                (scheduleDto.StartTime >= x.StartTime && scheduleDto.StartTime < x.EndTime) ||
                (scheduleDto.EndTime > x.StartTime && scheduleDto.EndTime <= x.EndTime) ||
                (scheduleDto.StartTime <= x.StartTime && scheduleDto.EndTime >= x.EndTime)
            )
        ).ConfigureAwait(false);

        if (conflict.Any())
            return BadRequest("Ya existe un horario para este usuario con ese puesto de trabajo en ese rango de tiempo.");

        scheduleDto.Id = id;
        await _scheduleService.UpdateAsync(scheduleDto).ConfigureAwait(false);
        return Ok(scheduleDto);
    }

    /// <summary>
    /// DELETE api/Schedule/c5b257e0-e73f-4f34-a30c-c0e139ad8e58
    /// </summary>
    /// <param name="ScheduleDto"></param>
    /// <returns></returns>
    [HttpDelete]
    public async Task<IActionResult> Delete(ScheduleDto scheduleDto)
    {
        scheduleDto.IsDeleted = true;
        await _scheduleService.DeleteAsync(scheduleDto).ConfigureAwait(false);
        return Ok(scheduleDto);
    }

    /// <summary>
    /// POST api/Schedule/notify?fechaIni=2025-07-01&fechaFin=2025-07-31
    /// </summary>
    /// <param name="fechaIni"></param>
    /// <returns></returns>
    [HttpDelete("{fechaIni}")]
    public async Task<IActionResult> DeleteAsync(DateOnly fechaIni)
    {
        var userRole = User.GetRoles();
        if (!userRole.Any(x => x == RoleType.Admin.ToString()))
        {
            return Unauthorized("Solo los administradores pueden eliminar horarios.");
        }

        var fechaFin = fechaIni.AddDays(6);

        try
        {
            var schedules = await _scheduleService
                .GetAllAsync(x => x.Date >= fechaIni && x.Date <= fechaFin && !x.IsDeleted)
                .ConfigureAwait(false);

            if (schedules.Any())
            {
                await _scheduleService.DeleteRangeAsync(schedules).ConfigureAwait(false);
            }
        }
        catch
        {
        }

        try
        {
            var scheduleGaps = await _scheduleGapService
                .GetAllAsync(x => x.Date >= fechaIni && x.Date <= fechaFin)
                .ConfigureAwait(false);

            if (scheduleGaps.Any())
            {
                await _scheduleGapService.DeleteRangeAsync(scheduleGaps).ConfigureAwait(false);
            }
        }
        catch
        {
        }

        try
        {
            var scheduleSuggestions = await _scheduleSuggestionService
                .GetAllAsync(x => x.WeekStart >= fechaIni && x.WeekStart <= fechaFin)
                .ConfigureAwait(false);

            if (scheduleSuggestions.Any())
            {
                await _scheduleSuggestionService.DeleteRangeAsync(scheduleSuggestions).ConfigureAwait(false);
            }
        }
        catch
        {
        }

        return Ok("Proceso de borrado finalizado.");
    }

    /// <summary>
    /// POST api/Schedule/notify?fechaIni=2025-07-01&fechaFin=2025-07-31
    /// </summary>
    /// <param name="fechaIni"></param>
    /// <returns></returns>
    [HttpPost("notify")]
    public async Task<IActionResult> NotifySchedules(DateOnly fechaIni)
    {
        try
        {
            var userRole = User.GetRoles();
            if (!userRole.Any(x => x == RoleType.Admin.ToString()))
            {
                return Unauthorized("Solo los administradores pueden notificar horarios.");
            }

            var fechaFin = fechaIni.AddDays(6);

            // Obtener los schedules una sola vez
            var schedules = await _scheduleService.GetAllAsync(x =>
                !x.IsDeleted &&
                x.Date >= fechaIni &&
                x.Date <= fechaFin,
                includeProperties: "User,Workstation"
            ).ConfigureAwait(false);

            if (!schedules.Any())
            {
                return BadRequest("No hay horarios programados para reportar");
            }

            var subject = $"Horarios programados del {fechaIni:dd/MM/yyyy} al {fechaFin:dd/MM/yyyy}";

            // Agrupar por usuario y materializar la consulta
            var groupedByUsers = schedules
                .Where(s => !string.IsNullOrWhiteSpace(s.UserEmail))
                .GroupBy(s => s.UserId)
                .ToList();
            var groupedByUser = schedules.GroupBy(s => s.UserId).ToList();

            // OPCIÓN 1: Procesamiento secuencial (más seguro)
            var generatedTokens = await ProcessSchedulesSequentially(groupedByUser, subject, fechaIni);

            foreach (var token in generatedTokens)
            {
                foreach (var schedule in schedules.Where(s => s.UserId == token.UserId))
                {
                    schedule.Token = token.EncryptedToken;
                }
            }

            await _scheduleService.UpdateRangeAsync(schedules);

            return Ok("Horarios notificados correctamente.");
        }
        catch (Exception Ex)
        {
            return BadRequest("Error al notificar horarios: " + Ex.Message);
        }
    }

    private async Task<IEnumerable<ScheduleDto>> AutomaticSchedule(DateOnly fechaIni)
    {
        try
        {
            //var httpClient = _httpClientFactory.CreateClient();

            //var baseUrl = _configuration["PythonApiSettings:BaseUrl"];
            var timeout = _configuration.GetValue<int>("PythonApiSettings:Timeout", 1520);

            //httpClient.Timeout = TimeSpan.FromSeconds(timeout);

            var weekStart = fechaIni.ToString("yyyy-MM-dd");

            var requestBody = new
            {
                path = "/api/agenda/save", // si tu lambda necesita saber la ruta
                httpMethod = "POST",
                body = new { week_start = weekStart, force = false }
            };

            var jsonPayload = JsonSerializer.Serialize(requestBody);

            var config = new AmazonLambdaConfig
            {
                Timeout = TimeSpan.FromSeconds(timeout)
            };
            using var lambdaClient = new AmazonLambdaClient(config);
            var invokeRequest = new InvokeRequest
            {
                FunctionName = _configuration["PythonApiSettings:FunctionName"],
                Payload = jsonPayload
            };

            //var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
            //var content = new StringContent(json, Encoding.UTF8, "application/json");

            //var requestUrl = $"{baseUrl}/api/agenda/save";
            //var response = await httpClient.PostAsync(requestUrl, content);

            var response = await lambdaClient.InvokeAsync(invokeRequest);

            if (response.StatusCode == 200)
            {
                DateOnly fechaFin = fechaIni.AddDays(6);
                return await _scheduleService.GetAllAsync(x => !x.IsDeleted &&
                    x.Date >= fechaIni &&
                    x.Date <= fechaFin, includeProperties: "User,Workstation.WorkArea").ConfigureAwait(false);
            }
            else
            {
                throw new Exception($"Lambda error: {response.StatusCode} - {response.FunctionError}");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error generating automatic schedule: {ex.Message}", ex);
        }
    }

    private async Task<List<UserTokenResult>> ProcessSchedulesSequentially(
        IEnumerable<IGrouping<Guid, ScheduleDto>> groupedByUser,
        string subject, DateOnly fechaIni)
    {
        var tokenResults = new List<UserTokenResult>();

        foreach (var userGroup in groupedByUser)
        {
            var user = userGroup.FirstOrDefault();
            var tokenResult = new UserTokenResult
            {
                UserId = user.UserId,
            };

            var body = HTMLHelper.GenerateScheduleHtml(userGroup.ToList());
            var (encryptedToken, qrCodeBytes) = await _qrCodeService.GenerateWeeklyTokenAsync(user.UserId, fechaIni).ConfigureAwait(false);
            tokenResult.EncryptedToken = encryptedToken;

            await _emailService.SendEmailAsync(
                user.UserEmail,
                subject,
                body,
                qrCodeBytes,
                $"{user.UserNickName}.png",
                "image/png"
            ).ConfigureAwait(false);

            tokenResults.Add(tokenResult);
        }

        return tokenResults;
    }
}