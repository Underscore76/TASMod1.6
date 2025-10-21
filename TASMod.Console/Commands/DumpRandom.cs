using System;
using System.IO;
using StardewValley.Locations;
using TASMod.Extensions;
using TASMod.Overlays;

namespace TASMod.Console.Commands
{
    public class DumpRandom : IConsoleCommand
    {
        public override string Name => "dump_random";
        public override string Description => "dump_random_stack_traces";

        public override void Run(string[] tokens)
        {
            int startFrame = 0;
            if (tokens.Length >= 1)
            {
                if (!Int32.TryParse(tokens[0], out startFrame))
                {
                    Error($"Invalid frame number {tokens[0]}");
                    return;
                }
            }
            WriteRandomsToFile("randomtraces", startFrame);
        }

        public void WriteRandomsToFile(string name, int startFrame)
        {
            name += ".txt";
            string filePath = Path.Combine(Constants.BasePath, name);
            using (StreamWriter file = File.CreateText(filePath))
            {
                foreach (var frameTraces in RandomExtensions.StackTraces)
                {
                    if (frameTraces.Key < startFrame)
                        continue;
                    file.WriteLine($"Frame\t{frameTraces.Key}");
                    foreach (var playerTraces in frameTraces.Value)
                    {
                        file.WriteLine($"\tPlayer\t{playerTraces.Key}\tTraces\t{playerTraces.Value.Count}");
                        foreach (var trace in playerTraces.Value)
                        {
                            var lines = trace.Split('\n');
                            foreach (var line in lines)
                            {
                                if (
                                    line.Contains(
                                        "at StardewModdingAPI.Framework.SCore.OnGameUpdating(GameTime gameTime, Action runGameUpdate)"
                                    )
                                )
                                {
                                    break;
                                }
                                var cleanLine = CleanLine(line);
                                if (cleanLine != "")
                                    file.WriteLine(cleanLine);
                            }
                            file.WriteLine();
                        }
                    }
                }
            }
        }

        private string CleanLine(string line)
        {
            if (line.Contains("at TASMod") || line.Contains("at Microsoft.Xna.Framework"))
                return "";

            return line.Replace(
                    "D:\\GitlabRunner\\builds\\Gq5qA5P4\\1\\ConcernedApe\\stardewvalley\\Farmer\\Farmer\\",
                    ""
                )
                .Replace("E:\\source\\_Stardew\\SMAPI\\src\\", "");
        }
    }
}
