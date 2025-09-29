using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.GameData;

namespace TASMod.Extensions
{
    public static class AudioEngineExtensions
    {
        public static void Clear(this AudioCategory category)
        {
            List<Cue> cues = (List<Cue>)Reflector.GetValue(category, "_sounds");
            for (int i = 0; i < cues.Count; i++)
            {
                cues[i].Stop(AudioStopOptions.Immediate);
            }
        }

        public static void AmbientLocationSoundsClear()
        {
            Reflector.GetStaticValue<AmbientLocationSounds, Dictionary<Vector2, int>>("sounds")?.Clear();
            Reflector.GetStaticValue<AmbientLocationSounds, ICue>("cricket")?.Stop(AudioStopOptions.Immediate);
            Reflector.GetStaticValue<AmbientLocationSounds, ICue>("engine")?.Stop(AudioStopOptions.Immediate);
            Reflector.GetStaticValue<AmbientLocationSounds, ICue>("waterfall")?.Stop(AudioStopOptions.Immediate);
            Reflector.GetStaticValue<AmbientLocationSounds, ICue>("waterfallBig")?.Stop(AudioStopOptions.Immediate);
            Reflector.GetStaticValue<AmbientLocationSounds, ICue>("babblingBrook")?.Stop(AudioStopOptions.Immediate);
            Reflector.GetStaticValue<AmbientLocationSounds, ICue>("cracklingFire")?.Stop(AudioStopOptions.Immediate);
        }

        public static void Reset(this AudioEngine engine)
        {
            if (Game1.currentSong != null)
            {
                Game1.currentSong.Stop(AudioStopOptions.Immediate);
            }
            engine.GetStopwatch().Reset();

            var dict =
                (Dictionary<MusicContext, KeyValuePair<string, bool>>)
                    Reflector.GetValue(Game1.game1, "_instanceRequestedMusicTracks");
            dict.Clear();

            AmbientLocationSoundsClear();
            Utility.killAllStaticLoopingSoundCues();

            // Reflector.GetValue(engine, "_activeCues");

            // {
            ModEntry.Console.Log("Clearing music category", StardewModdingAPI.LogLevel.Warn);
            AudioCategory musicCategory = (AudioCategory)Reflector.GetValue(Game1.musicCategory, "audioCategory");
            musicCategory.Clear();

            //     ModEntry.Console.Log("Clearing sound category", StardewModdingAPI.LogLevel.Warn);
            //     AudioCategory soundCategory = (AudioCategory)Reflector.GetValue(Game1.soundCategory, "audioCategory");
            //     soundCategory.Clear();

            //     ModEntry.Console.Log("Clearing ambient category", StardewModdingAPI.LogLevel.Warn);
            //     AudioCategory ambientCategory = (AudioCategory)Reflector.GetValue(Game1.ambientCategory, "audioCategory");
            //     ambientCategory.Clear();

            //     ModEntry.Console.Log("Clearing footstep category", StardewModdingAPI.LogLevel.Warn);
            //     AudioCategory footstepCategory = (AudioCategory)Reflector.GetValue(Game1.footstepCategory, "audioCategory");
            //     footstepCategory.Clear();
            // }
            engine.Update();
        }

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
