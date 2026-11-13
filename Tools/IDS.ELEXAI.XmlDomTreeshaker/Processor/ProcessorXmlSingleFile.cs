using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;
using IDS.ELEXAI.XmlDomTreeshaker.Model;
using IDS.ELEXAI.XmlDomTreeshaker.Processor.Abstract;
using Serilog.Core;

namespace IDS.ELEXAI.XmlDomTreeshaker.Processor
{
  public class ProcessorXmlSingleFile : AbstractProcessor
  {
    public override string Info => "Processor for single XML files.";

    public override void Execute(Logger logger, Configuration config, string input, string outputEval, string outputTrain)
    {
      foreach (var f in Directory.GetFiles(input, "*.*", SearchOption.TopDirectoryOnly))
      {
        var fileName = Path.GetFileName(f);
        logger.Information($"Processing file: {fileName}");

        var outputFileEval = Path.Combine(outputEval, fileName);
        var outputFileTrain = Path.Combine(outputTrain, fileName);

        File.Copy(f, outputFileEval, true);
        File.Copy(f, outputFileTrain, true);

        if (!TryLoadXmlDocument(f, logger, out var docOriginal))
        {
          logger.Warning($"Skipping file due to XML parse errors: {fileName}");
          continue;
        }

        foreach (var r in config.Rules)
        {
          SamplingProcess(logger, docOriginal, config.Random, r, out var eval, out var train);
          PrepareDocument(outputFileEval, train, logger);
          PrepareDocument(outputFileTrain, eval, logger);
        }
      }
    }

    private void PrepareDocument(string path, List<string> remove, Logger logger)
    {
      if (!TryLoadXmlDocument(path, logger, out var document))
      {
        logger.Warning($"Skipping output file due to XML parse errors: {Path.GetFileName(path)}");
        return;
      }

      foreach (var xpath in remove)
      {
        var node = SelectSingleNodeLenient(document, xpath);
        if (node is XmlAttribute attribute)
        {
          attribute.OwnerElement?.Attributes.Remove(attribute);
        }
        else
        {
          node?.ParentNode?.RemoveChild(node);
        }
      }

      SaveDocument(document, path);
    }

    private bool TryLoadXmlDocument(string path, Logger logger, out XmlDocument document)
    {
      var xml = File.ReadAllText(path, Encoding.UTF8);

      if (TryParseXml(xml, out document))
        return true;

      var normalized = NormalizeMalformedXml(xml);
      if (TryParseXml(normalized, out document))
      {
        logger.Warning($"Loaded malformed XML with fallback normalization: {Path.GetFileName(path)}");
        return true;
      }

      logger.Warning($"Unable to parse XML even after fallback normalization: {Path.GetFileName(path)}");
      return false;
    }

    private bool TryParseXml(string xml, out XmlDocument document)
    {
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
        return true;
      }
      catch (XmlException)
      {
        return false;
      }
    }

    private string NormalizeMalformedXml(string xml)
    {
      var filtered = new string(xml.Where(XmlConvert.IsXmlChar).ToArray());
      filtered = Regex.Replace(filtered, "\\sxmlns(:[\\w\\-\\.]+)?\\s*=\\s*\"[^\"]*\"", string.Empty, RegexOptions.IgnoreCase);
      filtered = Regex.Replace(filtered, "\\sxmlns(:[\\w\\-\\.]+)?\\s*=\\s*'[^']*'", string.Empty, RegexOptions.IgnoreCase);
      filtered = Regex.Replace(filtered, "(<\\/?)([A-Za-z_][\\w\\-\\.]*:)", "$1", RegexOptions.IgnoreCase);
      filtered = Regex.Replace(filtered, "(\\s)([A-Za-z_][\\w\\-\\.]*:)([A-Za-z_][\\w\\-\\.]*\\s*=)", "$1$3", RegexOptions.IgnoreCase);
      return filtered;
    }

    private XmlNode? SelectSingleNodeLenient(XmlDocument document, string xpath)
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

    private List<XmlNode> SelectNodesLenient(XmlDocument document, string xpath)
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

    private string StripXPathPrefixes(string xpath)
    {
      var withoutNodePrefixes = Regex.Replace(xpath, @"(?<=/|::)([A-Za-z_][\w\-.]*):(?=[A-Za-z_][\w\-.]*|\*)", string.Empty);
      return Regex.Replace(withoutNodePrefixes, @"(?<=@)([A-Za-z_][\w\-.]*):(?=[A-Za-z_][\w\-.]*)", string.Empty);
    }

    private void SamplingProcess(Logger logger, XmlDocument original, Random random, Rule rule, out List<string> eval, out List<string> train)
    {
      var nodes = SelectNodesLenient(original, rule.Xpath);

      if (nodes.Count == 0)
      {
        logger.Warning($"Rule matched no nodes/attributes: {rule.Xpath}");
        eval = [];
        train = [];
        return;
      }

      var ratio = (((double)nodes.Count) / 100.0) * (double)rule.SamplingRate;
      var evalCount = Math.Clamp((int)Math.Ceiling(ratio), 0, nodes.Count);

      eval = new List<string>(evalCount);

      for (var i = 0; i < evalCount; i++)
      {
        var idx = random.Next(0, nodes.Count);
        eval.Add(GetXPath(nodes[idx]));
        nodes.RemoveAt(idx);
      }

      train = nodes.Select(GetXPath).ToList();
    }

    private string GetXPath(XmlNode node)
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

    private void SaveDocument(XmlDocument document, string path)
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
