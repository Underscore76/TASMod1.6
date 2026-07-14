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
        public static string ObjectName(string index, bool return_unknown = false)
        {
            try
            {
                return Game1.objectData[index].Name;
            }
            catch (Exception)
            {
                if (return_unknown)
                {
                    return "unknown";
                }
                else
                {
                    return index;
                }
            }
        }
    }
}
