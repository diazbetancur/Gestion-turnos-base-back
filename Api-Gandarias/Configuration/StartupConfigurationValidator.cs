using CC.Domain.Helpers;
using CC.Domain.Options;
using Microsoft.Extensions.Options;

namespace Gandarias.Configuration;

public sealed class StartupConfigurationValidator
{
  private readonly IConfiguration _configuration;
  private readonly IWebHostEnvironment _environment;
  private readonly ILogger<StartupConfigurationValidator> _logger;
  private readonly EmailServiceOptions _emailOptions;
  private readonly GenearlsOptions _genearlsOptions;
  private readonly PythonApiSettingsOptions _pythonApiSettingsOptions;
  private readonly EncryptionOptions _encryptionOptions;
  private readonly AllowedOriginsOptions _allowedOriginsOptions;

  public StartupConfigurationValidator(
      IConfiguration configuration,
      IWebHostEnvironment environment,
      ILogger<StartupConfigurationValidator> logger,
      IOptions<EmailServiceOptions> emailOptions,
      IOptions<GenearlsOptions> genearlsOptions,
      IOptions<PythonApiSettingsOptions> pythonApiSettingsOptions,
      IOptions<EncryptionOptions> encryptionOptions,
      IOptions<AllowedOriginsOptions> allowedOriginsOptions)
  {
    _configuration = configuration;
    _environment = environment;
    _logger = logger;
    _emailOptions = emailOptions.Value;
    _genearlsOptions = genearlsOptions.Value;
    _pythonApiSettingsOptions = pythonApiSettingsOptions.Value;
    _encryptionOptions = encryptionOptions.Value;
    _allowedOriginsOptions = allowedOriginsOptions.Value;
  }

  public void Validate()
  {
    var errors = new List<string>();
    var warnings = new List<string>();

    ValidateConnectionString(errors);
    ValidateJwt(errors);
    ValidateAllowedOrigins(errors, warnings);
    ValidateEmail(warnings, errors);
    ValidateEncryption(errors, warnings);
    ValidateForgotPasswordUrl(errors, warnings);
    ValidatePythonApi(errors, warnings);

    foreach (var warning in warnings)
    {
      _logger.LogWarning("Configuration warning: {Warning}", warning);
    }

    if (errors.Count > 0)
    {
      foreach (var error in errors)
      {
        _logger.LogCritical("Configuration error: {Error}", error);
      }

      throw new InvalidOperationException("Startup configuration validation failed. Review the critical logs for the missing or invalid configuration keys.");
    }

    _logger.LogInformation("Configuration validation completed successfully for environment {Environment}.", _environment.EnvironmentName);
  }

  public string[] GetAllowedOrigins()
  {
    var configuredOrigins = (_allowedOriginsOptions.Origins ?? Array.Empty<string>())
        .Select(ApplicationConfigurationExtensions.NormalizeScalar)
        .Where(ApplicationConfigurationExtensions.IsConfigured)
        .Where(ApplicationConfigurationExtensions.IsValidOrigin)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    if (_environment.IsDevelopment())
    {
      foreach (var developmentOrigin in ApplicationConfigurationExtensions.DevelopmentOrigins)
      {
        if (!configuredOrigins.Contains(developmentOrigin, StringComparer.OrdinalIgnoreCase))
        {
          configuredOrigins.Add(developmentOrigin);
        }
      }
    }

    return configuredOrigins.ToArray();
  }

  private void ValidateConnectionString(List<string> errors)
  {
    var connectionString = _configuration.GetConnectionString("PgSQL");
    if (!ApplicationConfigurationExtensions.IsConfigured(connectionString))
    {
      errors.Add("ConnectionStrings:PgSQL is missing or empty.");
      return;
    }

    _logger.LogInformation("Configuration status: ConnectionStrings:PgSQL is configured.");
  }

  private void ValidateJwt(List<string> errors)
  {
    var jwtKey = ApplicationConfigurationExtensions.NormalizeScalar(_configuration["jwtKey"]);
    if (!ApplicationConfigurationExtensions.IsConfigured(jwtKey))
    {
      errors.Add("jwtKey is missing or empty.");
      return;
    }

    if (jwtKey.Length < 32)
    {
      errors.Add("jwtKey must contain at least 32 characters.");
      return;
    }

    _logger.LogInformation("Configuration status: jwtKey is configured.");
  }

  private void ValidateAllowedOrigins(List<string> errors, List<string> warnings)
  {
    var allowedOrigins = GetAllowedOrigins();
    if (_environment.IsDevelopment())
    {
      if (allowedOrigins.Length == 0)
      {
        warnings.Add("AllowedOrigins is empty. Only the built-in localhost origins are expected in Development.");
      }
      else
      {
        _logger.LogInformation("Configuration status: CORS is configured with {OriginCount} allowed origin(s) for Development.", allowedOrigins.Length);
      }

      return;
    }

    if (allowedOrigins.Length == 0)
    {
      errors.Add("AllowedOrigins must contain at least one valid origin outside Development.");
      return;
    }

    _logger.LogInformation("Configuration status: AllowedOrigins contains {OriginCount} valid origin(s).", allowedOrigins.Length);
  }

