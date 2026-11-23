using System.Xml;
using IDS.ELEXAI.XmlDomTreeshaker.Model;
using IDS.ELEXAI.XmlDomTreeshaker.Processor.Abstract;
using IDS.ELEXAI.XmlToolbox;
using Serilog.Core;

namespace IDS.ELEXAI.XmlDomTreeshaker.Processor
{
  public class ProcessorXmlSingleFile : AbstractProcessor
  {
    public override string Info => "Processor for XML files.";

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

        if (!XmlHelper.LoadXmlDocument(f, logger, out var docOriginal))
        {
          logger.Warning($"Skipping file due to XML parse errors: {fileName}");
          continue;
        }

        foreach (var r in config.Rules)
        {
          GenerateTrainAndEvalDataset(logger, docOriginal, config.Random, r, out var eval, out var train);

          RemoveUnwantedNodes(outputFileEval, train, logger);
          RemoveUnwantedNodes(outputFileTrain, eval, logger);
        }
      }
    }

    private void RemoveUnwantedNodes(string path, List<string> remove, Logger logger)
    {
      if (!XmlHelper.LoadXmlDocument(path, logger, out var document))
      {
        logger.Warning($"Skipping output file due to XML parse errors: {Path.GetFileName(path)}");
        return;
      }

      foreach (var xpath in remove)
      {
        var node = XmlHelper.SelectSingleNode(document, xpath);
        if (node is XmlAttribute attribute)
        {
          attribute.OwnerElement?.Attributes.Remove(attribute);
        }
        else
        {
          node?.ParentNode?.RemoveChild(node);
        }
      }

      XmlHelper.SaveDocument(document, path);
    }

    private void GenerateTrainAndEvalDataset(Logger logger, XmlDocument original, Random random, Rule rule, out List<string> eval, out List<string> train)
    {
      var nodes = XmlHelper.SelectAllNodes(original, rule.Xpath);

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
        eval.Add(XmlHelper.GetXPath(nodes[idx]));
        nodes.RemoveAt(idx);
      }

      train = nodes.Select(XmlHelper.GetXPath).ToList();
    }
  }
}
