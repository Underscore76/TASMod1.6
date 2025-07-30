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
            if (CurrentMenu.Active && Game1.activeClickableMenu is BobberBar bar)
            {
                SFishingGame game = new SFishingGame();
                var state = game.bobberBar;
                DrawRectLocal(spriteBatch,
                    new Rectangle(
                        bar.xPositionOnScreen + 56,
                        bar.yPositionOnScreen + 12 + (int)state.bobberBarPos - 16,
                        52, state.bobberBarHeight - 28
                        ),
                    state.bobberInBar ? Color.Green : Color.Red, 1, true
                    );
                DrawRectLocal(spriteBatch,
                    new Rectangle(
                        bar.xPositionOnScreen + 56,
                        bar.yPositionOnScreen + 12 + (int)state.bobberPosition,
                        52, 1
                        ),
                    new Color(0, 0, 196, 128), 1
                    );
                DrawRectLocal(spriteBatch,
                    new Rectangle(
                        bar.xPositionOnScreen + 56,
                        bar.yPositionOnScreen + 12 + (int)state.bobberTargetPosition,
                        52, 1
                        ),
                    new Color(196, 0, 0, 128), 1
                    );
                if (state.treasure)
                {
                    DrawRectLocal(spriteBatch,
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