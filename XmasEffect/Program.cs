using System;
using System.Threading;
using CommandLine;

namespace XmasEffect
{
    internal static class Program
    {
        private static readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

        private static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(options =>
                {
                    Console.CancelKeyPress += CancelKeyPressed;
                    var effect = new XmasEffect(options);
                    effect.Start(CancellationTokenSource.Token).GetAwaiter().GetResult(); // Ugly way to wait for completion since there is no WithParsedAsync.
                });
        }

        private static void CancelKeyPressed(object sender, ConsoleCancelEventArgs e)
        {
            CancellationTokenSource.Cancel();
        }
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class Options
    {
        [Option('u', "user", Required = true, HelpText = "User (hexadecimal string) created with POST /api")]
        public string User { get; set; }

        [Option('g', "light-group", Required = false, Default = "", HelpText = "Name of the light group to control. If omitted, the special group containing all lights is used.")]
        public string LightGroup { get; set; }

        /// <summary>
        /// Length of color cycle in seconds.
        /// </summary>
        [Option('c', "cycle-length", Required = false, Default = 60, HelpText = "Length of cycle in seconds")]
        public int CycleLength { get; set; }
    }
}
