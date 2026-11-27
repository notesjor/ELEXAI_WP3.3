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
    return Create(doc, null);
  }

  public Message Create(XmlDocument doc, string? sourceFilePath)
  {
    return new Message
    {
      Role = this.Role,
      Content = BuildContent(doc, sourceFilePath)
    };
  }

  private string BuildContent(XmlDocument doc, string? sourceFilePath)
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
          ? EvaluateExpression(doc, sourceFilePath, rule, match)
          : match.Value);
    }

    return content;
  }

  private static string EvaluateExpression(XmlDocument doc, string? sourceFilePath, Rule rule, Match expression)
  {
    var nodes = SelectNodes(doc, sourceFilePath, rule);
    var selector = expression.Groups["selector"].Value;
    var textOnly = expression.Groups["property"].Success;

    if (string.Equals(selector, "all", StringComparison.Ordinal))
      return string.Join("\n", nodes.Select(node => GetNodeValue(node, textOnly)));

    var index = string.IsNullOrEmpty(selector) ? 0 : int.Parse(selector);
    return index < nodes.Count ? GetNodeValue(nodes[index], textOnly) : string.Empty;
  }

  private static List<XmlNode> SelectNodes(XmlDocument doc, string? sourceFilePath, Rule rule)
  {
    if (string.IsNullOrWhiteSpace(rule.Lookup))
      return XmlHelper.SelectAllNodes(doc, rule.XpathInstance);

    if (string.IsNullOrWhiteSpace(sourceFilePath))
      throw new InvalidOperationException($"The template rule '{rule.Name}' needs the source file path for its lookup.");

    var sourcePath = Path.GetFullPath(sourceFilePath);
    var lookupPath = Path.IsPathRooted(rule.Lookup)
      ? Path.GetFullPath(rule.Lookup)
      : Path.GetFullPath(rule.Lookup, Path.GetDirectoryName(sourcePath)!);

    var files = Directory.EnumerateFiles(lookupPath, "*", SearchOption.TopDirectoryOnly)
      .Where(path => string.Equals(Path.GetExtension(path), ".xml", StringComparison.OrdinalIgnoreCase))
      .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
      .ToList();

    var nodes = new List<XmlNode>();
    foreach (var file in files)
    {
      var fullPath = Path.GetFullPath(file);
      if (string.Equals(fullPath, sourcePath, StringComparison.OrdinalIgnoreCase))
        continue;

      var lookupDocument = LoadLookupDocument(fullPath);
      nodes.AddRange(XmlHelper.SelectAllNodes(lookupDocument, rule.XpathInstance));
    }

    return nodes;
  }

  private static XmlDocument LoadLookupDocument(string path)
  {
    var document = new XmlDocument { XmlResolver = null, PreserveWhitespace = true };
    var settings = new XmlReaderSettings
    {
      DtdProcessing = DtdProcessing.Ignore,
      ValidationType = ValidationType.None,
      IgnoreComments = false,
      IgnoreWhitespace = false,
      IgnoreProcessingInstructions = false,
      CheckCharacters = false,
      XmlResolver = null,
      ConformanceLevel = ConformanceLevel.Document
    };

    try
    {
      using var reader = XmlReader.Create(path, settings);
      document.Load(reader);
    }
    catch (XmlException ex)
    {
      throw new InvalidOperationException($"Unable to parse lookup XML file '{path}'.", ex);
    }

    return document;
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