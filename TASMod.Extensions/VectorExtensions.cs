using Num = System.Numerics;
using Microsoft.Xna.Framework;

namespace TASMod.Extensions
{
    public static class VectorExtensions
    {
        public static Num.Vector4 ToNumerics(this Vector4 vec)
        {
            return new Num.Vector4(vec.X, vec.Y, vec.Z, vec.W);
        }
    }

    public static class NumericExtensions
    {
        public static Vector4 ToXna(this Num.Vector4 vec)
        {
            return new Vector4(vec.X, vec.Y, vec.Z, vec.W);
        }

        public static Color ToColor(this Num.Vector4 vec)
        {
            return new Color(vec.X, vec.Y, vec.Z, vec.W);
        }
    }
}