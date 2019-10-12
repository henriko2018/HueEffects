﻿using System;
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

        [Option('g', "light-group", Required = false, Default = "0", HelpText = "Name of the light group to control. If omitted, the special group containing all lights is used.")]
        public string LightGroup { get; set; }

        /// <summary>
        /// Length of color cycle in seconds.
        /// </summary>
        [Option('c', "cycle-length", Required = false, Default = 60, HelpText = "Length of cycle in seconds")]
        public int CycleLength { get; set; }
    }
}