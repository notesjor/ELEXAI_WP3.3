using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IDS.ELEXAI.XmlPromptBuilder.Model
{
  public class TrainingDataset
  {
    [JsonPropertyName("messages")]
    public IEnumerable<Message> Messages { get; set; }
  }
}
