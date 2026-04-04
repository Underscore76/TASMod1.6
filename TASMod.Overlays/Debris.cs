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
            for (int i = 0; i < GameRunner.instance.gameInstances.Count; i++)
            {
                try
                {
                    DrawForInstance(i, spriteBatch);
                }
                catch (Exception e)
                {
                    Error($"Debris ActiveDraw Exception: {e}");
                }
            }
        }
        public void DrawForInstance(int index, SpriteBatch spriteBatch)
        {
            var currentLocation = InstanceCurrentLocation.Get(index).Location;
            var player = InstanceCurrentPlayer.Get(index).Player;
            if (currentLocation == null)
                return;
            if (currentLocation.debris.Count == 0)
                return;
            foreach (var debris in currentLocation.debris)
            {
                Vector2 vec = approximatePosition(debris);
                /*
                    |X + 32 - pX| < radius
                    if X + 32 - pX < 0
                        X + 32 - pX > 
                    else
                        radius + X + 32 >= pX >= -radius + X + 32
                */
                int appliedMagneticRadius = player.GetAppliedMagneticRadius();
                Rectangle rect = new Rectangle(
                    (int)(-appliedMagneticRadius + vec.X + 32),
                    (int)(-appliedMagneticRadius + vec.Y + 32),
                    2 * appliedMagneticRadius,
                    2 * appliedMagneticRadius
                );
                if (debris.itemId.Value == null)
                    continue;
                if (debris.itemId.Value.Length < 3 || debris.itemId.Value.Substring(0, 3) != "(O)")
                    continue;
                string name = DropInfo.ObjectName(debris.itemId.Value.Substring(3));
                if (name == "unknown")
                    continue;
                if (playerInRange(vec, player))
                {
                    DrawRectOutline(index, spriteBatch, rect, Color.Green, 2);
                }
                else
                {
                    DrawRectOutline(index, spriteBatch, rect, Color.Purple, 2);
                }
                DrawTextGlobal(
                        index,
                        spriteBatch,
                        name,
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