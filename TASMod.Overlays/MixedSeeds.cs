using System;
using ImGuiNET;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Extensions;
using TASMod.Extensions;
using TASMod.Helpers;
using TASMod.System;

namespace TASMod.Overlays
{
    public class MixedSeed : IOverlay
    {
        public override string Name => "MixedSeed";

        public override string Description => "determine next crop if planting a mixed seed";

        public ulong LastFrame = 0;
        public string objectName = "";

        public static string GetRandomLowGradeCropForThisSeason()
        {
            Random random = Game1.random.Copy();
            random.NextDouble();
            Season season = Game1.GetSeasonForLocation(Game1.currentLocation);
            if (season == Season.Winter)
            {
                season = random.Choose(Season.Spring, Season.Summer, Season.Fall);
            }
            string res = season switch
            {
                //472
                Season.Spring => random.Next(472,476).ToString(),
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
            if (res == "473") {
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
            if (TASDateTime.CurrentFrame != LastFrame)
            {
                objectName = GetRandomLowGradeCropForThisSeason();    
                LastFrame = TASDateTime.CurrentFrame;
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