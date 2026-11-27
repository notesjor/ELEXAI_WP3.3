using System.Text.Json.Serialization;

namespace IDS.ELEXAI.Xml2Trainingdata.Model
{
  public class TrainingDataset
  {
    [JsonPropertyName("messages")]
    public IEnumerable<Message> Messages { get; set; }
  }
}
