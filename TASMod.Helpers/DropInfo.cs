using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;

namespace TASMod.Helpers
{
    public class DropInfo
    {
        public static string ObjectName(string index)
        {
            try
            {
                return Game1.objectData[index].Name;
            }
            catch (Exception e)
            {
                return "unknown";
            }
        }
    }
}
