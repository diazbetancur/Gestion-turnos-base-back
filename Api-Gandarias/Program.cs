using CC.Domain.Entities;
using CC.Domain.Options;
using CC.Infrastructure.Configurations;
using Gandarias.Configuration;
using Gandarias.Handlers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;

var dotnetEnvironment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
var aspnetEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
if (string.IsNullOrWhiteSpace(aspnetEnvironment) && !string.IsNullOrWhiteSpace(dotnetEnvironment))
{
    Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", dotnetEnvironment);
}

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
builder.Configuration.AddSecureConfigurationSources(builder.Environment);
builder.Services.AddSecureApplicationConfiguration(builder.Configuration);

// Services
builder.Services.AddHealthChecks();
builder.Services.AddControllers().AddJsonOptions(x => x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Swagger
SwaggerHandler.SwaggerConfig(builder.Services);

// Dependency Injection (pass configuration & environment)
DependencyInyectionHandler.DepencyInyectionConfig(builder.Services, builder.Configuration, builder.Environment.EnvironmentName);

// Identity
builder.Services.AddIdentity<User, Role>(opt =>
{
    opt.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;
    opt.SignIn.RequireConfirmedEmail = false;
    opt.Password.RequiredLength = 8;
    opt.Password.RequireLowercase = true;
    opt.Password.RequireUppercase = true;
    opt.Password.RequireNonAlphanumeric = true;
    opt.Password.RequiredUniqueChars = 1;
    opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
}).AddRoles<Role>().AddEntityFrameworkStores<DBContext>().AddDefaultTokenProviders();

var jwtKey = builder.Configuration.GetRequiredJwtKey();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(x =>
    {
        x.RequireHttpsMetadata = false; // App Runner terminates TLS
        x.SaveToken = true;
        x.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    });

var app = builder.Build();
var startupConfigurationValidator = app.Services.GetRequiredService<StartupConfigurationValidator>();
startupConfigurationValidator.Validate();
var allowedOrigins = startupConfigurationValidator.GetAllowedOrigins();

// Apply EF Core migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<DBContext>();
        var migrationSettings = scope.ServiceProvider.GetRequiredService<IOptions<MigrationSettingsOptions>>().Value;
        var autoMigrateEnabled = migrationSettings.EnableAutoMigrate;

        if (!autoMigrateEnabled)
        {
            Console.WriteLine("Auto migrations are disabled by configuration.");
        }
        else
        {
            await EnsureMigrationsHistoryTableAsync(dbContext);
            await SeedMigrationBaselineIfNeededAsync(dbContext, migrationSettings);

            var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();

            if (pendingMigrations.Any())
            {
                Console.WriteLine($"Applying {pendingMigrations.Count} pending migration(s).");
                await dbContext.Database.MigrateAsync();
                Console.WriteLine("Database migrations applied successfully.");
            }
            else
            {
                Console.WriteLine("No pending migrations.");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error applying migrations: {ex.Message}");
        throw;
    }
}

static async Task EnsureMigrationsHistoryTableAsync(DBContext dbContext)
{
    await dbContext.Database.ExecuteSqlRawAsync(@"
        CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
            ""MigrationId"" character varying(150) NOT NULL,
            ""ProductVersion"" character varying(32) NOT NULL,
            CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY (""MigrationId"")
        );
    ");
}

static async Task SeedMigrationBaselineIfNeededAsync(DBContext dbContext, MigrationSettingsOptions migrationSettings)
{
    var baselineMigrationId = migrationSettings.BaselineMigrationId;
    if (string.IsNullOrWhiteSpace(baselineMigrationId))
        return;

    var appliedMigrations = (await dbContext.Database.GetAppliedMigrationsAsync()).ToList();
    if (appliedMigrations.Any())
        return;

    var schemaExists = await dbContext.Database.SqlQueryRaw<int>(@"
        SELECT COUNT(1) AS ""Value""
        FROM information_schema.tables
        WHERE table_schema = 'Management' AND table_name = 'AspNetRoles'
    ").SingleAsync();

    if (schemaExists == 0)
        return;

    var allMigrations = dbContext.Database.GetMigrations().ToList();
    if (!allMigrations.Contains(baselineMigrationId))
    {
        Console.WriteLine($"Baseline migration '{baselineMigrationId}' was not found in the assembly migrations.");
        return;
    }

    var efCoreVersion = typeof(DbContext).Assembly.GetName().Version;
    var productVersion = efCoreVersion == null
        ? "8.0.0"
        : $"{efCoreVersion.Major}.{efCoreVersion.Minor}.{efCoreVersion.Build}";

    var baselineMigrations = allMigrations.TakeWhile(x => x != baselineMigrationId).Concat(new[] { baselineMigrationId });

    foreach (var migrationId in baselineMigrations)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") SELECT {0}, {1} WHERE NOT EXISTS (SELECT 1 FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = {0})",
            migrationId,
            productVersion);
    }

    Console.WriteLine($"Migration baseline seeded up to '{baselineMigrationId}'.");
}

// Ejecutar Seeder en background (no bloquea el inicio)
_ = Task.Run(async () =>
{
    await Task.Delay(2000); // Esperar 2 segundos a que la app inicie
    Console.WriteLine("Running database seeder in background...");
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var seeder = scope.ServiceProvider.GetRequiredService<SeedDB>();
            await seeder.SeedAsync();
            Console.WriteLine("Database seeder completed successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error running seeder: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
});

// Swagger - IGUAL EN TODOS LOS AMBIENTES
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gandarias API V1");
    c.RoutePrefix = "swagger";
});

app.UseHsts();
app.Use(async (context, next) =>
{
    context.Response.Headers.Remove("X-Powered-By");
    context.Response.Headers.Remove("Server");
    context.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; style-src 'self' 'unsafe-inline'; script-src 'self' 'unsafe-inline'; img-src 'self' data:");
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-Permitted-Cross-Domain-Policies", "master-only");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Cache-Control", "no-cache,no-store,must-revalidate");
    context.Response.Headers.Append("Pragma", "no-cache");
    context.Response.Headers.Append("Referrer-Policy", "no-referrer");
    context.Response.Headers.Append("Permissions-Policy", "fullscreen=(), geolocation=()");

    await next();
});

Console.WriteLine($"CORS configured with {allowedOrigins.Length} origin(s) for environment {app.Environment.EnvironmentName}.");

app.UseCors(x => x
    .WithOrigins(allowedOrigins)
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials());

app.UseMiddleware(typeof(ErrorHandlingMiddleware));
app.UseAuthentication();
app.UseMiddleware<ActivityLoggingMiddleware>();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");
Console.WriteLine($"Listening on http://0.0.0.0:{port}");

app.Run();