using System.Text.Json.Serialization;

namespace IDS.ELEXAI.Xml2Trainingdata.Model
{
  public class Template
  {
    [JsonPropertyName("messages")]
    public IEnumerable<MessageExtended> Messages { get; set; }
  }
}
