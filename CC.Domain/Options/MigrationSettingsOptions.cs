namespace CC.Domain.Options;

public class MigrationSettingsOptions
{
  public const string SectionName = "MigrationSettings";

  public bool EnableAutoMigrate { get; set; }

  public string BaselineMigrationId { get; set; } = string.Empty;
}