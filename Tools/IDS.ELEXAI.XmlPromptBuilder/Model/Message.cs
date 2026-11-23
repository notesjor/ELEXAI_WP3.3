using System.Text.Json.Serialization;

namespace IDS.ELEXAI.XmlPromptBuilder.Model;

public class Message
{
  [JsonConverter(typeof(JsonStringEnumConverter<Role>))]
  [JsonPropertyName("role")]
  public Role Role { get; set; }

  [JsonPropertyName("content")]
  public string Content { get; set; }
}