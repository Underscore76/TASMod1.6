using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using TASMod.Console;
using TASMod.Extensions;
using TASMod.Helpers;
using TASMod.Inputs;
using TASMod.Monogame.Framework;

namespace TASMod.Overlays
{
    public abstract class IOverlay : IConsoleAware
    {
        private static int ViewportWidth => Game1.graphics.GraphicsDevice.Viewport.Width;
        private static int ViewportHeight => Game1.graphics.GraphicsDevice.Viewport.Height;
        public bool Active = true;
        public float Priority { get; set; } = 0;
        private static Rectangle? _outlineRect;
        private static Rectangle? OutlineRect
        {
            get
            {
                if (_outlineRect == null)
                    _outlineRect = new Rectangle?(
                        Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 29, -1, -1)
                    );
                return _outlineRect;
            }
        }
        public Texture2D SolidColor
        {
            get { return Console.solidColor; }
        }
        public SpriteFont Font
        {
            get { return Console.consoleFont; }
        }

        public bool Toggle()
        {
            Active = !Active;
            return Active;
        }

        public bool HandleInput(TASMouseState mouseState, TASKeyboardState keyState)
        {
            if (Active)
            {
                return ActiveHandleInput(mouseState, keyState);
            }
            return false;
        }

        public virtual bool ActiveHandleInput(TASMouseState mouseState, TASKeyboardState keyState)
        {
            return false;
        }

        public void Update()
        {
            if (Active)
            {
                ActiveUpdate();
            }
        }

        public virtual void ActiveUpdate() { }

        public void Draw()
        {
            if (Active && TASSpriteBatch.Active)
            {
                if (Game1.spriteBatch.inBeginEndPair())
                    ActiveDraw(Game1.spriteBatch);
            }
        }

        public virtual void ActiveDraw(SpriteBatch spriteBatch) { }

        public virtual void RenderImGui() { }

        public virtual void Reset() { }

        public Rectangle TransformToLocal(int index, Rectangle global)
        {
            var viewport = InstanceViewport.Get(index);
            var options = InstanceOptions.Get(index);
            var window = viewport.Window;
            Rectangle local = new Rectangle(
                (int)((global.X - viewport.X) * options.zoomLevel),
                (int)((global.Y - viewport.Y) * options.zoomLevel),
                Math.Max((int)(global.Width * options.zoomLevel), 1),
                Math.Max((int)(global.Height * options.zoomLevel), 1)
            );
            local.Offset(window.X, window.Y);
            return local;
        }

        public Vector2 TransformToLocal(int index, Vector2 global)
        {
            var viewport = InstanceViewport.Get(index);
            var options = InstanceOptions.Get(index);
            Vector2 local = new Vector2(
                (int)((global.X - viewport.X) * options.zoomLevel) + viewport.Window.X,
                (int)((global.Y - viewport.Y) * options.zoomLevel) + viewport.Window.Y
            );
            return local;
        }

        public Rectangle TileToRect(Vector2 tile)
        {
            return new Rectangle(
                (int)tile.X * Game1.tileSize,
                (int)tile.Y * Game1.tileSize,
                Game1.tileSize,
                Game1.tileSize
            );
        }

        public Rectangle RectFromVecDim(Vector2 vec, Vector2 dim)
        {
            return new Rectangle((int)vec.X, (int)vec.Y, (int)dim.X, (int)dim.Y);
        }

        public Vector2 MeasureString(string text, float scale = 1)
        {
            return Font.MeasureString(text) * scale;
        }

        public void DrawText(
            int index,
            SpriteBatch spriteBatch,
            string text,
            Vector2 vector,
            Color textColor,
            float fontScale = 1,
            bool offsetTopRight = false
        )
        {
            DrawText(
                index,
                spriteBatch,
                text,
                vector,
                textColor,
                Color.Transparent,
                fontScale,
                offsetTopRight
            );
        }

