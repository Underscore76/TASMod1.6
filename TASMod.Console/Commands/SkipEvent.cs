using System;
using System.Collections.Generic;
using System.Linq;
using TASMod.Helpers;
using TASMod.Inputs;

namespace TASMod.Automation
{
    public class SkipEvent : IAutomatedLogic
    {
        public override string Name => "SkipEvent";
        public override string Description => "auto skip events";
        public override bool ActiveUpdate(int index, out TASKeyboardState kstate, out TASMouseState mstate, out TASGamePadState gstate)
        {
            if (index != 0) // only player 1
            {
                return base.ActiveUpdate(index, out kstate, out mstate, out gstate);
            }
            kstate = null;
            mstate = new TASMouseState(Controller.LastFrameMouse(), false, false);
            gstate = null;
            var CurrentLocation = InstanceCurrentLocation.Get(0);
            var CurrentMenu = InstanceCurrentMenu.Get(0);
            var CurrentEvent = InstanceCurrentEvent.Get(0);
            if (!CurrentLocation.Active || CurrentMenu.Active || !CurrentEvent.Active)
                return false;

            if (CurrentEvent.Skippable)
            {
                kstate = new TASKeyboardState("F");
                return true;
            }
            if (CurrentEvent.CurrentCommand.Contains("pause"))
                return true;

            return false;
        }
    };
}