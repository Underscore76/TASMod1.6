using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.GameData.HomeRenovations;
using XMouse = Microsoft.Xna.Framework.Input.Mouse;

namespace TASMod.Overlays
{
    public class Mouse : IOverlay
    {
        public override string Name => "Mouse";
        public override string Description => "display the real mouse over the screen";

        public Texture2D Cursor;
        public Color MouseColor = Color.Black;
        public Mouse() : base()
        {
            Priority = 1000;
        }

        public void BuildCursor()
        {
            if (Cursor == null && Game1.content != null)
            {
                var tex = Game1.content.Load<Texture2D>("LooseSprites\\Cursors");
                Color[] data = new Color[15 * 15];
                Rectangle sourceRect = Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, Game1.cursor_default, 15, 15);
                tex.GetData(0, sourceRect, data, 0, data.Length);
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i].A > 0)
                    {
                        data[i] = Color.White;
                    }
                }

                Cursor = new Texture2D(
                    Game1.graphics.GraphicsDevice,
                    15,
                    15,
                    false,
                    SurfaceFormat.Color
                );
                Cursor.SetData(data);
            }
        }

        public override void ActiveDraw(SpriteBatch spriteBatch)
        {
            BuildCursor();

            MouseState mouseState = XMouse.GetState();
            Vector2 coords = new Vector2(mouseState.X, mouseState.Y);
            //(int)((float)mouseState.X / (1f / Game1.options.zoomLevel)),
            //(int)((float)mouseState.Y / (1f / Game1.options.zoomLevel))
            spriteBatch.Draw(Cursor, coords, null, MouseColor,
                0f,
                Vector2.Zero,
                4f + Game1.dialogueButtonScale / 150f,
                SpriteEffects.None,
                1f
            );
        }
    }
}
