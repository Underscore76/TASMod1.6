using System;
using System.Collections.Generic;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace TASMod.Helpers
{
    public class ScreenFadeInfo
    {
        public ScreenFade ScreenFade;
        public bool Active => ScreenFade != null;
        public bool GlobalFade => ScreenFade?.globalFade ?? false;
        public bool FadeIn => ScreenFade?.fadeIn ?? false;
        public bool FadeToBlack => ScreenFade?.fadeToBlack ?? false;
        public float FadeToBlackAlpha => ScreenFade?.fadeToBlackAlpha ?? 0f;
    }
    public class InstanceScreenFade
    {
        public static ScreenFadeInfo Get(int index)
        {
            ScreenFade screenFade;
            // if (GameRunner.instance.gameInstances.Count == 1)
            // {
            //     screenFade = Reflector.GetStaticValue<Game1, ScreenFade>("screenFade");
            //     return new ScreenFadeInfo { ScreenFade = screenFade };
            // }
            screenFade = Reflector.GetStaticVar(index, "Game1_screenFade") as ScreenFade;
            return new ScreenFadeInfo { ScreenFade = screenFade };
        }
    }
}