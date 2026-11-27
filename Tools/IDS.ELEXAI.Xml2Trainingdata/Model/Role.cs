using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace IDS.ELEXAI.Xml2Trainingdata.Model;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Role
{
  system,
  user,
  assistant
}