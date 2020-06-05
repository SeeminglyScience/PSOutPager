using System;
using System.Management.Automation;

namespace PSOutPager
{
    [Cmdlet(VerbsData.Out, "Pager")]
    public class OutPagerCommand : PSCmdlet, IDisposable
    {
        [Parameter(ValueFromPipeline = true)]
        public PSObject InputObject { get; set; }

        private readonly DisplayWriter _writer = new ConsoleDisplayWriter();

        private readonly SteppablePipeline _pipe = ScriptBlock
            .Create("Out-String -Stream")
            .GetSteppablePipeline(CommandOrigin.Internal);

        public void Dispose()
        {
            _pipe.Dispose();
            _writer.Dispose();
        }

        protected override void BeginProcessing()
        {
            _pipe.Begin(MyInvocation.ExpectingInput);
        }

        protected override void ProcessRecord()
        {
            foreach (PSObject line in _pipe.Process(InputObject))
            {
                _writer.Write(line.BaseObject.ToString());
                _writer.WriteLine();
            }
        }

        protected override void EndProcessing()
        {
            foreach (object lineObj in _pipe.End())
            {
                object baseObj = lineObj is PSObject pso ? pso.BaseObject : lineObj;
                if (baseObj is null)
                {
                    continue;
                }

                if (!(baseObj is string line))
                {
                    throw new InvalidOperationException(
                        "Out-String wrote a non-string object as output. Please report this exception on GitHub.");
                }

                _writer.Write(line);
                _writer.WriteLine();
            }
        }
    }
}
