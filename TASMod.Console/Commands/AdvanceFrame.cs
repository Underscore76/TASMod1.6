using System;
using TASMod.Inputs;
using TASMod.Overlays;
using TASMod.Recording;

namespace TASMod.Console.Commands
{
    public class AdvanceFrame : IConsoleCommand
    {
        public override string Name => "advance";
        public override string Description => "take 1 step forward";

        public override void Run(string[] tokens)
        {
            Controller.State.FrameStates.Add(new FrameState());
        }
    }
}
