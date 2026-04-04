using System.Diagnostics;
using Microsoft.Xna.Framework.Audio;

namespace TASMod.Extensions
{
    public static class AudioEngineExtensions
    {
        public static Stopwatch GetStopwatch(this AudioEngine engine)
        {
            return (Stopwatch)Reflector.GetValue(engine, "_stopwatch");
        }

        public static void SetStopwatch(this AudioEngine engine, Stopwatch stopwatch)
        {
            Reflector.SetValue(engine, "_stopwatch", stopwatch);
        }
    }
}
