using System;
using Microsoft.Xna.Framework.Input;
using TASMod.Helpers;
using TASMod.Inputs;

namespace TASMod.Automation
{
    public class ScreenFade : IAutomatedLogic
    {
        public override string Name => "ScreenFade";
        public override string Description => "auto advance frames during screen transitions";

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
            var locationInfo = InstanceCurrentLocation.Get(index);
            var menuInfo = InstanceCurrentMenu.Get(index);
            var eventInfo = InstanceCurrentEvent.Get(index);
            var playerInfo = InstanceCurrentPlayer.Get(index);
            var fadeInfo = InstanceScreenFade.Get(index);

            if (!locationInfo.Active || menuInfo.Active || eventInfo.Active)
                return false;

            if (fadeInfo.GlobalFade)
                return true;
            if (fadeInfo.FadeIn && fadeInfo.FadeToBlackAlpha < 1f && fadeInfo.FadeToBlackAlpha != 0)
                return true;
            if (fadeInfo.FadeToBlack && fadeInfo.FadeToBlackAlpha > 0f && !playerInfo.CanMove)
                return true;

            return false;
        }
    }
}
