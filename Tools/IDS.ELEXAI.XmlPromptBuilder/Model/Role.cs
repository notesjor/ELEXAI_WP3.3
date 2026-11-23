using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace IDS.ELEXAI.XmlPromptBuilder.Model;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Role
{
  [EnumMember(Value = "system")]
  System,
  [EnumMember(Value = "user")]
  User,
  [EnumMember(Value = "assistant")]
  Assistant
}