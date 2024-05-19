using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.GameData;

namespace TASMod.Minigames
{
    public static class SMineCartGlobal
    {
        public static void PlaySound(bool shouldPlaySound, string soundName, int? pitch = null)
        {
            if (shouldPlaySound)
            {
                Game1.playSound(soundName, pitch);
            }
        }

        public static void PlaySound(bool shouldPlaySound, string soundName, out ICue cue)
        {
            cue = new DummyCue();
            if (shouldPlaySound)
            {
                Game1.playSound(soundName, out cue);
            }
        }

        public static void PlaySound(
            bool shouldPlaySound,
            string soundName,
            int pitch,
            out ICue cue
        )
        {
            cue = new DummyCue();
            if (shouldPlaySound)
            {
                Game1.playSound(soundName, pitch, out cue);
            }
        }

        public static void ChangeMusicTrack(
            bool shouldPlaySound,
            string newTrackName,
            bool track_interruptable = false,
            MusicContext music_context = MusicContext.Default
        )
        {
            if (shouldPlaySound)
            {
                Game1.changeMusicTrack(newTrackName, track_interruptable, music_context);
            }
        }

        public static void Draw(
            bool shouldDraw,
            SpriteBatch b,
            Texture2D texture,
            Vector2 position,
            Rectangle? sourceRectangle,
            Color color,
            float rotation,
            Vector2 origin,
            float scale,
            SpriteEffects effects,
            float layerDepth
        )
        {
            if (shouldDraw)
            {
                b.Draw(
                    texture,
                    position,
                    sourceRectangle,
                    color,
                    rotation,
                    origin,
                    scale,
                    effects,
                    layerDepth
                );
            }
        }

        public static void Draw(
            bool shouldDraw,
            SpriteBatch b,
            Texture2D texture,
            Vector2 position,
            Rectangle? sourceRectangle,
            Color color,
            float rotation,
            Vector2 origin,
            Vector2 scale,
            SpriteEffects effects,
            float layerDepth
        )
        {
            if (shouldDraw)
            {
                b.Draw(
                    texture,
                    position,
                    sourceRectangle,
                    color,
                    rotation,
                    origin,
                    scale,
                    effects,
                    layerDepth
                );
            }
        }

        public static void Draw(
            bool shouldDraw,
            SpriteBatch b,
            Texture2D texture,
            Rectangle destinationRectangle,
            Rectangle? sourceRectangle,
            Color color,
            float rotation,
            Vector2 origin,
            SpriteEffects effects,
            float layerDepth
        )
        {
            if (shouldDraw)
            {
                b.Draw(
                    texture,
                    destinationRectangle,
                    sourceRectangle,
                    color,
                    rotation,
                    origin,
                    effects,
                    layerDepth
                );
            }
        }

        public static void DrawString(
            bool shouldDraw,
            SpriteBatch b,
            SpriteFont spriteFont,
            string text,
            Vector2 position,
            Color color,
            float rotation,
            Vector2 origin,
            float scale,
            SpriteEffects effects,
            float layerDepth
        )
        {
            if (shouldDraw)
            {
                b.DrawString(
                    spriteFont,
                    text,
                    position,
                    color,
                    rotation,
                    origin,
                    scale,
                    effects,
                    layerDepth
                );
            }
        }
    }
}