        public void DrawText(
            int index,
            SpriteBatch spriteBatch,
            string text,
            Vector2 vector,
            Color textColor,
            Color backgroundColor,
            float fontScale = 1,
            bool offsetTopRight = false
        )
        {
            var viewport = InstanceViewport.Get(index);
            // measure font and offset vector if drawing offscreen
            Vector2 textSize = MeasureString(text, fontScale);
            if (offsetTopRight)
            {
                vector.X -= textSize.X;
            }
            if (vector.X + textSize.X > ViewportWidth)
                vector.X = ViewportWidth - textSize.X;
            else if (vector.X < 0)
                vector.X = 0;

            if (vector.Y + textSize.Y > ViewportHeight)
                vector.Y = ViewportHeight - textSize.Y;
            else if (vector.Y < 0)
                vector.Y = 0;
            Rectangle background = new Rectangle((int)vector.X, (int)vector.Y, (int)textSize.X, (int)textSize.Y);
            if (!background.Intersects(viewport.Window))
                return;
            spriteBatch.Draw(
                SolidColor,
                background,
                backgroundColor
            );
            spriteBatch.DrawString(
                Font,
                text,
                vector,
                textColor,
                0f,
                Vector2.Zero,
                fontScale,
                SpriteEffects.None,
                1f
            );
        }

        public void DrawText(
            int index,
            SpriteBatch spriteBatch,
            IEnumerable<string> text,
            Vector2 vector,
            Color textColor,
            Color backgroundColor,
            float fontScale = 1,
            bool offsetTopRight = false
        )
        {
            float maxWidth = 0;
            float height = 0;
            float startX = vector.X;
            float startY = vector.Y;
            foreach (var line in text)
            {
                Vector2 textSize = MeasureString(line, fontScale);
                if (textSize.X > maxWidth)
                    maxWidth = textSize.X;
                height += textSize.Y;
            }
            if (offsetTopRight)
            {
                startX -= maxWidth;
            }
            if (startX + maxWidth > ViewportWidth)
                startX = ViewportWidth - maxWidth;
            else if (startX < 0)
                startX = 0;
            if (startY + height > ViewportHeight)
                startY = ViewportHeight - height;
            else if (startY < 0)
                startY = 0;

            spriteBatch.Draw(
                SolidColor,
                new Rectangle((int)startX, (int)startY, (int)maxWidth, (int)height),
                backgroundColor
            );
            Vector2 start = new Vector2(startX, startY);
            foreach (var line in text)
            {
                spriteBatch.DrawString(
                    Font,
                    line,
                    start,
                    textColor,
                    0f,
                    Vector2.Zero,
                    fontScale,
                    SpriteEffects.None,
                    1f
                );
                start.Y += Font.LineSpacing;
            }
        }

        public void DrawTextGlobal(
            int index,
            SpriteBatch spriteBatch,
            string text,
            Vector2 global,
            Color textColor,
            Color backgroundColor,
            float fontScale = 1
        )
        {
            var options = InstanceOptions.Get(index);
            Vector2 local = TransformToLocal(index, global);
            DrawText(
                index,
                spriteBatch,
                text,
                local,
                textColor,
                backgroundColor,
                fontScale * options.zoomLevel
            );
        }

        public void DrawTextAtTile(
            int index,
            SpriteBatch spriteBatch,
            string text,
            Vector2 tile,
            Color textColor,
            Color backgroundColor,
            float fontScale = 1
        )
        {
            var options = InstanceOptions.Get(index);
            Vector2 local = TransformToLocal(index, tile * Game1.tileSize);
            DrawText(
                index,
                spriteBatch,
                text,
                local,
                textColor,
                backgroundColor,
                fontScale * options.zoomLevel
            );
        }

        public void DrawTextAtTile(
            int index,
            SpriteBatch spriteBatch,
            IEnumerable<string> text,
            Vector2 tile,
            Color textColor,
            Color backgroundColor,
            float fontScale = 1
        )
        {
            var options = InstanceOptions.Get(index);
            Vector2 local = TransformToLocal(index, tile * Game1.tileSize);
            DrawText(
                index,
                spriteBatch,
                text,
                local,
                textColor,
                backgroundColor,
                fontScale * options.zoomLevel
            );
        }

        public void DrawObjectSpriteGlobal(
            int index,
            SpriteBatch spriteBatch,
            Vector2 global,
            Vector2 dim,
            int parentSheetIndex
        )
        {
            Rectangle rect = TransformToLocal(index, RectFromVecDim(global, dim));
            DrawObjectSpriteLocal(index, spriteBatch, rect, parentSheetIndex);
        }

