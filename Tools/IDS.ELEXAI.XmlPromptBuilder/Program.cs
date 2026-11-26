
using IDS.ELEXAI.XmlPromptBuilder.Model;
using IDS.ELEXAI.XmlToolbox;
using Serilog;
using Serilog.Core;

namespace IDS.ELEXAI.XmlPromptBuilder
{
  internal class Program
  {
    private static Template? _template;
    private static Logger _logger;
    private static string _outputPrompts;

    static void Main(string[] args)
    {
      if (args.Length == 1)
      {
        SingleArgumentHandling(args[0]);
        return;
      }
      if (args.Length != 3)
      {
        PrintHelp();
        return;
      }

      var configJsonPath = args[0];
      var input = args[1];
      var output = args[2];

      if (!ValidateComfig(configJsonPath))
        return;
      if (!ValidInput(input))
        return;
      ValidateOutput(output);

      foreach (var f in Directory.GetFiles(input, "*.*", SearchOption.TopDirectoryOnly))
      {
        var fileName = Path.GetFileName(f);
        _logger.Information($"Processing file: {fileName}");

        if (!XmlHelper.LoadXmlDocument(f, _logger, out var docOriginal))
        {
          _logger.Warning($"Skipping file due to XML parse errors: {fileName}");
          continue;
        }

        var promptFilePath = Path.Combine(_outputPrompts, $"{fileName}.json");

        var instance = new TrainingDataset { 
          Messages = _template.Messages.Select(m => m.Create(docOriginal))
        };

        using var fs = new FileStream(promptFilePath, FileMode.Create, FileAccess.Write);
        System.Text.Json.JsonSerializer.Serialize(fs, instance);
      }
    }

    private static void PrintHelp()
    {
      Console.WriteLine("Usage: pbuild <templateJsonPath> <input> <output>");
      Console.WriteLine("Where:");
      Console.WriteLine("<templateJsonPath> - path to the template JSON file");
      Console.WriteLine("<input> - path to the input file or directory");
      Console.WriteLine("<output> - path to the output directory (create log.txt and prompts)");
      Console.WriteLine("Or: pbuild init - to create an empty configuration file (see examples in repo)");
      Console.WriteLine("Or: pbuild help - to display this help information");
    }

    private static void SingleArgumentHandling(string arg)
    {
      switch (arg)
      {
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
      System.Text.Json.JsonSerializer.Serialize(fs, new Template
      {
        Messages = new List<MessageExtended>
        {
          new MessageExtended
          {
            Role = Role.System,
            Content = "You are an experienced lexicographer who is completing the following task with the goal of later creating a dictionary entry based on this data.",
          },
          new MessageExtended
          {
            Role = Role.User,
            Content = "You get these kwic-snippts: {citation[all].text} - please write a short definition.",
            Rules = new List<Rule>
            {
              new Rule
              {
                Name = "citation",
                XpathInstance = "//citation"
              }
            }
          },
          new MessageExtended
          {
            Role = Role.Assistant,
            Content = "{definition[0].text}",
            Rules = new List<Rule>
            {
              new Rule
              {
                Name = "definition",
                XpathInstance = "//definition"
              }
            }
          }
        }
      });
    }

    private static void ValidateOutput(string output)
    {
      if (!Directory.Exists(output))
        Directory.CreateDirectory(output);
      _outputPrompts = Path.Combine(output, "prompts");
      if (!Directory.Exists(_outputPrompts))
        Directory.CreateDirectory(_outputPrompts);

      var logFile = Path.Combine(output, "log.txt");
      if (File.Exists(logFile))
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
        _template = System.Text.Json.JsonSerializer.Deserialize<Template>(fs);
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
