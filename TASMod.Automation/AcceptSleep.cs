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
        int index,
            out TASKeyboardState kstate,
            out TASMouseState mstate,
            out TASGamePadState gstate
        )
        {
            var menuInfo = InstanceCurrentMenu.Get(index);
            if (
                !menuInfo.Active
                || !menuInfo.IsDialogue
                || !menuInfo.IsQuestion
                || menuInfo.Transitioning
                || !menuInfo.CurrentString.Equals("Go to sleep for the night?")
            )
            {
                return base.ActiveUpdate(index, out kstate, out mstate, out gstate);
            }
            kstate = new TASKeyboardState("Y");
            mstate = null;
            gstate = null;
            return true;
        }
    }
}