        // draw over on screen tile
        public void DrawObjectSpriteAtTile(
            int index,
            SpriteBatch spriteBatch,
            Vector2 tile,
            int parentSheetIndex
        )
        {
            Rectangle sourceRect = GameLocation.getSourceRectForObject(parentSheetIndex);
            Rectangle destRect = TransformToLocal(index, TileToRect(tile));
            DrawObjectSpriteRect(index, spriteBatch, sourceRect, destRect);
        }

        public void DrawObjectSpriteLocal(
            int index,
            SpriteBatch spriteBatch,
            Vector2 start,
            Vector2 dim,
            int parentSheetIndex
        )
        {
            Rectangle sourceRect = GameLocation.getSourceRectForObject(parentSheetIndex);
            Rectangle destRect = RectFromVecDim(start, dim);
            DrawObjectSpriteRect(index, spriteBatch, sourceRect, destRect);
        }

        public void DrawObjectSpriteLocal(
            int index,
            SpriteBatch spriteBatch,
            Rectangle destRect,
            int parentSheetIndex
        )
        {
            Rectangle sourceRect = GameLocation.getSourceRectForObject(parentSheetIndex);
            DrawObjectSpriteRect(index, spriteBatch, sourceRect, destRect);
        }

        private void DrawObjectSpriteRect(
            int index,
            SpriteBatch spriteBatch,
            Rectangle sourceRect,
            Rectangle destRect
        )
        {
            // TODO
            spriteBatch.Draw(Game1.objectSpriteSheet, destRect, sourceRect, Color.White);
        }

        public void DrawLineGlobal(
            int index,
            SpriteBatch spriteBatch,
            Vector2 start,
            Vector2 end,
            Color color,
            int thickness = 1
        )
        {
            Vector2 startCoord = TransformToLocal(index, start);
            Vector2 endCoord = TransformToLocal(index, end);
            DrawLineLocal(index, spriteBatch, startCoord, endCoord, color, thickness);
        }

        public void DrawLineBetweenTiles(
            int index,
            SpriteBatch spriteBatch,
            Vector2 startTile,
            Vector2 endTile,
            Color color,
            int thickness = 1
        )
        {
            Vector2 startCoord = TransformToLocal(index,
                (startTile + new Vector2(0.5f, 0.5f)) * Game1.tileSize
            );
            Vector2 endCoord = TransformToLocal(index,
                (endTile + new Vector2(0.5f, 0.5f)) * Game1.tileSize
            );
            DrawLineLocal(index, spriteBatch, startCoord, endCoord, color, thickness);
        }

        public void DrawLineLocalToGlobal(
            int index,
            SpriteBatch spriteBatch,
            Vector2 local,
            Vector2 global,
            Color color,
            int thickness = 1
        )
        {
            Vector2 globalCoord = TransformToLocal(index, global);
            DrawLineLocal(index, spriteBatch, local, globalCoord, color, thickness);
        }

        public void DrawLineLocalToTile(
            int index,
            SpriteBatch spriteBatch,
            Vector2 local,
            Vector2 tile,
            Color color,
            int thickness = 1
        )
        {
            Vector2 tileCoord = TransformToLocal(index, (tile + new Vector2(0.5f, 0.5f)) * Game1.tileSize);
            DrawLineLocal(index, spriteBatch, local, tileCoord, color, thickness);
        }

        public void DrawLineLocal(
            int index,
            SpriteBatch spriteBatch,
            Vector2 start,
            Vector2 end,
            Color color,
            int thickness = 1
        )
        {
            Vector2 edge = end - start;
            float angle = (float)Math.Atan2(edge.Y, edge.X);
            // generate the clipped rectangle
            Rectangle rect = new Rectangle(
                start.X > end.X ? (int)end.X : (int)start.X,
                start.Y > end.Y ? (int)end.Y : (int)start.Y,
                (int)Math.Abs(start.X - end.X),
                (int)Math.Abs(start.Y - end.Y)
            );
            var window = GameRunner.instance.gameInstances[index].localMultiplayerWindow;
            rect = Rectangle.Intersect(rect, window);
            edge = new Vector2(rect.Width, rect.Height);
            // which rect corner should I pick?
            // want the corner that is closest to the start point
            int startX = start.X > end.X ? rect.Right : rect.Left;
            int startY = start.Y > end.Y ? rect.Bottom : rect.Top;
            Rectangle line = new(startX, startY, (int)edge.Length(), thickness);

            spriteBatch.Draw(
                SolidColor,
                line,
                null,
                color,
                angle,
                Vector2.Zero,
                SpriteEffects.None,
                0
            );
        }

