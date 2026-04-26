namespace CC.Domain.Options;

public class PythonApiSettingsOptions
{
  public const string SectionName = "PythonApiSettings";

  public string BaseUrl { get; set; } = string.Empty;

  public string FunctionName { get; set; } = string.Empty;

  public int Timeout { get; set; } = 1520;
}