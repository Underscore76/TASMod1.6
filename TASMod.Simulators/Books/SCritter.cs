using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using TASMod.System;

namespace TASMod.Simulators.Books
{
    public class SCritter
    {
        public Vector2 position;
        public Vector2 startingPosition;
        public StardewValley.AnimatedSprite sprite;
        public int baseFrame;

        public Random random;

        public SCritter(Random r) { random = r; }
        public SCritter(Random r, Vector2 position)
        {
            this.position = position;
            startingPosition = position;
            sprite = new StardewValley.AnimatedSprite();
            random = r;
        }

        public virtual bool update(SGameLocation loc)
        {
            sprite.animateOnce(TASDateTime.CurrentGameTime);
            if (position.X < -128f || position.Y < -128f || position.X > (float)loc.DisplayWidth || position.Y > (float)loc.DisplayHeight)
            {
                return true;
            }
            return false;
        }
    }
}