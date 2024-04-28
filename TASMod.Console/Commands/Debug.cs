using System;
using StardewValley;
using StardewValley.Locations;
using TASMod.Overlays;

namespace TASMod.Console.Commands
{
    public class Debug : IConsoleCommand
    {
        public override string Name => "debug";
        public override string Description => "run some debug code";

        public override void Run(string[] tokens)
        {
            // Alert("Checking what special keys are locked");
            // foreach (var v in TASConsole.handler.specialKeys)
            // {
            //     Alert($"\t{v.Key}: \"{v.Value}\"");
            // }
            // var tbh = Controller.Overlays["TextBoxHelper"] as TextBoxHelper;

            // Alert("Checking status of TextBoxHelper");
            // Alert("\tWasListening: " + tbh.WasListening);
            // Alert("\tIsListening: " + tbh.Listening);
            // Alert("\tLastBox: " + tbh.LastBox);
            Alert($"{Game1.random}");
            if (Game1.currentLocation is MineShaft shaft)
            {
                Alert($"{shaft.mineRandom}");
            }
            // Controller.Console.DebugMode = !Controller.Console.DebugMode;
            // Alert($"Toggle debug mode: {Controller.Console.DebugMode}");
        }
    }
}