        public void DrawLineTileToPlayer(
            int index,
            SpriteBatch spriteBatch,
            Vector2 tile,
            Color color,
            int thickness = 1
        )
        {
            var player = InstanceCurrentPlayer.Get(index);
            Vector2 tileCoord = TransformToLocal(index, (tile + new Vector2(0.5f, 0.5f)) * Game1.tileSize);
            Vector2 playerCoord = TransformToLocal(index,
                Utility.PointToVector2(player.BoundingBox.Center)
            );
            DrawLineLocal(index, spriteBatch, playerCoord, tileCoord, color, thickness);
        }

        public void DrawRectOutline(int index, SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness = 1)
        {
            Rectangle localRect = TransformToLocal(index, rect);
            DrawLineLocal(index, spriteBatch, new Vector2(localRect.Left, localRect.Top), new Vector2(localRect.Right, localRect.Top), color, thickness);
            DrawLineLocal(index, spriteBatch, new Vector2(localRect.Left, localRect.Bottom), new Vector2(localRect.Right, localRect.Bottom), color, thickness);
            DrawLineLocal(index, spriteBatch, new Vector2(localRect.Left, localRect.Top), new Vector2(localRect.Left, localRect.Bottom), color, thickness);
            DrawLineLocal(index, spriteBatch, new Vector2(localRect.Right, localRect.Top), new Vector2(localRect.Right, localRect.Bottom), color, thickness);
        }

        public void DrawRectGlobal(int index, SpriteBatch spriteBatch, Rectangle rect, Color color)
        {
            Rectangle localRect = TransformToLocal(index, rect);
            DrawRectLocal(index, spriteBatch, localRect, color);
        }

        public void DrawRectGlobal(
            int index,
            SpriteBatch spriteBatch,
            Rectangle rect,
            Color color,
            Color crossColor
        )
        {
            Rectangle localRect = TransformToLocal(index, rect);
            DrawRectLocal(index, spriteBatch, localRect, color, crossColor);
        }

        public void DrawRectLocal(
            int index,
            SpriteBatch spriteBatch,
            Rectangle rect,
            Color color,
            int thickness = 1,
            bool filled = true
        )
        {
            if (filled)
            {
                var viewport = InstanceViewport.Get(index);
                var drawRect = Rectangle.Intersect(rect, viewport.Window);
                spriteBatch.Draw(SolidColor, drawRect, null, color);
            }
            else
            {
                DrawLineLocal(
                    index,
                    spriteBatch,
                    new Vector2(rect.Left, rect.Top),
                    new Vector2(rect.Right, rect.Top),
                    color,
                    thickness
                );
                DrawLineLocal(
                    index,
                    spriteBatch,
                    new Vector2(rect.Left, rect.Bottom),
                    new Vector2(rect.Right, rect.Bottom),
                    color,
                    thickness
                );
                DrawLineLocal(
                    index,
                    spriteBatch,
                    new Vector2(rect.Left, rect.Top),
                    new Vector2(rect.Left, rect.Bottom),
                    color,
                    thickness
                );
                DrawLineLocal(
                    index,
                    spriteBatch,
                    new Vector2(rect.Right, rect.Top),
                    new Vector2(rect.Right, rect.Bottom),
                    color,
                    thickness
                );
            }
        }

        public void DrawRectLocal(
            int index,
            SpriteBatch spriteBatch,
            Rectangle rect,
            Color color,
            Color crossColor
        )
        {
            var window = GameRunner.instance.gameInstances[index].localMultiplayerWindow;
            var drawRect = Rectangle.Intersect(rect, window);
            spriteBatch.Draw(SolidColor, drawRect, color);
            DrawLineLocal(
                index,
                spriteBatch,
                new Vector2(rect.Left, rect.Center.Y),
                new Vector2(rect.Right, rect.Center.Y),
                crossColor
            );
            DrawLineLocal(
                index,
                spriteBatch,
                new Vector2(rect.Center.X, rect.Top),
                new Vector2(rect.Center.X, rect.Bottom),
                crossColor
            );
        }

