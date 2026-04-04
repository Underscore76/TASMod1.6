
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using StardewValley.Audio;
using TASMod.Extensions;

namespace TASMod.Monogame.Framework.Audio
{
    public class TASAudioCategory : IAudioCategory
    {
        public static Dictionary<string, TASAudioCategory> Categories = new();
        public static TASAudioCategory Get(TASAudioEngine engine, string name)
        {
            if (Categories.TryGetValue(name, out var cat))
            {
                return cat;
            }
            var newCat = new TASAudioCategory(engine.Engine.GetCategory(name));
            Categories[name] = newCat;
            return newCat;
        }

        public string Name => Category.Name;
        public AudioCategory Category { get; private set; }
        public TASAudioCategory(AudioCategory category)
        {
            Category = category;
        }

        public void SetVolume(float volume)
        {
            Category.SetVolume(volume);
        }

        public void Stop()
        {
            List<Cue> cues = (List<Cue>)Reflector.GetValue(Category, "_sounds");
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
    }
}