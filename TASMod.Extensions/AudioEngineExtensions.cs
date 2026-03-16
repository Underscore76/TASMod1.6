using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.GameData;
using TASMod.Patches;

namespace TASMod.Extensions
{
    public static class AudioEngineExtensions
    {
        public static void Clear(this AudioCategory category)
        {
            List<Cue> cues = (List<Cue>)Reflector.GetValue(category, "_sounds");
            if (cues == null || cues.Count == 0)
            {
                return;
            }

            List<Cue> snapshot = new List<Cue>(cues);
            for (int i = 0; i < snapshot.Count; i++)
            {
                snapshot[i]?.Stop(AudioStopOptions.Immediate);
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
            PauseOpenALManager(() =>
            {
                if (Game1.currentSong != null)
                {
                    Game1.currentSong.Stop(AudioStopOptions.Immediate);
                }
                engine.GetStopwatch().Reset();

                var dict =
                    (Dictionary<MusicContext, KeyValuePair<string, bool>>)
                        Reflector.GetValue(Game1.game1, "_instanceRequestedMusicTracks");
                dict?.Clear();

                AmbientLocationSoundsClear();
                Utility.killAllStaticLoopingSoundCues();

                ModEntry.Console.Log("Clearing music category", StardewModdingAPI.LogLevel.Warn);
                AudioCategory musicCategory = (AudioCategory)
                    Reflector.GetValue(Game1.musicCategory, "audioCategory");
                musicCategory?.Clear();

                engine.Update();
            });
        }

        private static void PauseOpenALManager(global::System.Action action)
        {
            if (action == null)
            {
                return;
            }

            global::System.Type managerType = typeof(AudioEngine).Assembly.GetType(
                "Microsoft.Xna.Framework.Audio.OpenALSoundEffectInstanceManager"
            );
            if (managerType == null)
            {
                action();
                return;
            }

            FieldInfo pausedField = managerType.GetField(
                "paused",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public
            );
            FieldInfo pauseMutexField = managerType.GetField(
                "pauseMutex",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public
            );

            if (pausedField == null || pauseMutexField == null)
            {
                action();
                return;
            }

            object pauseMutex = pauseMutexField.GetValue(null);
            if (pauseMutex == null)
            {
                action();
                return;
            }

            lock (pauseMutex)
            {
                bool wasPaused = (bool)pausedField.GetValue(null);
                pausedField.SetValue(null, true);
                try
                {
                    action();
                }
                finally
                {
                    pausedField.SetValue(null, wasPaused);
                }
            }
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
