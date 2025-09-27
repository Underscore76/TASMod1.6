using HarmonyLib;
using StardewValley;
using StardewValley.Network;
using Lidgren.Network;
using System;
using StardewValley.Menus;
using TASMod.Networking;

namespace TASMod.Patches
{
    public class Multiplayer_StartLocalMultiplayerServer : IPatch
    {
        public override string Name => "Multiplayer.StartLocalMultiplayerServer";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(
                    typeof(Multiplayer), "StartLocalMultiplayerServer"
                ),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix)),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
        }

        public static bool Prefix()
        {
            Warn("Multiplayer.StartLocalMultiplayerServer: No SHOT this doesn't get inlined");
            return false;
        }

        public static void Postfix()
        {
            Warn("Multiplayer.StartLocalMultiplayerServer:Postfix: Starting local multiplayer server");
            Game1.server = new SGameServer(true);
            Game1.server.startServer();
        }
    }
    public class FarmhandMenu_Constructor : IPatch
    {
        public override string Name => "FarmhandMenu.Constructor";

        public override void Patch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Constructor(
                    typeof(FarmhandMenu), new Type[] { typeof(Client) }
                ),
                prefix: new HarmonyMethod(this.GetType(), nameof(this.Prefix)),
                postfix: new HarmonyMethod(this.GetType(), nameof(this.Postfix))
            );
        }

        public static bool Prefix(ref Client client)
        {
            Warn($"FarmhandMenu.Constructor: No SHOT this doesn't get inlined {client}");
            client = new SLidgrenClient();
            return true;
        }

        public static void Postfix(ref Client client)
        {
            Warn($"FarmhandMenu.Constructor:Postfix: Created SLidgrenClient {client}");
        }
    }
}