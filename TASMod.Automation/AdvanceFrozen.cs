using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TASMod.Helpers;
using TASMod.Inputs;

namespace TASMod.Automation
{
    public class AdvanceFrozen : IAutomatedLogic
    {
        public override string Name => "AdvanceFrozen";

        public override string Description => "advance when player is mid-animation state";

        private List<string> ValidStrings = new List<string>
        {
            "showHoldingItem",
            "showReceiveNewItemMessage",
            "doSleepEmote",
            "frozen"
        };

        public override bool ActiveUpdate(
            int index,
            out TASKeyboardState kstate,
            out TASMouseState mstate,
            out TASGamePadState gstate
        )
        {
            kstate = null;
            mstate = new TASMouseState(Controller.LastFrameMouse(), false, false);
            gstate = null;
            var menuInfo = InstanceCurrentMenu.Get(index);
            var playerInfo = InstanceCurrentPlayer.Get(index);
            // only want to advance if can't move in a non-tool scenario
            if (!playerInfo.Active || playerInfo.CanMove || playerInfo.UsingTool)
                return false;
            // we can't move cause other stuff is going on
            if (menuInfo.Active)
                return false;
            if (playerInfo.IsEmoting || playerInfo.FreezePause)
                return true;
            string behavior = playerInfo.LastAnimationEndBehavior;
            if (behavior == null)
                behavior = playerInfo.CurrentAnimationStartBehavior;
            if (behavior != null)
            {
                if (ValidStrings.Contains(behavior))
                    return true;
            }
            return false;
        }
    };
}
