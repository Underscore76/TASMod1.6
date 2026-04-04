using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Audio;
using StardewValley;

namespace TASMod.Monogame.Framework.Audio
{
    public class TASWaveBank : WaveBank
    {
        public static Dictionary<string, TASWaveBank> Banks = new();
        public static TASWaveBank Get(AudioEngine engine, string settingsFile)
        {
            if (Banks.TryGetValue(settingsFile, out var bank))
            {
                return bank;
            }
            var newBank = new TASWaveBank(engine, Path.Combine(GameRunner.instance.Content.RootDirectory, "XACT", settingsFile));
            Banks[settingsFile] = newBank;
            return newBank;
        }

        public TASWaveBank(AudioEngine engine, string settingsFile)
            : base(engine, settingsFile)
        {
        }
    }
}