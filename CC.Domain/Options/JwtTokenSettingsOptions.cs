namespace CC.Domain.Options;

public class JwtTokenSettingsOptions
{
  public const string SectionName = "JwtTokenSettings";

  public int DefaultHours { get; set; } = 1;

  public int CoordinatorHours { get; set; } = 24;
}