
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Audio;
using StardewValley;
using StardewValley.Audio;
using StardewValley.GameData;
using TASMod.Extensions;

namespace TASMod.Monogame.Framework.Audio
{
    public class TASAudioEngine : IAudioEngine, IDisposable
    {
        public static TASAudioEngine Instance { get; private set; }
        public static bool Initialized { get; private set; } = false;
        public static TASAudioEngine Get()
        {
            if (!Initialized)
            {
                Instance = new TASAudioEngine(Path.Combine(GameRunner.instance.Content.RootDirectory, "XACT", "FarmerSounds.xgs"));
                Initialized = true;
            }
            return Instance;
        }

        private AudioEngine audioEngine;
        public TASSoundBank SoundBank;
        public AudioEngine Engine => audioEngine;
        public Dictionary<string, int> CategoryLookup = new();
        public List<TASAudioCategory> Categories = new();

        public bool IsDisposed => this.audioEngine.IsDisposed;
        public void Dispose() { }

        public TASAudioEngine(string path)
        {
            this.audioEngine = new AudioEngine(path);
            this.audioEngine.GetReverbSettings()[18] = 4f;
            this.audioEngine.GetReverbSettings()[17] = -12f;
        }


        public IAudioCategory GetCategory(string name)
        {
            if (CategoryLookup.TryGetValue(name, out var index))
            {
                return Categories[index];
            }

            var newCategory = TASAudioCategory.Get(this, name);
            CategoryLookup[name] = Categories.Count;
            Categories.Add(newCategory);
            return newCategory;

        }

        public int GetCategoryIndex(string name)
        {
            for (int i = 0; i < Categories.Count; i++)
            {
                if (Categories[i].Name == name)
                {
                    return i;
                }
            }
            return -1;
        }

        public void Update()
        {
            Engine.Update();
        }

        public void Reset()
        {
            SoundBank.StopAllCues();
            for (int i = 0; i < Categories.Count; i++)
            {
                var cat = Categories[i];
                if (cat == null) continue;
                cat.Stop();
            }
            Engine.Update();
            Engine.GetStopwatch().Reset();
        }
    }
}