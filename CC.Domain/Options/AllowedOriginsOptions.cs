namespace CC.Domain.Options;

public class AllowedOriginsOptions
{
  public string[] Origins { get; set; } = Array.Empty<string>();
}