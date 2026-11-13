using System.Text.Json.Serialization;

namespace IDS.ELEXAI.XmlDomTreeshaker.Model
{
  public class Configuration
  {
    private Random _random = null;

    [JsonIgnore]
    public Random Random => _random ??= new Random((int)(Seed == "RANDOM" ? DateTime.Now.Ticks : long.Parse(Seed)));

    [JsonPropertyName("seed")]
    public string Seed { get; set; } = "1337021632";

    [JsonPropertyName("rules")]
    public Rule[] Rules { get; set; } = [];
  }
}