  private void ValidateEmail(List<string> warnings, List<string> errors)
  {
    var smtpServerConfigured = ApplicationConfigurationExtensions.IsConfigured(_emailOptions.SmtpServer);
    var smtpUserConfigured = ApplicationConfigurationExtensions.IsConfigured(_emailOptions.SmtpUser);
    var smtpPasswordConfigured = ApplicationConfigurationExtensions.IsConfigured(_emailOptions.SmtpPassword);

    var configuredFields = new[] { smtpServerConfigured, smtpUserConfigured, smtpPasswordConfigured }.Count(x => x);
    if (configuredFields == 0)
    {
      warnings.Add("EmailService is not configured. Email-dependent features will fail until SMTP settings are provided.");
      return;
    }

    if (configuredFields is > 0 and < 3)
    {
      if (_environment.IsDevelopment())
      {
        warnings.Add("EmailService is only partially configured. Provide smtpServer, smtpUser and smtpPassword together.");
      }
      else
      {
        errors.Add("EmailService is partially configured. Provide smtpServer, smtpUser and smtpPassword together.");
      }

      return;
    }

    if (_emailOptions.SmtpPort <= 0)
    {
      if (_environment.IsDevelopment())
      {
        warnings.Add("EmailService:smtpPort is invalid. Use a positive integer value.");
      }
      else
      {
        errors.Add("EmailService:smtpPort is invalid. Use a positive integer value.");
      }

      return;
    }

    _logger.LogInformation("Configuration status: EmailService is configured.");
  }

  private void ValidateEncryption(List<string> errors, List<string> warnings)
  {
    var keyConfigured = ApplicationConfigurationExtensions.IsConfigured(_encryptionOptions.Key);
    var ivConfigured = ApplicationConfigurationExtensions.IsConfigured(_encryptionOptions.IV);

    if (!keyConfigured || !ivConfigured)
    {
      if (_environment.IsDevelopment())
      {
        warnings.Add("Encryption settings are incomplete. QR/token encryption features will fail until Encryption:Key and Encryption:IV are configured.");
      }
      else
      {
        errors.Add("Encryption:Key and Encryption:IV are required outside Development.");
      }

      return;
    }

    if (!IsBase64(_encryptionOptions.Key) || !IsBase64(_encryptionOptions.IV))
    {
      if (_environment.IsDevelopment())
      {
        warnings.Add("Encryption settings are present but not valid Base64 values.");
      }
      else
      {
        errors.Add("Encryption:Key and Encryption:IV must be valid Base64 values.");
      }

      return;
    }

    _logger.LogInformation("Configuration status: Encryption settings are configured.");
  }

  private void ValidateForgotPasswordUrl(List<string> errors, List<string> warnings)
  {
    var forgotUrl = ApplicationConfigurationExtensions.NormalizeScalar(_genearlsOptions.UrlForgot);
    if (!ApplicationConfigurationExtensions.IsConfigured(forgotUrl))
    {
      if (_environment.IsDevelopment())
      {
        warnings.Add("Genearls:UrlForgot is missing. Password recovery links will not be generated correctly.");
      }
      else
      {
        errors.Add("Genearls:UrlForgot is required outside Development for password recovery.");
      }

      return;
    }

    if (!ApplicationConfigurationExtensions.IsValidOrigin(forgotUrl))
    {
      if (_environment.IsDevelopment())
      {
        warnings.Add("Genearls:UrlForgot is configured but is not a valid absolute HTTP/HTTPS URL.");
      }
      else
      {
        errors.Add("Genearls:UrlForgot must be a valid absolute HTTP/HTTPS URL.");
      }

      return;
    }

    _logger.LogInformation("Configuration status: Genearls:UrlForgot is configured.");
  }

  private void ValidatePythonApi(List<string> errors, List<string> warnings)
  {
    var baseUrlConfigured = ApplicationConfigurationExtensions.IsConfigured(_pythonApiSettingsOptions.BaseUrl);
    var functionNameConfigured = ApplicationConfigurationExtensions.IsConfigured(_pythonApiSettingsOptions.FunctionName);

    if (!baseUrlConfigured && !functionNameConfigured)
    {
      warnings.Add("PythonApiSettings is not configured. Automatic scheduling features that depend on the Python/Lambda integration will fail until configured.");
      return;
    }

    if (baseUrlConfigured && !ApplicationConfigurationExtensions.IsValidOrigin(_pythonApiSettingsOptions.BaseUrl))
    {
      if (_environment.IsDevelopment())
      {
        warnings.Add("PythonApiSettings:BaseUrl is configured but is not a valid absolute HTTP/HTTPS URL.");
      }
      else
      {
        errors.Add("PythonApiSettings:BaseUrl must be a valid absolute HTTP/HTTPS URL when provided.");
      }

      return;
    }

    if (_pythonApiSettingsOptions.Timeout <= 0)
    {
      if (_environment.IsDevelopment())
      {
        warnings.Add("PythonApiSettings:Timeout is invalid. Use a positive number of seconds.");
      }
      else
      {
        errors.Add("PythonApiSettings:Timeout is invalid. Use a positive number of seconds.");
      }

      return;
    }

    if (!functionNameConfigured)
    {
      warnings.Add("PythonApiSettings:FunctionName is missing. The current Lambda integration will not work until it is configured.");
      return;
    }

    _logger.LogInformation("Configuration status: PythonApiSettings is configured.");
  }

  private static bool IsBase64(string value)
  {
    try
    {
      Convert.FromBase64String(value);
      return true;
    }
    catch (FormatException)
    {
      return false;
    }
  }
}