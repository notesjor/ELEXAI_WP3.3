using System.Text.Json.Serialization;

namespace IDS.ELEXAI.XmlDomTreeshaker.Model
{
  public class Rule
  {
    [JsonPropertyName("sampling_rate")]
    public byte SamplingRate { get; set; } = 20;

    [JsonPropertyName("xpath")]
    public string Xpath { get; set; } = "//*";
  }
}
