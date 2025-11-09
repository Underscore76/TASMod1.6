using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StardewValley;
using StardewValley.Menus;
using TASMod.Helpers;
using TASMod.Inputs;

namespace TASMod.Automation
{
    public class DialogueBox : IAutomatedLogic
    {
        public override string Name => "DialogueBox";

        public override string Description => "advance to click off frame";

        public override bool ActiveUpdate(
            int index,
            out TASKeyboardState kstate,
            out TASMouseState mstate,
            out TASGamePadState gstate
        )
        {
            var menuInfo = InstanceCurrentMenu.Get(index);
            var viewport = InstanceViewport.Get(index);
            var minigame = InstanceCurrentMinigame.Get(index);
            kstate = null;
            mstate = new TASMouseState(Controller.LastFrameMouse(), false, false);
            gstate = null;

            if (!menuInfo.Active || !menuInfo.IsDialogue)
                return false;

            if (minigame.Minigame != null)
                return false;

            // transitioning on/off screen
            if (menuInfo.Transitioning)
                return true;

            if (!menuInfo.IsQuestion)
            {
                // force the characters on the screen
                if (menuInfo.CharacterIndexInDialogue == 0)
                {
                    mstate = new TASMouseState(
                        (int)viewport.Center.X,
                        (int)viewport.Center.Y,
                        true,
                        false
                    );
                    return true;
                }
                // waiting after characters have loaded
                if (menuInfo.SafetyTimer > 0)
                    return true;
            }

            return false;
        }
    }
}
