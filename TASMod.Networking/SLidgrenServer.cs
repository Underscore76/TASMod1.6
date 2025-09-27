using System;
using System.Collections.Generic;
using System.Net.Http;
using Lidgren.Network;
using StardewValley;
using StardewValley.Network;

namespace TASMod.Networking
{
    public class SLidgrenServer : HookableServer
    {
        public IGameServer gameServer;
        public HashSet<string> introductionsSent = new HashSet<string>();
        public Bimap<long, string> peers = new Bimap<long, string>();

        public override int connectionsCount
        {
            get
            {
                return NetworkState.NumConnections;
            }
        }

        public SLidgrenServer(IGameServer parent)
            : base(null)
        {
            gameServer = parent;
        }

        public override void initialize()
        {
            Log("SLidgrenServer.initialize: Starting LAN server");
        }

        public override void stopServer()
        {
            Log("SLidgrenServer.stopServer: Stopping LAN server");
            NetworkState.Shutdown();
            introductionsSent.Clear();
            peers.Clear();
        }

        public static void Log(string msg)
        {
            if (NetworkState.VerboseLogging)
                ModEntry.Console.Log(msg, StardewModdingAPI.LogLevel.Warn);
        }

        public override void receiveMessages()
        {
            SIncomingMessage inc;
            while ((inc = NetworkState.ReadServerMessage()) != null)
            {
                Log($"SLidgrenServer.receiveMessages: {inc.IncomingMessageType} {inc.connectionId} {inc.message.FarmerID}");
                switch (inc.IncomingMessageType)
                {
                    case NetIncomingMessageType.DiscoveryRequest:
                        sendVersionInfo(inc);
                        break;
                    case NetIncomingMessageType.ConnectionApproval:
                        if (Game1.options.ipConnectionsEnabled || gameServer.IsLocalMultiplayerInitiatedServer())
                        {
                            sendConnectApproval(inc);
                        }
                        break;
                    case NetIncomingMessageType.Data:
                        parseDataMessageFromClient(inc);
                        break;
                    case NetIncomingMessageType.DebugMessage:
                    case NetIncomingMessageType.WarningMessage:
                    case NetIncomingMessageType.ErrorMessage:
                        {
                            string message = inc.message.Reader?.ReadString();
                            Log("SLidgrenServer.debugOutput:" + inc.IncomingMessageType.ToString() + ": " + message);
                            Game1.debugOutput = message;
                            break;
                        }
                    case NetIncomingMessageType.StatusChanged:
                        statusChanged(inc);
                        break;
                    default:
                        Log("SLidgrenServer.debugOutput:FallThrough:" + inc.IncomingMessageType.ToString() + ": " + inc.ToString());
                        Game1.debugOutput = inc.ToString();
                        break;
                }
            }
            foreach (string conn in NetworkState.Connections)
            {
                if (!introductionsSent.Contains(conn))
                {
                    if (!gameServer.whenGameAvailable(delegate
                    {
                        gameServer.sendAvailableFarmhands("", conn, delegate (OutgoingMessage msg)
                        {
                            sendMessage(conn, msg);
                        });
                    }, () => Game1.gameMode != 6))
                    {
                        Log("SLidgrenServer.receiveMessages: Postponing introduction message");
                        sendMessage(conn, new OutgoingMessage(11, Game1.player, "Strings\\UI:Client_WaitForHostLoad"));
                    }
                    introductionsSent.Add(conn);
                }
            }
        }

        private void sendConnectApproval(SIncomingMessage inc)
        {
            NetworkState.SendConnectionApproval(inc.connectionId, new OutgoingMessage(132, null));
        }

        private void statusChanged(SIncomingMessage inc)
        {
            switch (inc.message.MessageType)
            {
                case 5:
                    onConnect(inc.connectionId);
                    break;
                case 6:
                case 7:
                    onDisconnect(inc.connectionId);
                    if (peers.ContainsRight(inc.connectionId))
                    {
                        playerDisconnected(peers.GetLeft(inc.connectionId));
                    }
                    break;
            }
        }

        private void sendVersionInfo(SIncomingMessage inc)
        {
            OutgoingMessage msg = new(0, 0, Multiplayer.protocolVersion);
            NetworkState.SendDiscoveryResponse(inc.connectionId, msg);
        }

        private void parseDataMessageFromClient(SIncomingMessage inc)
        {
            IncomingMessage message = inc.message;
            string peer = inc.connectionId;
            if (peers.ContainsLeft(message.FarmerID) && peers[message.FarmerID] == peer)
            {
                gameServer.processIncomingMessage(message);
            }
            else if (message.MessageType == 2)
            {
                NetFarmerRoot farmer = Game1.Multiplayer.readFarmer(message.Reader);
                gameServer.checkFarmhandRequest("", peer, farmer, delegate (OutgoingMessage msg)
                {
                    sendMessage(peer, msg);
                }, delegate
                {
                    // approve
                    Log($"SLidgrenServer.parseDataMessageFromClient: Approved connection for {farmer.Value.Name} {farmer.Value.UniqueMultiplayerID} {peer}");
                    peers[farmer.Value.UniqueMultiplayerID] = peer;
                });
            }
        }

        public override void sendMessage(long peerId, OutgoingMessage message)
        {
            if (peers.ContainsLeft(peerId))
            {
                sendMessage(peers[peerId], message);
            }
        }
        public void sendMessage(string connectionId, OutgoingMessage message)
        {
            NetworkState.SendServerMessage(connectionId, message);
        }

        public override void kick(long disconnectee)
        {
        }

        public override void setPrivacy(ServerPrivacy privacy)
        {
        }

        public override bool connected()
        {
            return NetworkState.Connected;
        }

        public override string getUserId(long farmerId)
        {
            if (!peers.ContainsLeft(farmerId))
            {
                return null;
            }
            return peers[farmerId];
        }

        public override bool hasUserId(string userId)
        {
            foreach (string rightValue in peers.RightValues)
            {
                if (rightValue == userId)
                {
                    return true;
                }
            }
            return false;
        }

        public override string getUserName(long farmerId)
        {
            return getUserId(farmerId);
        }

        public override void setLobbyData(string key, string value)
        {
        }

        public override bool isConnectionActive(string connectionId)
        {
            return peers.ContainsRight(connectionId);
        }

        public override void onConnect(string connectionId)
        {
            gameServer.onConnect(connectionId);
        }
        public override void onDisconnect(string connectionId)
        {
            gameServer.onDisconnect(connectionId);
        }

        public override void playerDisconnected(long disconnectee)
        {
            gameServer.playerDisconnected(disconnectee);
            introductionsSent.Remove(peers[disconnectee]);
            peers.RemoveLeft(disconnectee);
        }
    }
}