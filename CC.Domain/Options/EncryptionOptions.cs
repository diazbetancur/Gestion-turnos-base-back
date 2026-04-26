namespace CC.Domain.Options;

public class EncryptionOptions
{
  public const string SectionName = "Encryption";

  public string Key { get; set; } = string.Empty;

  public string IV { get; set; } = string.Empty;
}