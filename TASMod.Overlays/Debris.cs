using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using TASMod.Helpers;

namespace TASMod.Overlays
{
    public class DebrisRange : IOverlay
    {
        public override string Name => "Debris";

        public override string Description => "Debris overlay.";

        public override void ActiveDraw(SpriteBatch spriteBatch)
        {
            if (Game1.currentLocation == null)
                return;
            if (Game1.currentLocation.debris.Count == 0)
                return;
            foreach (var debris in Game1.currentLocation.debris)
            {
                Vector2 vec = approximatePosition(debris);
                /*
                    |X + 32 - pX| < radius
                    if X + 32 - pX < 0
                        X + 32 - pX > 
                    else
                        radius + X + 32 >= pX >= -radius + X + 32
                */
                int appliedMagneticRadius = Game1.player.GetAppliedMagneticRadius();
                Rectangle rect = new Rectangle(
                    (int)(-appliedMagneticRadius + vec.X + 32),
                    (int)(-appliedMagneticRadius + vec.Y + 32),
                    2 * appliedMagneticRadius,
                    2 * appliedMagneticRadius
                );
                if (playerInRange(vec, Game1.player))
                {
                    DrawRectOutline(spriteBatch, rect, Color.Green);
                }
                else
                {
                    DrawRectOutline(spriteBatch, rect, Color.Red);
                }
                if (debris.itemId.Value == null)
                    continue;
                DrawTextGlobal(
                        spriteBatch,
                        DropInfo.ObjectName(debris.itemId.Value.Substring(3)),
                        vec,
                        Color.White,
                        Color.Black, 1
                    );
            }
        }

        public Vector2 approximatePosition(Debris debris)
        {
            Vector2 vector = default(Vector2);
            foreach (Chunk chunk in debris.Chunks)
            {
                vector += chunk.position.Value;
            }

            return vector / debris.Chunks.Count;
        }

        private bool playerInRange(Vector2 position, Farmer farmer)
        {
            int appliedMagneticRadius = farmer.GetAppliedMagneticRadius();
            Point standingPixel = farmer.StandingPixel;
            if (Math.Abs(position.X + 32f - (float)standingPixel.X) <= (float)appliedMagneticRadius)
            {
                return Math.Abs(position.Y + 32f - (float)standingPixel.Y) <= (float)appliedMagneticRadius;
            }

            return false;
        }
    }
}