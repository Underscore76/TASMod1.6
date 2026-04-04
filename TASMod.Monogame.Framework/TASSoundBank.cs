using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework.Audio;
using StardewValley;

namespace TASMod.Monogame.Framework.Audio
{
    public class TASSoundBank : ISoundBank
    {
        public static Dictionary<string, TASSoundBank> Banks = new();
        public static TASSoundBank Get(AudioEngine engine, string settingsFile)
        {
            if (Banks.TryGetValue(settingsFile, out var bank))
            {
                return bank;
            }
            var newBank = new TASSoundBank(engine, Path.Combine(GameRunner.instance.Content.RootDirectory, "XACT", settingsFile));
            Banks[settingsFile] = newBank;
            return newBank;
        }

        public SoundBank Bank { get; private set; }
        public TASSoundBank(AudioEngine engine, string settingsFile)
        {
            this.Bank = new SoundBank(engine, settingsFile);
        }

        public bool IsInUse => Bank.IsInUse;
        public bool IsDisposed => Bank.IsDisposed;
        public void Dispose() { }

        public ConditionalWeakTable<CueWrapper, object> CueTable = new();

        public ICue GetCue(string name)
        {
            var cue = new CueWrapper(Bank.GetCue(name));
            CueTable.AddOrUpdate(cue, null);
            return cue;
        }

        public void PlayCue(string name)
        {
            Bank.PlayCue(name);
        }

        public void PlayCue(string name, AudioListener listener, AudioEmitter emitter)
        {
            Bank.PlayCue(name, listener, emitter);
        }

        public void AddCue(CueDefinition definition)
        {
            Bank.AddCue(definition);
        }

        public bool Exists(string name)
        {
            return Bank.Exists(name);
        }

        public CueDefinition GetCueDefinition(string name)
        {
            return Bank.GetCueDefinition(name);
        }

        public void StopAllCues()
        {
            foreach (var kvp in CueTable)
            {
                var cue = kvp.Key;
                if (cue == null || Reflector.GetValue(cue, "cue") as Cue == null)
                    continue;
                if (cue.IsPlaying)
                    cue.Stop(AudioStopOptions.Immediate);
            }
        }
    }
}