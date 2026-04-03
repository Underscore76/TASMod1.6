using System;
using HarmonyLib;
using Microsoft.Xna.Framework.Audio;
using StardewValley;
using StardewValley.Audio;
using StardewValley.BellsAndWhistles;
using TASMod.Extensions;
using TASMod.System;

namespace TASMod.Patches
{
    public class Game1_InitializeSound : IPatch
    {
        public static bool Override = false;
        public override string Name => "Game1.InitializeSounds";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(Game1), "InitializeSounds"),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix)),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
        }

        public static bool Prefix()
        {
            return !Override;
        }
        public static void Postfix()
        {
            if (!Override) return;
            var v = AccessTools.TypeByName("StardewValley.Audio.DummyAudioEngine");
            Game1.audioEngine = (IAudioEngine)Activator.CreateInstance(v);
            Game1.soundBank = new DummySoundBank();
            Game1.audioEngine.Update();
            Game1.musicCategory = Game1.audioEngine.GetCategory("Music");
            Game1.soundCategory = Game1.audioEngine.GetCategory("Sound");
            Game1.ambientCategory = Game1.audioEngine.GetCategory("Ambient");
            Game1.footstepCategory = Game1.audioEngine.GetCategory("Footsteps");
            Game1.wind = Game1.soundBank.GetCue("wind");
            Game1.chargeUpSound = Game1.soundBank.GetCue("toolCharge");
            AmbientLocationSounds.InitShared();
        }
    }

    public class AudioEngine_Constructor : IPatch
    {
        public static bool BreakAudioEngine = false;
        public override string Name => "AudioEngine.Constructor";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Constructor(
                    typeof(AudioEngine),
                    new Type[] { typeof(string) }
                ),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix)),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
        }

        public static bool Prefix()
        {
            if (BreakAudioEngine)
            {
                throw new Exception("dont want to work");
            }
            return true;
        }

        public static void Postfix(ref AudioEngine __instance)
        {
            // ensure that the audio engine uses TAS timing
            var stopwatch = new TASStopWatch();
            __instance.SetStopwatch(stopwatch);
            stopwatch.Start();

        }
    }

    public class AudioEngine_Update : IPatch
    {
        public override string Name => "AudioEngine.Update";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(AudioEngine), "Update"),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix))
            );
        }

        public static bool Prefix(ref AudioEngine __instance)
        {
            // update the TAS stopwatch
            TASStopWatch stopwatch = __instance.GetStopwatch() as TASStopWatch;
            if (stopwatch != null)
            {
                stopwatch.Advance(TASDateTime.CurrentGameTime.ElapsedGameTime);
            }
            return true;
        }
    }
}
