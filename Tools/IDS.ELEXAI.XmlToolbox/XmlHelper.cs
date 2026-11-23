using Serilog.Core;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;

namespace IDS.ELEXAI.XmlToolbox
{
  public class XmlHelper
  {
    public static bool LoadXmlDocument(string path, Logger logger, out XmlDocument document)
    {
      var xml = File.ReadAllText(path, Encoding.UTF8);

      bool ret;
      document = new XmlDocument { XmlResolver = null, PreserveWhitespace = true };

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
        using var sr = new StringReader(xml);
        using var xr = XmlReader.Create(sr, settings);
        document.Load(xr);
        ret = true;
      }
      catch (XmlException)
      {
        ret = false;
      }

      if (ret)
        return true;

      logger.Warning($"Unable to parse XML even after fallback normalization: {Path.GetFileName(path)}");
      return false;
    }

    public static XmlNode? SelectSingleNode(XmlDocument document, string xpath)
    {
      try
      {
        return document.SelectSingleNode(xpath);
      }
      catch (XPathException)
      {
        var stripped = StripXPathPrefixes(xpath);
        return document.SelectSingleNode(stripped);
      }
    }

    public static List<XmlNode> SelectAllNodes(XmlNode node, string xpath)
    {
      try
      {
        return node.SelectNodes(xpath)?.Cast<XmlNode>().ToList() ?? [];
      }
      catch (XPathException)
      {
        var stripped = StripXPathPrefixes(xpath);
        return node.SelectNodes(stripped)?.Cast<XmlNode>().ToList() ?? [];
      }
    }

    public static List<XmlNode> SelectAllNodes(XmlDocument document, string xpath)
    {
      try
      {
        return document.SelectNodes(xpath)?.Cast<XmlNode>().ToList() ?? [];
      }
      catch (XPathException)
      {
        var stripped = StripXPathPrefixes(xpath);
        return document.SelectNodes(stripped)?.Cast<XmlNode>().ToList() ?? [];
      }
    }

    private static string StripXPathPrefixes(string xpath)
    {
      var withoutNodePrefixes = Regex.Replace(xpath, @"(?<=/|::)([A-Za-z_][\w\-.]*):(?=[A-Za-z_][\w\-.]*|\*)", string.Empty);
      return Regex.Replace(withoutNodePrefixes, @"(?<=@)([A-Za-z_][\w\-.]*):(?=[A-Za-z_][\w\-.]*)", string.Empty);
    }

    public static string GetXPath(XmlNode node)
    {
      if (node is XmlAttribute attribute)
      {
        return $"{GetXPath(attribute.OwnerElement!)}/@*[local-name()='{attribute.LocalName}']";
      }

      var segments = new Stack<string>();
      var current = node;

      while (current is { NodeType: XmlNodeType.Element })
      {
        var siblings = current.ParentNode?.ChildNodes
          .Cast<XmlNode>()
          .Where(x => x.NodeType == XmlNodeType.Element && x.LocalName == current.LocalName)
          .ToList() ?? [];

        var index = siblings.Count > 1 ? siblings.IndexOf(current) + 1 : 1;
        segments.Push($"*[local-name()='{current.LocalName}'][{index}]");
        current = current.ParentNode!;
      }

      return "/" + string.Join("/", segments);
    }

    public static void SaveDocument(XmlDocument document, string path)
    {
      var settings = new XmlWriterSettings
      {
        Encoding = Encoding.UTF8,
        OmitXmlDeclaration = false,
        Indent = false,
        CheckCharacters = false
      };

      using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
      using var writer = XmlWriter.Create(fs, settings);
      document.Save(writer);
    }
  }
}
