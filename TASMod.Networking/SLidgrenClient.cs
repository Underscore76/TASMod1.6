using System;
using System.IO;
using Lidgren.Network;
using StardewValley;
using StardewValley.Network;

namespace TASMod.Networking
{
    public class SLidgrenClient : HookableClient
    {
        public bool serverDiscovered;
        public float lastLatencyMs;

        public long farmerID => Game1.player.UniqueMultiplayerID;
        public string connectionId;

        public SLidgrenClient()
        {
            connectionId = NetworkState.GetNewConnectionId();
        }

        public override void disconnect(bool neatly = true)
        {
            NetworkState.SendStatusChange(connectionId, new OutgoingMessage(6, Game1.player));
        }

        public override float GetPingToHost()
        {
            return lastLatencyMs / 2f;
        }

        public override string getUserID()
        {
            return farmerID.ToString();
        }

        public override void sendMessage(OutgoingMessage message)
        {
            NetworkState.SendMessage(connectionId, message);
        }

        protected override void connectImpl()
        {
            NetworkState.SendDiscoveryRequest(connectionId, new OutgoingMessage(0, Game1.player));
        }

        protected override string getHostUserName()
        {
            return "";
        }
        public static void Log(string msg)
        {
            if (NetworkState.VerboseLogging)
                ModEntry.Console.Log(msg, StardewModdingAPI.LogLevel.Alert);
        }

        protected override void receiveMessagesImpl()
        {
            SIncomingMessage inc;
            while ((inc = NetworkState.ReadClientMessage(connectionId)) != null)
            {
                Log($"SLidgrenClient.receiveMessagesImpl: {inc.IncomingMessageType} {inc.connectionId}");
                switch (inc.IncomingMessageType)
                {
                    case NetIncomingMessageType.ConnectionLatencyUpdated:
                        break;
                    case NetIncomingMessageType.DiscoveryResponse:
                        if (!serverDiscovered)
                        {
                            Log($"SLidgrenClient.receiveMessagesImpl: Found server at localhost {connectionId}");
                            receiveHandshake(inc);
                            serverDiscovered = true;
                        }
                        break;
                    case NetIncomingMessageType.Data:
                        parseDataMessageFromServer(inc);
                        break;
                    case NetIncomingMessageType.DebugMessage:
                    case NetIncomingMessageType.WarningMessage:
                    case NetIncomingMessageType.ErrorMessage:
                        {
                            string message = inc.message.Reader?.ReadString();
                            Log("SLidgrenClient.debugOutput: " + inc.IncomingMessageType.ToString() + ": " + message);
                            Game1.debugOutput = message;
                            break;
                        }
                    case NetIncomingMessageType.StatusChanged:
                        // should never get sent by the server side
                        break;
                }
            }
        }

        private void parseDataMessageFromServer(SIncomingMessage inc)
        {
            try
            {
                Log($"SLidgrenClient.parseDataMessageFromServer: {inc.IncomingMessageType} {inc.message.MessageType} {inc.message.Reader}");
                // if (inc.message.MessageType == 9)
                // {
                //     BinaryReader msg = inc.message.Reader;
                //     int value = msg.ReadInt32();
                //     int value2 = msg.ReadInt32();
                //     int value3 = msg.ReadInt32();
                //     int num = msg.ReadByte();
                //     msg.BaseStream.Seek(0, SeekOrigin.Begin);
                //     Log($"SLidgrenClient.parseDataMessageFromServer[{msg.BaseStream.Length}]: loading {value} {value2} {value3} {num}");
                // }
                processIncomingMessage(inc.message);
            }
            finally
            {
                if (inc != null)
                {
                    inc.Dispose();
                }
            }
        }

        private void receiveHandshake(SIncomingMessage msg)
        {
            NetworkState.ConnectClient(connectionId);
        }

    }
}