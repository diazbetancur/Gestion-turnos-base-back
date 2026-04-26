using CC.Domain.Entities;
using CC.Infrastructure.Configurations;
using Gandarias.Handlers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

// Environment & Config
Console.WriteLine($"DOTNET_ENVIRONMENT: {Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "(null)"}");
Console.WriteLine($"ASPNETCORE_ENVIRONMENT: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "(null)"}");
Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

var envSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), $"appsettings.{builder.Environment.EnvironmentName}.json");
Console.WriteLine($"Env settings file present: {File.Exists(envSettingsPath)} ({envSettingsPath})");

// Services
builder.Services.AddHealthChecks();
builder.Services.AddControllers().AddJsonOptions(x => x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Swagger
SwaggerHandler.SwaggerConfig(builder.Services);

// Dependency Injection (pass configuration & environment)
DependencyInyectionHandler.DepencyInyectionConfig(builder.Services, builder.Configuration, builder.Environment.EnvironmentName);

Console.WriteLine($"ConnStr PgSQL available: {!string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("PgSQL"))}");

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

// JWT - read from env first, then config
var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
            ?? builder.Configuration["jwtKey"]
            ?? string.Empty;

if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Contains("${"))
{
    throw new InvalidOperationException("JWT secret key not configured properly. Set JWT_SECRET_KEY env var with at least 32 characters.");
}
if (jwtKey.Length < 32)
{
    throw new InvalidOperationException($"JWT secret key too short: {jwtKey.Length} chars. Minimum 32 required.");
}
Console.WriteLine($"JWT key length OK: {jwtKey.Length} chars");

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

// Apply EF Core migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<DBContext>();
        var autoMigrateEnabledConfig = builder.Configuration["MigrationSettings:EnableAutoMigrate"];
        var autoMigrateEnabled = string.IsNullOrWhiteSpace(autoMigrateEnabledConfig)
            || bool.TryParse(autoMigrateEnabledConfig, out var enabled) && enabled;

        if (!autoMigrateEnabled)
        {
            Console.WriteLine("⚠️ Auto migrations disabled by configuration");
        }
        else
        {
            await EnsureMigrationsHistoryTableAsync(dbContext);
            await SeedMigrationBaselineIfNeededAsync(dbContext, builder.Configuration);

            var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();

            if (pendingMigrations.Any())
            {
                Console.WriteLine($"🔄 Applying migrations: {string.Join(", ", pendingMigrations)}");
                await dbContext.Database.MigrateAsync();
                Console.WriteLine("✅ Database migrations applied successfully");
            }
            else
            {
                Console.WriteLine("✅ No pending migrations");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error applying migrations: {ex.Message}");
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

static async Task SeedMigrationBaselineIfNeededAsync(DBContext dbContext, IConfiguration configuration)
{
    var baselineMigrationId = configuration["MigrationSettings:BaselineMigrationId"];
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
        Console.WriteLine($"⚠️ Baseline migration '{baselineMigrationId}' not found in assembly migrations");
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

    Console.WriteLine($"✅ Migration baseline seeded up to: {baselineMigrationId}");
}

// Ejecutar Seeder en background (no bloquea el inicio)
_ = Task.Run(async () =>
{
    await Task.Delay(2000); // Esperar 2 segundos a que la app inicie
    Console.WriteLine("🌱 Running database seeder in background...");
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var seeder = scope.ServiceProvider.GetRequiredService<SeedDB>();
            await seeder.SeedAsync();
            Console.WriteLine("✅ Database seeder completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error running seeder: {ex.Message}");
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

// CORS - Permisivo en Development, restrictivo en Production
string[] allowedOrigins;
if (app.Environment.IsDevelopment())
{
    // Development: Permitir localhost y el frontend de QA
    allowedOrigins = new[]
    {
        "http://localhost:3000",
        "http://localhost:4200",
        "http://localhost:5173",
        "http://gandarias-qa.s3-website.eu-central-1.amazonaws.com",
        "http://gandarias.s3-website.eu-north-1.amazonaws.com"
    };
    Console.WriteLine($"CORS: Development mode - allowing: {string.Join(", ", allowedOrigins)}");
}
else
{
    // Production: Solo dominios autorizados desde configuración
    allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
        ?? new[] { "https://app.restaurantegandarias.com" };
    Console.WriteLine($"CORS: Production mode - restricting to: {string.Join(", ", allowedOrigins)}");
}

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