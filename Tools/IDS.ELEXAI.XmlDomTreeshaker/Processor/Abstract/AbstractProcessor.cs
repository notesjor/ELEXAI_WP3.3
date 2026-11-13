using IDS.ELEXAI.XmlDomTreeshaker.Model;
using Serilog.Core;

namespace IDS.ELEXAI.XmlDomTreeshaker.Processor.Abstract
{
  public abstract class AbstractProcessor
  {
    public abstract string Info { get; }
    public abstract void Execute(Logger logger, Configuration config, string input, string outputEval, string outputTrain);
  }
}
