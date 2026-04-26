using CC.Domain.Helpers;
using CC.Domain.Options;

namespace Gandarias.Configuration;

public static class ApplicationConfigurationExtensions
{
  public static readonly string[] DevelopmentOrigins =
  [
      "http://localhost:3000",
        "http://localhost:4200",
        "http://localhost:5173"
  ];

  public static void AddSecureConfigurationSources(this ConfigurationManager configuration, IWebHostEnvironment environment)
  {
    configuration.Sources.Clear();

    configuration
        .SetBasePath(environment.ContentRootPath)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

    var lowerCaseEnvironmentName = environment.EnvironmentName.ToLowerInvariant();
    if (!string.Equals(environment.EnvironmentName, lowerCaseEnvironmentName, StringComparison.Ordinal))
    {
      configuration.AddJsonFile($"appsettings.{lowerCaseEnvironmentName}.json", optional: true, reloadOnChange: true);
    }

    if (environment.IsDevelopment())
    {
      configuration.AddUserSecrets(typeof(ApplicationConfigurationExtensions).Assembly, optional: true, reloadOnChange: false);
    }

    configuration.AddEnvironmentVariables();
  }

  public static IServiceCollection AddSecureApplicationConfiguration(this IServiceCollection services, IConfiguration configuration)
  {
    services.AddOptions<JwtSecurityOptions>()
        .Configure(options => options.Key = NormalizeScalar(configuration["jwtKey"]));

    services.AddOptions<JwtTokenSettingsOptions>()
        .Bind(configuration.GetSection(JwtTokenSettingsOptions.SectionName));

    services.AddOptions<EmailServiceOptions>()
        .Bind(configuration.GetSection("EmailService"));

    services.AddOptions<GenearlsOptions>()
        .Bind(configuration.GetSection(GenearlsOptions.SectionName));

    services.AddOptions<PythonApiSettingsOptions>()
        .Bind(configuration.GetSection(PythonApiSettingsOptions.SectionName));

    services.AddOptions<EncryptionOptions>()
        .Bind(configuration.GetSection(EncryptionOptions.SectionName));

    services.AddOptions<MigrationSettingsOptions>()
        .Bind(configuration.GetSection(MigrationSettingsOptions.SectionName));

    services.AddOptions<AllowedOriginsOptions>()
        .Configure(options =>
        {
          options.Origins = configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        });

    services.AddSingleton<StartupConfigurationValidator>();

    return services;
  }

  public static string GetRequiredJwtKey(this IConfiguration configuration)
  {
    var jwtKey = NormalizeScalar(configuration["jwtKey"]);

    if (!IsConfigured(jwtKey))
    {
      throw new InvalidOperationException("JWT signing key is not configured. Configure 'jwtKey' with a strong secret via user-secrets or environment variables.");
    }

    if (jwtKey.Length < 32)
    {
      throw new InvalidOperationException("JWT signing key is too short. Configure 'jwtKey' with at least 32 characters.");
    }

    return jwtKey;
  }

  public static bool IsConfigured(string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return false;
    }

    return !value.Contains("${", StringComparison.Ordinal) &&
           !value.Contains("__REPLACE_ME__", StringComparison.OrdinalIgnoreCase);
  }

  public static string NormalizeScalar(string? value)
  {
    return value?.Trim() ?? string.Empty;
  }

  public static bool IsValidOrigin(string value)
  {
    if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
    {
      return false;
    }

    return string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
           string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
  }
}