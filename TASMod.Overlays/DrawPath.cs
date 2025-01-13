using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TASMod.Overlays
{
    public class DrawPath : IOverlay
    {
        public override string Name => "DrawPath";
        public Color color = Color.LightCyan;
        public int thickness = 8;
        public override string Description => "draw active path";

        public override void ActiveDraw(SpriteBatch spriteBatch)
        {
            if (Controller.PathFinder.hasPath && Controller.PathFinder.path != null)
            {
                for (int i = 0; i < Controller.PathFinder.path.Count - 1; i++)
                {
                    DrawLineBetweenTiles(
                        spriteBatch,
                        Controller.PathFinder.path[i].toVector2(),
                        Controller.PathFinder.path[i + 1].toVector2(),
                        color,
                        thickness
                    );
                }
            }
        }
    }
}
