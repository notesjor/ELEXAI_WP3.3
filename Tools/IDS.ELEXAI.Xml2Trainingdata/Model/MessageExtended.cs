using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml;
using IDS.ELEXAI.XmlToolbox;

namespace IDS.ELEXAI.Xml2Trainingdata.Model;

public class MessageExtended : Message
{
  private static readonly Regex TemplateExpression = new(
    @"\{(?<name>[A-Za-z_][\w.-]*)(?:\[(?<selector>all|\d+)\])?(?:\.(?<property>text))?\}",
    RegexOptions.Compiled | RegexOptions.CultureInvariant);

  [JsonPropertyName("rules")]
  public IEnumerable<Rule> Rules { get; set; } = [];

  public Message Create(XmlDocument doc)
  {
    return new Message
    {
      Role = this.Role,
      Content = BuildContent(doc)
    };
  }

  private string BuildContent(XmlDocument doc)
  {
    ArgumentNullException.ThrowIfNull(doc);

    var content = Content ?? string.Empty;
    if (!content.Contains('{'))
      return content;

    foreach (var rule in Rules ?? [])
    {
      if (string.IsNullOrWhiteSpace(rule.Name))
        throw new InvalidOperationException("A template rule must have a name.");
      if (string.IsNullOrWhiteSpace(rule.XpathInstance))
        throw new InvalidOperationException($"The template rule '{rule.Name}' must have an XPath expression.");

      content = TemplateExpression.Replace(content, match =>
        string.Equals(match.Groups["name"].Value, rule.Name, StringComparison.Ordinal)
          ? EvaluateExpression(doc, rule, match)
          : match.Value);
    }

    return content;
  }

  private static string EvaluateExpression(XmlDocument doc, Rule rule, Match expression)
  {
    var nodes = XmlHelper.SelectAllNodes(doc, rule.XpathInstance);
    var selector = expression.Groups["selector"].Value;
    var textOnly = expression.Groups["property"].Success;

    if (string.Equals(selector, "all", StringComparison.Ordinal))
      return string.Concat(nodes.Select(node => GetNodeValue(node, textOnly)));

    var index = string.IsNullOrEmpty(selector) ? 0 : int.Parse(selector);
    return index < nodes.Count ? GetNodeValue(nodes[index], textOnly) : string.Empty;
  }

  private static string GetNodeValue(XmlNode node, bool textOnly)
  {
    if (node is XmlAttribute attribute)
      return attribute.Value;

    return textOnly
      ? node.InnerText
      : node switch
      {
        XmlElement element => element.InnerXml,
        _ => node.Value ?? node.OuterXml
      };
  }
}