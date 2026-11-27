using System.Text.Json.Serialization;

namespace IDS.ELEXAI.Xml2Trainingdata.Model
{
  public class Rule
  {
    [JsonPropertyName("name")]
    public string Name { get; set; } = "my-rule";

    [JsonPropertyName("xpath")]
    public string XpathInstance { get; set; } = "//*";

    [JsonPropertyName("lookup")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Lookup { get; set; }
  }
}
