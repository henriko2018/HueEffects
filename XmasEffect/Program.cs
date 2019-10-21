using System;
using System.Threading.Tasks;
using CommandLine;

namespace XmasEffect
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            await Parser.Default.ParseArguments<Options>(args).MapResult(
                async options =>
                {
                    var effect = new XmasEffect(options);
                    Console.CancelKeyPress += (_, eventArgs) =>
                    {
                        Console.Out.WriteLine("Ctrl-C");
                        eventArgs.Cancel = true;
                        effect.Continue = false;
                    };
                    await effect.Start();
                },
                errors => Task.CompletedTask);
        }
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class Options
    {
        [Option('u', "user", Required = true, HelpText = "User (hexadecimal string) created with POST /api")]
        public string User { get; set; }

        [Option('c', "cycle-length", Required = false, Default = 15, HelpText = "Length of cycle in seconds")]
        public int CycleLength { get; set; }
    }
}