        public void DrawFilledTile(int index, SpriteBatch spriteBatch, Vector2 tile, Color color)
        {
            Rectangle rect = TileToRect(tile);
            DrawRectGlobal(index, spriteBatch, rect, color);
        }

        public void DrawCenteredTextInRectGlobal(
            int index,
            SpriteBatch spriteBatch,
            Rectangle rect,
            string text,
            Color color,
            float fontScale = 1,
            int shadowOffset = 0
        )
        {
            // measure font and offset vector if drawing offscreen
            Rectangle local = TransformToLocal(index, rect);
            Vector2 textSize = MeasureString(text, fontScale);
            Vector2 pos =
                (new Vector2(local.Width - textSize.X, local.Height - textSize.Y) / 2)
                + new Vector2(local.X, local.Y);
            if (shadowOffset != 0)
                spriteBatch.DrawString(
                    Font,
                    text,
                    new Vector2(pos.X + shadowOffset, pos.Y + shadowOffset),
                    Color.Black,
                    0f,
                    Vector2.Zero,
                    fontScale,
                    SpriteEffects.None,
                    1f
                );
            spriteBatch.DrawString(
                Font,
                text,
                pos,
                color,
                0f,
                Vector2.Zero,
                fontScale,
                SpriteEffects.None,
                1f
            );
        }

        public void DrawCenteredTextInRectLocal(
            int index,
            SpriteBatch spriteBatch,
            Rectangle rect,
            string text,
            Color color,
            float fontScale = 1,
            int shadowOffset = 0
        )
        {
            // TODO
            // measure font and offset vector if drawing offscreen
            Vector2 textSize = MeasureString(text, fontScale);
            Vector2 pos =
                (new Vector2(rect.Width - textSize.X, rect.Height - textSize.Y) / 2)
                + new Vector2(rect.X, rect.Y);
            if (shadowOffset != 0)
                spriteBatch.DrawString(
                    Font,
                    text,
                    new Vector2(pos.X + shadowOffset, pos.Y + shadowOffset),
                    Color.Black,
                    0f,
                    Vector2.Zero,
                    fontScale,
                    SpriteEffects.None,
                    1f
                );
            spriteBatch.DrawString(
                Font,
                text,
                pos,
                color,
                0f,
                Vector2.Zero,
                fontScale,
                SpriteEffects.None,
                1f
            );
        }

        public float FitTextInTile(int index, string text, float minScale = 1, float maxScale = 2, float stepSize = 0.1f)
        {
            var options = InstanceOptions.Get(index);
            float fontScale = maxScale;
            while (fontScale > minScale)
            {
                float localFontScale = fontScale * options.zoomLevel;
                Vector2 textSize = MeasureString(text, localFontScale);
                if (textSize.X < Game1.tileSize)
                    return fontScale;
                fontScale -= stepSize;
            }
            return minScale;
        }

        public void DrawCenteredTextInTile(
            int index,
            SpriteBatch spriteBatch,
            Vector2 tile,
            string text,
            Color color,
            float fontScale = 1,
            int shadowOffset = 1
        )
        {
            var options = InstanceOptions.Get(index);
            // measure font and offset vector if drawing offscreen
            Rectangle rect = TransformToLocal(index, TileToRect(tile));
            float localFontScale = fontScale * options.zoomLevel;
            Vector2 textSize = MeasureString(text, localFontScale);
            Vector2 pos =
                (new Vector2(rect.Width - textSize.X, rect.Height - textSize.Y) / 2)
                + new Vector2(rect.X, rect.Y);
            spriteBatch.DrawString(
                Font,
                text,
                new Vector2(pos.X + shadowOffset, pos.Y + shadowOffset),
                Color.Black,
                0f,
                Vector2.Zero,
                localFontScale,
                SpriteEffects.None,
                1f
            );
            spriteBatch.DrawString(
                Font,
                text,
                pos,
                color,
                0f,
                Vector2.Zero,
                localFontScale,
                SpriteEffects.None,
                1f
            );
        }

