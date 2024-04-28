using System;
using TASMod.Extensions;

namespace TASMod.System
{
	public class TASGuid
	{
        private static Random random => RandomExtensions.SharedRandom;

        public static Guid NewGuid()
        {
            // Every time a guid gets created (NetCollections adds, object sorting in some contexts), 
            // it's uncontrollable random as that call uses a system function for generating guids
            // best solution I can come up with is to generate off of the global shared random
            // This is reproducible, and doesn't impact normal RNG manip that a person might do.
            var bytes = new byte[16];
            random.NextBytes(bytes);
            Guid ret = new Guid(bytes);
            return ret;
        }
    }
}

