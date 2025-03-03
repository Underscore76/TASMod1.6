using System;
using Microsoft.Xna.Framework;
using StardewValley;

namespace TASMod.Simulators.Fishing
{
    public class SFarmer
    {
        public ulong millisecondsPlayed;
        public int noMovementPause;
        public int freezePause;
        public bool CanMove;
        public float jitterStrength;
        public Vector2 jitter;
        public int blinkTimer;
        public bool isCrafting;
        public SGameLocation currentLocation;

        public SFarmer()
        {
            millisecondsPlayed = Game1.player.millisecondsPlayed;
            noMovementPause = Game1.player.noMovementPause;
            freezePause = Game1.player.freezePause;
            CanMove = Game1.player.CanMove;
            jitterStrength = Game1.player.jitterStrength;
            jitter = Game1.player.jitter;
            blinkTimer = Game1.player.blinkTimer;
        }

        public void Update(GameTime gameTime, SGameLocation location, Random Game1_random)
        {

            updateCommon(gameTime, location, Game1_random);
        }

        public void updateCommon(GameTime time, SGameLocation location, Random Game1_random)
        {
            if (jitterStrength > 0f)
            {
                jitter = new Vector2((float)Game1_random.Next(-(int)(jitterStrength * 100f), (int)((jitterStrength + 1f) * 100f)) / 100f, (float)Game1_random.Next(-(int)(jitterStrength * 100f), (int)((jitterStrength + 1f) * 100f)) / 100f);
            }

            blinkTimer += time.ElapsedGameTime.Milliseconds;
            if (blinkTimer > 2200 && Game1_random.NextDouble() < 0.01)
            {
                blinkTimer = -150;
            }
            else if (blinkTimer > -100)
            {

            }
        }
    }
}