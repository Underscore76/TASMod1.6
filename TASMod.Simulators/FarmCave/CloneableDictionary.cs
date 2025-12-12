using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Reflection;
using StardewValley;
using StardewValley.Extensions;
using Microsoft.Xna.Framework;
using StardewValley.TerrainFeatures;

namespace TASMod.Extensions
{
    public static class DictionaryCloneHelper
    {
        // Define the BindingFlags needed to access non-public instance fields
        private const BindingFlags FieldFlags = BindingFlags.NonPublic | BindingFlags.Instance;
        // private int[] _buckets;
        // private Entry[] _entries;
        // private ulong _fastModMultiplier;
        // private int _count;
        // private int _freeList;
        // private int _freeCount;
        // private int _version;

        public static Dictionary<TKey, TValue> CloneWithReflection<TKey, TValue>(Dictionary<TKey, TValue> original)
        {
            // Get the Type of the Dictionary
            Type dictType = typeof(Dictionary<TKey, TValue>);

            // Get the FieldInfo for the private fields. Names are implementation-specific.
            FieldInfo bucketsField = dictType.GetField("_buckets", FieldFlags);
            FieldInfo entriesField = dictType.GetField("_entries", FieldFlags);
            FieldInfo fastModMultiplierField = dictType.GetField("_fastModMultiplier", FieldFlags);
            FieldInfo countField = dictType.GetField("_count", FieldFlags);
            FieldInfo freeListField = dictType.GetField("_freeList", FieldFlags);
            FieldInfo freeCountField = dictType.GetField("_freeCount", FieldFlags);
            FieldInfo versionField = dictType.GetField("_version", FieldFlags);

            if (entriesField == null || bucketsField == null)
            {
                throw new InvalidOperationException("Could not find expected Dictionary fields. The implementation may have changed.");
            }

            // Get the values from the original dictionary
            var entries = entriesField.GetValue(original);
            var buckets = bucketsField.GetValue(original);
            var fastModMultiplier = fastModMultiplierField.GetValue(original);
            var count = (int)countField.GetValue(original);
            var freeList = (int)freeListField.GetValue(original);
            var freeCount = (int)freeCountField.GetValue(original);
            var version = (int)versionField.GetValue(original);

            // --- Cloning the underlying data ---

            // The 'entries' field is an array of a private struct type 'Entry'.
            // We cannot simply cast it, but we can copy the array contents.
            // Array.Clone() performs a shallow copy of the array elements.
            Array clonedEntries = (Array)((Array)entries).Clone();

            // 'buckets' is typically an int[]
            int[] clonedBuckets = (int[])((int[])buckets).Clone();

            // --- Creating the new Dictionary instance ---

            // The challenge is creating a Dictionary instance without calling a constructor 
            // that might reset the internal fields we just cloned. 
            // This is complex and usually requires using FormatterServices.GetUninitializedObject 
            // which might have security or compatibility issues. 
            // A simpler, though slightly less direct, approach is to use the copy constructor 
            // for a shallow copy, but then *overwrite* its fields with our cloned ones.

            var newDict = new Dictionary<TKey, TValue>(original.Count, original.Comparer);

            // Overwrite the newly initialized dictionary's internal state with the cloned data
            entriesField.SetValue(newDict, clonedEntries);
            bucketsField.SetValue(newDict, clonedBuckets);
            fastModMultiplierField.SetValue(newDict, fastModMultiplier);
            countField.SetValue(newDict, count);
            freeListField.SetValue(newDict, freeList);
            freeCountField.SetValue(newDict, freeCount);
            versionField.SetValue(newDict, version);
            return newDict;
        }

        public static StardewValley.Object CreateObjectWithRandom(Random random, string itemId, int initialStack, bool isRecipe = false, int price = -1, int quality = 0)
        {
            if (itemId.StartsWith("(O)"))
            {
                itemId = itemId.Substring(3);
            }
            StardewValley.Object obj = new StardewValley.Object();
            obj.ItemId = itemId;
            obj.Stack = initialStack;
            obj.IsRecipe = isRecipe;
            obj.ResetParentSheetIndex();
            if (Game1.objectData.TryGetValue(itemId, out var data))
            {
                obj.Name = data.Name ?? ItemRegistry.GetDataOrErrorItem(obj.QualifiedItemId).InternalName;
                obj.Price = data.Price;
                obj.Edibility = data.Edibility;
                obj.Type = data.Type;
                obj.Category = data.Category;
            }
            if (price != -1)
            {
                obj.Price = price;
            }
            obj.CanBeSetDown = true;
            obj.CanBeGrabbed = true;
            obj.IsSpawnedObject = false;
            if (random.NextBool() && Utility.IsLegacyIdAbove(itemId, 52) && !Utility.IsLegacyIdBetween(itemId, 8, 15) && !Utility.IsLegacyIdBetween(itemId, 384, 391))
            {
                obj.Flipped = true;
            }
            if (obj.QualifiedItemId == "(O)463" || obj.QualifiedItemId == "(O)464")
            {
                obj.scale = new Vector2(1f, 1f);
            }
            if (itemId == "449" || obj.IsWeeds() || obj.IsTwig())
            {
                obj.Fragility = 2;
            }
            else if (obj.name.Contains("Fence"))
            {
                obj.scale = new Vector2(10f, 0f);
            }
            else if (obj.IsBreakableStone())
            {
                switch (itemId)
                {
                    case "8":
                        obj.MinutesUntilReady = 4;
                        break;
                    case "10":
                        obj.MinutesUntilReady = 8;
                        break;
                    case "12":
                        obj.MinutesUntilReady = 16;
                        break;
                    case "14":
                        obj.MinutesUntilReady = 12;
                        break;
                    case "25":
                        obj.MinutesUntilReady = 8;
                        break;
                    default:
                        obj.MinutesUntilReady = 1;
                        break;
                }
            }
            if (obj.Category == -22)
            {
                obj.scale.Y = 1f;
            }
            return obj;
        }

        /*
        public Tree()
                : base(needsTick: true)
            {
                resetTexture();
            }

            public Tree(string id, int growthStage, bool isGreenRainTemporaryTree = false)
                : this()
            {
                this.growthStage.Value = growthStage;
                isTemporaryGreenRainTree.Value = isGreenRainTemporaryTree;
                treeType.Value = id;
                if (treeType.Value == "4")
                {
                    treeType.Value = "1";
                }

                if (treeType.Value == "5")
                {
                    treeType.Value = "2";
                }

                flipped.Value = Game1.random.NextBool();
                health.Value = 10f;
            }
        */
        public static Tree CreateTreeWithRandom(Random random, string id, int growthStage)
        {
            var tree = new Tree();
            tree.growthStage.Value = 0;
            tree.treeType.Value = id;
            if (tree.treeType.Value == "4")
            {
                tree.treeType.Value = "1";
            }

            if (tree.treeType.Value == "5")
            {
                tree.treeType.Value = "2";
            }
            tree.flipped.Value = random.NextBool();
            tree.health.Value = 10f;
            return tree;
        }
    }
}