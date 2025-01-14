using Microsoft.Xna.Framework;

namespace TASMod.Simulators.SkullCaverns
{
    public class SNPC
    {
        public Vector2 Position { get; set; }
        public string Name { get; set; }
        public bool IsInvisible { get; set; }
        public bool farmerPassesThrough { get; set; }
        public int SpriteWidth { get; set; }
        public int SpriteHeight { get; set; }

        public SNPC(Vector2 position, string name)
        {
            SpriteWidth = 16;
            SpriteHeight = 24;
            Position = position;
            Name = name;
        }

        public virtual Microsoft.Xna.Framework.Rectangle GetBoundingBox()
        {
            Vector2 vector = Position;
            int width = SpriteWidth * 4 * 3 / 4;
            return new Microsoft.Xna.Framework.Rectangle(
                (int)vector.X + 8,
                (int)vector.Y + 16,
                width,
                32
            );
        }

        public void setTilePosition(Point p)
        {
            setTilePosition(p.X, p.Y);
        }

        public void setTilePosition(int x, int y)
        {
            Position = new Vector2(x * 64, y * 64);
        }
    }
}
