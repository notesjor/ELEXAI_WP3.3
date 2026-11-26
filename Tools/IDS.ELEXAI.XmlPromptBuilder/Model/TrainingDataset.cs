using System.Text.Json.Serialization;

namespace IDS.ELEXAI.XmlPromptBuilder.Model
{
  public class TrainingDataset
  {
    [JsonPropertyName("messages")]
    public IEnumerable<Message> Messages { get; set; }
  }
}
