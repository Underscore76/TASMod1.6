using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TASMod.Helpers;
using TASMod.Inputs;

namespace TASMod.Automation
{
    public class SaveGame : IAutomatedLogic
    {
        public override string Name => "SaveGame";
        public override string Description => "advance frame through night save";

        public override bool ActiveUpdate(
            int index,
            out TASKeyboardState kstate,
            out TASMouseState mstate,
            out TASGamePadState gstate
        )
        {
            var menuInfo = InstanceCurrentMenu.Get(index);
            kstate = null;
            mstate = new TASMouseState(Controller.LastFrameMouse(), false, false);
            gstate = null;
            if (menuInfo.IsSaveGame)
            {
                return !menuInfo.CanQuit;
            }
            return false;
        }
    };
}
