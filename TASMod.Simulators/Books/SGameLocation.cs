using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using TASMod.System;
using BF = StardewValley.BellsAndWhistles.Butterfly;

namespace TASMod.Simulators.Books
{
    public class SGameLocation
    {
        public List<SCritter> critters;
        public List<SDebris> debris;
        public List<SObject> objects;

        public int DisplayHeight;
        public int DisplayWidth;

        public SGameLocation()
        {
            //
        }

        public void UpdateWhenCurrentLocation()
        {
            critters?.RemoveAll((SCritter critter) => critter.update(this));
            debris?.RemoveAll((SDebris d) => d.updateChunks(this));
            foreach (var obj in objects)
            {
                obj.updateWhenCurrentLocation();
            }
        }
    }
}
