using System;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using StardewValley;
using TASMod.GameData;

namespace TASMod.Recording
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SaveState
    {
        [JsonProperty]
        public string Prefix = "tmp";

        [JsonProperty, JsonConverter(typeof(StringEnumConverter))]
        public LocalizedContentManager.LanguageCode Language = LocalizedContentManager
            .LanguageCode
            .en;

        [JsonProperty]
        public int Seed = 0;

        [JsonProperty]
        public StateList FrameStates = new StateList();

        [JsonProperty]
        public ulong ReRecords = 0;

        [JsonProperty]
        public int XActSeed = 0;

        // public GameState LastSave;

        public SaveState()
        {
            StoreGameDetails();
        }

        public SaveState(int seed, LocalizedContentManager.LanguageCode lang)
        {
            LocalizedContentManager.CurrentLanguageCode = lang;
            StoreGameDetails();
            Seed = seed;
            Prefix = string.Format("tmp_{0}", seed);
        }

        public SaveState(StateList states)
            : base()
        {
            FrameStates.AddRange(states);
        }

        public override string ToString()
        {
            return string.Format("Prefix:{0}|#Frames:{1}", Prefix, Count);
        }

        public string FilePath
        {
            get { return Path.Combine(Constants.SaveStatePath, Prefix + ".json"); }
        }

        [JsonProperty]
        public int Count
        {
            get { return FrameStates.Count; }
        }

        public void StoreGameDetails()
        {
            Language = LocalizedContentManager.CurrentLanguageCode;
            //XActSeed = AudioEngineWrapper.XactSeed;
        }

        public void RestoreGameDetails()
        {
            LocalizedContentManager.CurrentLanguageCode = Language;
            //AudioEngineWrapper.XactSeed = XActSeed;
        }

        public static string PathFromPrefix(string prefix)
        {
            return Path.Combine(Constants.SaveStatePath, prefix + ".json");
        }

        public void Save()
        {
            ModEntry.Console.Log($"Called Save {FilePath}", StardewModdingAPI.LogLevel.Alert);
            StoreGameDetails();
            using (StreamWriter file = File.CreateText(FilePath))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                serializer.Serialize(file, this);
            }
        }

        public static SaveState Load(string prefix = "tmp", bool restore = true)
        {
            string filePath = SaveState.PathFromPrefix(prefix);
            SaveState state = null;
            if (File.Exists(filePath))
            {
                ModEntry.Console.Log(
                    $"Called load on {filePath}",
                    StardewModdingAPI.LogLevel.Alert
                );
                try
                {
                    using (StreamReader file = File.OpenText(filePath))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        state = (SaveState)serializer.Deserialize(file, typeof(SaveState));
                        Debug.WriteLine(state.ToString());
                    }
                }
                catch (Exception e)
                {
                    ModEntry.Console.Log(
                        $"Failed to load file {e}",
                        StardewModdingAPI.LogLevel.Alert
                    );
                    return state;
                }
                if (restore)
                {
                    state.RestoreGameDetails();
                }
                ModEntry.Console.Log($"loaded {state.Count}", StardewModdingAPI.LogLevel.Alert);
            }
            return state;
        }

        public static void ChangeSaveStatePrefix(string filePath, string newPrefix)
        {
            SaveState state = null;
            if (File.Exists(filePath))
            {
                using (StreamReader file = File.OpenText(filePath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    // TODO: any safety rails for overwriting current State?
                    state = (SaveState)serializer.Deserialize(file, typeof(SaveState));
                }
                state.Prefix = newPrefix;
                state.Save();
            }
        }

        public void Reset(int resetTo)
        {
            if (resetTo < 0)
                resetTo = FrameStates.Count + 1 + resetTo;
            resetTo = Math.Min(resetTo, FrameStates.Count);

            while (Count > resetTo)
                FrameStates.Pop();
        }
    }
}
