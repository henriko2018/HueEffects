using CommandLine;

namespace XmasEffect
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(options => { });
        }
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class Options
    {
        [Option('g', "light-group", Required = true, HelpText = "Name of the light group to control.")]
        public string LightGroup { get; set; }

        /// <summary>
        /// Length of color cycle in seconds.
        /// </summary>
        [Option('c', "cycle-length", Required = false, Default = 60, HelpText = "Length of cycle in seconds")]
        public int CycleLength { get; set; }
    }
}