        public void DrawTileOutline(
            int index,
            SpriteBatch spriteBatch,
            Vector2 tile,
            Color color,
            float scale = 1f
        )
        {
            var options = InstanceOptions.Get(index);
            var viewport = InstanceViewport.Get(index);
            Vector2 local = TransformToLocal(index, tile * Game1.tileSize);
            Rectangle rect = new Rectangle((int)local.X, (int)local.Y, (int)(Game1.tileSize * scale * options.zoomLevel), (int)(Game1.tileSize * scale * options.zoomLevel));
            if (!rect.Intersects(viewport.Window))
                return;
            spriteBatch.Draw(
                Game1.mouseCursors,
                local,
                OutlineRect,
                color,
                0.0f,
                Vector2.Zero,
                scale * options.zoomLevel,
                SpriteEffects.None,
                1f
            );
        }

        public void DrawTileOutline(
            int index,
            SpriteBatch spriteBatch,
            Vector2 tile,
            Color color,
            Vector2 scale
        )
        {
            var options = InstanceOptions.Get(index);
            var viewport = InstanceViewport.Get(index);
            Vector2 local = TransformToLocal(index, tile * Game1.tileSize);
            Rectangle rect = new Rectangle((int)local.X, (int)local.Y, (int)(Game1.tileSize * scale.X * options.zoomLevel), (int)(Game1.tileSize * scale.Y * options.zoomLevel));
            if (!rect.Intersects(viewport.Window))
                return;
            spriteBatch.Draw(
                Game1.mouseCursors,
                local,
                OutlineRect,
                color,
                0.0f,
                Vector2.Zero,
                scale * options.zoomLevel,
                SpriteEffects.None,
                1f
            );
        }

        public void DrawRectFromTexture(
            int index,
            SpriteBatch spriteBatch,
            Texture2D texture,
            Rectangle sourceRect,
            Rectangle destRect,
            Color color
        )
        {
            spriteBatch.Draw(texture, destRect, sourceRect, color);
        }

        public void DrawViewport(
            int index,
            SpriteBatch spriteBatch,
            RenderTarget2D target,
            xTile.Dimensions.Rectangle viewport,
            Rectangle destRect,
            Color color
        )
        {
            Rectangle rect = new Rectangle(viewport.X, viewport.Y, viewport.Width, viewport.Height);
            Rectangle screen = new Rectangle(
                0,
                0,
                (int)(destRect.Height * (float)rect.Width / rect.Height),
                destRect.Height
            );
            spriteBatch.Draw(target, screen, rect, color, 0f, Vector2.Zero, SpriteEffects.None, 0f);
            //spriteBatch.Draw(target, destRect, rect, color, 0f, Vector2.Zero, SpriteEffects.None, 0f);
        }

        public void Log(string message, LogLevel level)
        {
            if (ModEntry.Console != null)
            {
                ModEntry.Console.Log(message, level);
            }
        }

        public enum Quadrant
        {
            TOP_LEFT = 0,
            TOP_RIGHT,
            BOTTOM_LEFT,
            BOTTOM_RIGHT
        }

        public void DrawTileQuadrant(
            int index,
            SpriteBatch spriteBatch,
            Vector2 tile,
            Color color,
            Quadrant quadrant,
            float fraction = 1f
        )
        {
            Rectangle rect = Game1.GlobalToLocal(Game1.viewport, TileToRect(tile));
            rect.Width /= 2;
            rect.Height /= 2;
            switch (quadrant)
            {
                case Quadrant.TOP_LEFT:
                    break;
                case Quadrant.TOP_RIGHT:
                    rect.X += rect.Width;
                    break;
                case Quadrant.BOTTOM_LEFT:
                    rect.Y += rect.Height;
                    break;
                case Quadrant.BOTTOM_RIGHT:
                    rect.X += rect.Width;
                    rect.Y += rect.Height;
                    break;
            }
            rect.Height = (int)(rect.Height * fraction);
            spriteBatch.Draw(Game1.staminaRect, rect, color);
        }
    }
}
