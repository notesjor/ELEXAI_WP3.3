using System.Text.Json.Serialization;

namespace IDS.ELEXAI.XmlPromptBuilder.Model
{
  public class Rule
  {
    [JsonPropertyName("name")]
    public string Name { get; set; } = "my-rule";

    [JsonPropertyName("xpath")]
    public string XpathInstance { get; set; } = "//*";
  }
}
