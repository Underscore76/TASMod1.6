using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;
using StardewValley.TerrainFeatures;
using TASMod.Helpers;
using TASMod.Scripting;

namespace TASMod.Overlays
{
    public class MonsterDrop : IOverlay
    {
        public override string Name => "MonsterDrop";
        public override string Description => "overlay current monster drops";

        public Color TextColor = Color.White;
        public Color RectColor = new Color(0, 0, 0, 180);

        public override void ActiveDraw(SpriteBatch spriteBatch)
        {
            var currentLocation = InstanceCurrentLocation.Get(0);
            var player = InstanceCurrentPlayer.Get(0);
            if (!currentLocation.Active || !player.Active) return;

            if (currentLocation.Location is MineShaft mine)
            {
                foreach (var character in mine.characters)
                {
                    if (character is Monster monster)
                    {
                        Vector2 loc = Game1.GlobalToLocal(new Vector2(monster.GetBoundingBox().Left, monster.GetBoundingBox().Top));
                        foreach (var obj in monster.objectsToDrop)
                        {
                            DrawText(0, spriteBatch, DropInfo.ObjectName(obj), loc, TextColor, RectColor);
                        }
                    }
                }
            }
        }
    }
}