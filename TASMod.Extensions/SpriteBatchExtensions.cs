using Microsoft.Xna.Framework.Graphics;

namespace TASMod.Extensions
{
    internal static class SpriteBatchExtensions
    {
        public static bool inBeginEndPair(this SpriteBatch spriteBatch)
        {
            var field = ModEntry.Reflection.GetField<bool>(spriteBatch, "_beginCalled");
            return field.GetValue();
        }
    }
}
