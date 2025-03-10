using System;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using TASMod.Helpers;
using TASMod.Inputs;

namespace TASMod.Automation
{
    public class AcceptSleep : IAutomatedLogic
    {
        public override string Name => "AcceptSleep";
        public override string Description => "auto accept the sleep dialogue on first frame";

        public AcceptSleep()
        {
            Active = true;
        }

        public override bool ActiveUpdate(
            out TASKeyboardState kstate,
            out TASMouseState mstate,
            out TASGamePadState gstate
        )
        {
            if (
                !CurrentMenu.Active
                || !CurrentMenu.IsDialogue
                || !CurrentMenu.IsQuestion
                || CurrentMenu.Transitioning
                || !CurrentMenu.CurrentString.Equals("Go to sleep for the night?")
            )
            {
                return base.ActiveUpdate(out kstate, out mstate, out gstate);
            }
            Log($"{CurrentMenu.CurrentString}", StardewModdingAPI.LogLevel.Alert);
            kstate = new TASKeyboardState("Y");
            mstate = null;
            gstate = null;
            return true;
        }
    }
}
