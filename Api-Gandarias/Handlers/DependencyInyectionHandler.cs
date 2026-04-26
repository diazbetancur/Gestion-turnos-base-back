using CC.Application.Services;
using CC.Domain.Helpers;
using CC.Domain.Interfaces.Repositories;
using CC.Domain.Interfaces.Services;
using CC.Domain.Options;
using CC.Infrastructure.Configurations;
using CC.Infrastructure.EmailServices;
using CC.Infrastructure.Interceptors;
using CC.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using System.Reflection;
using ILogger = Serilog.ILogger;

namespace Gandarias.Handlers;

public class DependencyInyectionHandler
{
    public static void DepencyInyectionConfig(IServiceCollection services, IConfiguration configuration, string environment)
    {
        try
        {
            Console.WriteLine($"Environment detected in DI: {environment}");

            // Registrar HttpContextAccessor (requerido para auditoría)
            services.AddHttpContextAccessor();

            #region Database Configuration

            var connectionString = configuration.GetConnectionString("PgSQL");

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Database connection string not found");

            // Configurar DbContext con interceptor de auditoría
            services.AddDbContext<DBContext>((serviceProvider, opt) =>
            {
                opt.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null);
                    npgsqlOptions.CommandTimeout(300);
                    npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                });

                var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
                opt.AddInterceptors(new AuditSaveChangesInterceptor(httpContextAccessor));

                // Configuración IGUAL para todos los ambientes
                opt.EnableSensitiveDataLogging(false);
                opt.EnableDetailedErrors(false);
                opt.UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);
            });

            Console.WriteLine("✅ PostgreSQL DbContext configured with Audit Interceptor");

            #endregion Database Configuration

            #region AutoMapper

            services.AddAutoMapper(Assembly.Load("CC.Domain"));

            #endregion AutoMapper

            #region Services and Repositories

            ServicesRegistration(services);
            RepositoryRegistration(services);

            #endregion Services and Repositories

            #region EmailService

            services.PostConfigure<EmailServiceOptions>(options =>
            {
                options.SmtpPort = options.SmtpPort <= 0 ? 587 : options.SmtpPort;
            });

            services.PostConfigure<MigrationSettingsOptions>(options =>
            {
                options.BaselineMigrationId = options.BaselineMigrationId?.Trim() ?? string.Empty;
            });

            #endregion EmailService

            services.AddSingleton<ExceptionControl>();

            #region Logging

            Logger logger = new LoggerConfiguration()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File("logs/log-.txt",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                    retainedFileCountLimit: 7)
                .CreateLogger();

            logger.Information("DI configured for {Environment}", environment);
            services.AddSingleton<ILogger>(logger);

            #endregion Logging

            services.AddTransient<SeedDB>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DI failure: {ex.Message}");
            throw new InvalidOperationException("Dependency injection configuration failed", ex);
        }
    }

    public static void ServicesRegistration(IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IWorkAreaService, WorkAreaService>();
        services.AddScoped<IWorkstationService, WorkstationService>();
        services.AddScoped<IRolePermissionService, RolePermissionService>();
        services.AddScoped<IHireTypeService, HireTypeService>();
        services.AddScoped<IShiftTypeService, ShiftTypeService>();
        services.AddScoped<ILicenseService, LicenseService>();
        services.AddScoped<IHybridWorkstationService, HybridWorkstationService>();
        services.AddScoped<IUserWorkstationService, UserWorkstationService>();
        services.AddScoped<IEmployeeScheduleRestrictionService, EmployeeScheduleRestrictionService>();
        services.AddScoped<IAbsenteeismTypeService, AbsenteeismTypeService>();
        services.AddScoped<IUserAbsenteeismService, UserAbsenteeismService>();
        services.AddScoped<IEmployeeScheduleExceptionService, EmployeeScheduleExceptionService>();
        services.AddScoped<ILawRestrictionService, LawRestrictionService>();
        services.AddScoped<IWorkstationDemandService, WorkstationDemandService>();
        services.AddScoped<IWorkstationDemandTemplateService, WorkstationDemandTemplateService>();
        services.AddScoped<IEmployeeShiftTypeRestrictionService, EmployeeShiftTypeRestrictionService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IUserShiftService, UserShiftService>();
        services.AddScoped<IScheduleService, ScheduleService>();
        services.AddTransient<IQrCodeService, QrCodeRepository>();
        services.AddTransient<IEncryptionService, AesEncryptionService>();
        services.AddScoped<ISigningService, SigningService>();
        services.AddScoped<IScheduleGapService, ScheduleGapService>();
        services.AddScoped<IScheduleSuggestionService, ScheduleSuggestionService>();
        services.AddScoped<IScheduleQualityShiftScoreService, ScheduleQualityShiftScoreService>();
        services.AddScoped<IIgnoredRestrictionService, IgnoredRestrictionService>();

        services.AddHttpClient();
    }

    public static void RepositoryRegistration(IServiceCollection services)
    {
        services.AddScoped<IQueryableUnitOfWork, DBContext>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IWorkAreaRepository, WorkAreaRepository>();
        services.AddScoped<IWorkstationRepository, WorkstationRepository>();
        services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
        services.AddScoped<IHireTypeRepository, HireTypeRepository>();
        services.AddScoped<IShiftTypeRepository, ShiftTypeRepository>();
        services.AddScoped<ILicenseRepository, LicenseRepository>();
        services.AddScoped<IHybridWorkstationRepository, HybridWorkstationRepository>();
        services.AddScoped<IUserWorkstationRepository, UserWorkstationRepository>();
        services.AddScoped<IEmployeeScheduleRestrictionRepository, EmployeeScheduleRestrictionRepository>();
        services.AddScoped<IAbsenteeismTypeRepository, AbsenteeismTypeRepository>();
        services.AddScoped<IUserAbsenteeismRepository, UserAbsenteeismRepository>();
        services.AddScoped<IEmployeeScheduleExceptionRepository, EmployeeScheduleExceptionRepository>();
        services.AddScoped<ILawRestrictionRepository, LawRestrictionRepository>();
        services.AddScoped<IWorkstationDemandRepository, WorkstationDemandRepository>();
        services.AddScoped<IWorkstationDemandTemplateRepository, WorkstationDemandTemplateRepository>();
        services.AddScoped<IEmployeeShiftRestrictionRepository, EmployeeShiftRestrictionRepository>();
        services.AddScoped<IUserShiftRepository, UserShiftRepository>();
        services.AddScoped<IScheduleRepository, ScheduleRepository>();
        services.AddScoped<ISigningRepository, SigningRepository>();
        services.AddScoped<IScheduleGapRepository, ScheduleGapRepository>();
        services.AddScoped<IScheduleSuggestionRepository, ScheduleSuggestionRepository>();
        services.AddScoped<IScheduleQualityShiftScoreRepository, ScheduleQualityShiftScoreRepository>();
        services.AddScoped<IIgnoredRestrictionRepository, IgnoredRestrictionRepository>();
    }
}