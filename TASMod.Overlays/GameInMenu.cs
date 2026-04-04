using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using TASMod.System;

namespace TASMod.Overlays
{
    public class GameInMenu : IOverlay
    {
        public override string Name => "GameInMenu";
        public override string Description => "draw a rectangle in the topleft corner when in a menu";
        public int StartFrame;
        public GameInMenu()
        {
            Active = false;
        }

        public override void ActiveDraw(SpriteBatch spriteBatch)
        {
            if (TASDateTime.CurrentFrame < (ulong)StartFrame)
                return;
            if (Game1.activeClickableMenu != null && Game1.activeClickableMenu is ItemGrabMenu)
                return;
            DrawRectLocal(0, spriteBatch, new Rectangle(0, 0, 100, 100), Color.Red);
        }
    }
}
