using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using StardewValley.Menus;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TASMod.Helpers;
using TASMod.Simulators.Fishing;

namespace TASMod.Overlays
{
    public class Fishing : IOverlay
    {
        public override string Name => "Fishing";

        public override string Description => "draws fishing minigame data";

        public override void ActiveDraw(SpriteBatch spriteBatch)
        {
            for (int i = 0; i < GameRunner.instance.gameInstances.Count; i++)
            {
                try
                {
                    DrawForInstance(i, spriteBatch);
                }
                catch (Exception e)
                {
                    ModEntry.Console.Log($"Fishing ActiveDraw Exception: {e}", StardewModdingAPI.LogLevel.Error);
                }
            }
        }
        public void DrawForInstance(int index, SpriteBatch spriteBatch)
        {
            var menu = InstanceCurrentMenu.Get(index); ;
            if (menu.Active && menu.Menu is BobberBar bar)
            {
                SFishingGame game = new SFishingGame(index);
                var state = game.bobberBar;
                DrawRectLocal(index, spriteBatch,
                    new Rectangle(
                        bar.xPositionOnScreen + 56,
                        bar.yPositionOnScreen + 12 + (int)state.bobberBarPos - 16,
                        52, state.bobberBarHeight - 28
                        ),
                    state.bobberInBar ? Color.Green : Color.Red, 1, true
                    );
                DrawRectLocal(index, spriteBatch,
                    new Rectangle(
                        bar.xPositionOnScreen + 56,
                        bar.yPositionOnScreen + 12 + (int)state.bobberPosition,
                        52, 1
                        ),
                    new Color(0, 0, 196, 128), 1
                    );
                DrawRectLocal(index, spriteBatch,
                    new Rectangle(
                        bar.xPositionOnScreen + 56,
                        bar.yPositionOnScreen + 12 + (int)state.bobberTargetPosition,
                        52, 1
                        ),
                    new Color(196, 0, 0, 128), 1
                    );
                if (state.treasure)
                {
                    DrawRectLocal(index, spriteBatch,
                    new Rectangle(
                        bar.xPositionOnScreen + 56,
                        bar.yPositionOnScreen + 12 + (int)state.treasurePosition,
                        52, 1
                        ),
                    new Color(196, 196, 0, 128), 1
                    );
                }
                base.ActiveDraw(spriteBatch);
            }
        }
    }
}