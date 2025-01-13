using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using NLua;
using NLua.Exceptions;
using StardewValley;
using TASMod.Console;
using TASMod.Extensions;
using TASMod.Helpers;
using TASMod.Inputs;
using TASMod.Minigames;
using TASMod.Recording;
using TASMod.System;

namespace TASMod.Scripting
{
    public class Solvers
    {
        public static Solvers _instance = null;
        public static TASConsole Console => Controller.Console;

        public Solvers()
        {
            _instance = this;
        }
    }
}
