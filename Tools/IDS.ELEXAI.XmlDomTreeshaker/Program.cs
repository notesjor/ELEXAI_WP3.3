using IDS.ELEXAI.XmlDomTreeshaker.Model;
using IDS.ELEXAI.XmlDomTreeshaker.Processor;
using IDS.ELEXAI.XmlDomTreeshaker.Processor.Abstract;
using Serilog;
using Serilog.Core;

namespace IDS.ELEXAI.XmlDomTreeshaker
{
  internal class Program
  {
    private static Logger _logger;
    private static string _outputEval;
    private static string _outputTrain;
    private static AbstractProcessor _selectedProcessor;
    private static Configuration? _config;
    private static Dictionary<string, AbstractProcessor> _processors =
      new Dictionary<string, AbstractProcessor>
      {
        { "xml", new ProcessorXmlSingleFile() }
      };

    static void Main(string[] args)
    {
      if(args.Length == 1)
      {
        SingleArgumentHandling(args[0]);
        return;
      }
      if(args.Length != 4)
      {
        PrintHelp();
        return;
      }

      var configJsonPath = args[0];
      var processor = args[1];
      var input = args[2];
      var output = args[3];

      if (!ValidateComfig(configJsonPath))
        return;
      if (!ValidInput(input))
        return;
      if (!ValidateProcessor(processor))
        return;
      ValidateOutput(output);

      _selectedProcessor.Execute(_logger, _config, input, _outputEval, _outputTrain);
    }

    private static void PrintHelp()
    {
      Console.WriteLine("Usage: shake <configJsonPath> <processor> <input> <output>");
      Console.WriteLine("Where:");
      Console.WriteLine("<configJsonPath> - path to the configuration JSON file");
      Console.WriteLine("<processor> - the processor to use (see 'shake list' for available processors)");
      Console.WriteLine("<input> - path to the input file or directory");
      Console.WriteLine("<output> - path to the output directory (create log.txt and eval/train subdirectories)");      
      Console.WriteLine("Or: shake init - to create an empty configuration file (see examples in repo)");
      Console.WriteLine("Or: shake list - to list all supported file processors");
      Console.WriteLine("Or: shake help - to display this help information");
    }

    private static void SingleArgumentHandling(string arg)
    {
      switch (arg)
      {
        case "list":
          PrintProcessors();
          break;
        case "init":
          CreateEmptyConfiguration();
          break;
        default:
          PrintHelp();
          break;
      }
    }

    private static void CreateEmptyConfiguration()
    {
      using var fs = new FileStream("template.json", FileMode.Create, FileAccess.Write);
      System.Text.Json.JsonSerializer.Serialize(fs, new Configuration());
    }

    private static void PrintProcessors()
    {
      Console.WriteLine("Available processors:");
      foreach(var p in _processors)
        Console.WriteLine($"{p.Key}\t-\t{p.Value.Info}");
    }

    private static bool ValidateProcessor(string processor)
    {
      if (_processors.TryGetValue(processor, out var res))
      {
        _selectedProcessor = res;
        return true;
      }

      Console.WriteLine($"{processor} not found.");
      PrintProcessors();
      return false;
    }

    private static void ValidateOutput(string output)
    {
      if (!Directory.Exists(output))
        Directory.CreateDirectory(output);
      _outputEval = Path.Combine(output, "eval");
      if (!Directory.Exists(_outputEval))
        Directory.CreateDirectory(_outputEval);
      _outputTrain = Path.Combine(output, "train");
      if (!Directory.Exists(_outputTrain))
        Directory.CreateDirectory(_outputTrain);

      var logFile = Path.Combine(output, "log.txt");
      if(File.Exists(logFile))
        File.Delete(logFile);
      _logger = new LoggerConfiguration()
        .WriteTo.Console()
        .WriteTo.File(logFile)
        .CreateLogger();
    }

    private static bool ValidInput(string input) 
      => File.Exists(input) || Directory.Exists(input);

    private static bool ValidateComfig(string configJsonPath)
    {
      try
      {
        using var fs = new FileStream(configJsonPath, FileMode.Open, FileAccess.Read);
        _config = System.Text.Json.JsonSerializer.Deserialize<Configuration>(fs);
        return true;
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.Message);
      }
      return false;
    }
  }
}
