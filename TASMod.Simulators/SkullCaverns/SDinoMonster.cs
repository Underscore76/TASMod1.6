using System;
using Microsoft.Xna.Framework;

namespace TASMod.Simulators.SkullCaverns
{
    public class SDinoMonster : SMonster
    {
        public SDinoMonster(Vector2 position, Random Game1_random)
            : base(position, "Dino Monster", Game1_random)
        {
            int nextChangeDirectionTime = Game1_random.Next(1000, 3000);
            int nextWanderTime = Game1_random.Next(1000, 2000);
            SpriteWidth = 32;
            SpriteHeight = 32;
        }

        public override Rectangle GetBoundingBox()
        {
            Vector2 vector = base.Position;
            return new Rectangle((int)vector.X + 8, (int)vector.Y, SpriteWidth * 4 * 3 / 4, 64);
        }
    }
}
