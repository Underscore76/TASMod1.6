using System;
using ImGuiNET;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Extensions;
using TASMod.Extensions;
using TASMod.Helpers;
using TASMod.Networking;
using TASMod.System;

namespace TASMod.Overlays
{
    public class MixedSeed : IOverlay
    {
        public override string Name => "MixedSeed";

        public override string Description => "determine next crop if planting a mixed seed";

        public int LastInstanceIndex = -1;
        public ulong LastFrame = 0;
        public string objectName = "";

        public static string GetRandomLowGradeCropForThisSeason(Random random)
        {
            Season season = Game1.GetSeasonForLocation(Game1.currentLocation);
            if (season == Season.Winter)
            {
                season = random.Choose(Season.Spring, Season.Summer, Season.Fall);
            }
            string res = season switch
            {
                //472
                Season.Spring => random.Next(472, 476).ToString(),
                Season.Summer => random.Next(4) switch
                {
                    0 => "487",
                    1 => "483",
                    2 => "482",
                    _ => "484",
                },
                Season.Fall => random.Next(487, 491).ToString(),
                _ => null
            };
            if (res == "473")
            {
                res = "472";
            }
            if (res == null)
            {
                return "null";
            }
            return DropInfo.ObjectName(res);
        }

        public override void ActiveUpdate()
        {
            if (ActiveInstance.InstanceIndex < 0 || ActiveInstance.InstanceIndex >= GameRunner.instance.gameInstances.Count)
            {
                return;
            }
            if (TASDateTime.CurrentFrame != LastFrame || ActiveInstance.InstanceIndex != LastInstanceIndex)
            {
                Random r = InstanceData.Get(ActiveInstance.InstanceIndex).random.Copy();
                GetRandomLowGradeCropForThisSeason(r);
                if (ActiveInstance.InstanceIndex == 0)
                {
                    objectName = GetRandomLowGradeCropForThisSeason(r);

                }
                else
                {
                    GetRandomLowGradeCropForThisSeason(r);
                    objectName = GetRandomLowGradeCropForThisSeason(r);

                }
                LastFrame = TASDateTime.CurrentFrame;
                LastInstanceIndex = ActiveInstance.InstanceIndex;
            }
        }

        public override void RenderImGui()
        {
            if (ImGui.CollapsingHeader("MixedSeed"))
            {
                ImGui.Text("Next crop: " + objectName);
            }
        }
    }
}