using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IDS.ELEXAI.XmlPromptBuilder.Model
{
  public class Rule
  {
    [JsonPropertyName("xpath")]
    public string XpathInstance { get; set; } = "//*";
  }
}
