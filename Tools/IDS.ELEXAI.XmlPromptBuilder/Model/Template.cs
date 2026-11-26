using System.Text.Json.Serialization;

namespace IDS.ELEXAI.XmlPromptBuilder.Model
{
  public class Template
  {
    [JsonPropertyName("messages")]
    public IEnumerable<MessageExtended> Messages { get; set; }
  }
}
