using System;
using System.Collections.Generic;
using System.IO;
using Lidgren.Network;
using StardewValley;
using StardewValley.Network;
using Netcode;

namespace TASMod.Networking
{
    public enum NetMessageType : byte
    {
        Unconnected = 0,
        LibraryError = 128,
        Ping = 129,
        Pong = 130,
        Connect = 131,
        ConnectResponse = 132,
        ConnectionEstablished = 133,
        Acknowledge = 134,
        Disconnect = 135,
        Discovery = 136,
        DiscoveryResponse = 137,
        NatPunchMessage = 138,
        NatIntroduction = 139,
        NatIntroductionConfirmRequest = 142,
        NatIntroductionConfirmed = 143,
        ExpandMTURequest = 140,
        ExpandMTUSuccess = 141
    }

    public class SOutgoingMessage
    {
        public NetIncomingMessageType MessageType;
        public string connectionId;

        public OutgoingMessage message;
    }

    public class SIncomingMessage : IDisposable
    {
        public NetIncomingMessageType IncomingMessageType;
        public string connectionId;

        public IncomingMessage message;
        public SIncomingMessage(NetIncomingMessageType inMessageType, string connId, byte messageType, long farmerID, byte[] data)
        {
            IncomingMessageType = inMessageType;
            connectionId = connId;
            message = new IncomingMessage();
            using BinaryReader reader = new BinaryReader(new MemoryStream(data));
            reader.ReadByte(); // message type
            reader.ReadInt64(); // farmerID
            byte[] msgData = reader.ReadSkippableBytes();
            Reflector.SetValue(message, "messageType", messageType);
            Reflector.SetValue(message, "farmerID", farmerID);
            Reflector.SetValue(message, "data", msgData);
            Reflector.SetValue(message, "stream", new MemoryStream((byte[])Reflector.GetValue(message, "data")));
            Reflector.SetValue(message, "reader", new BinaryReader((MemoryStream)Reflector.GetValue(message, "stream")));
        }

        public void Read(BinaryReader reader)
        {
            message.Read(reader);
        }

        public void Dispose()
        {
            message?.Dispose();
            message = null;
        }
    }

    public static class NetworkState
    {
        public static bool VerboseLogging = false;
        public static bool Connected = false;
        public static HashSet<string> Connections = new HashSet<string>();
        public static int ConnectionAttempts = 0;
        public static int NumConnections => Connections.Count;
        public static Dictionary<string, Queue<SIncomingMessage>> IncomingMessages = new();
        public const string ServerId = "SERVER";

        public static void Log(string msg)
        {
            if (VerboseLogging)
                ModEntry.Console.Log(msg, StardewModdingAPI.LogLevel.Error);
        }

        public static SIncomingMessage ReadServerMessage()
        {
            Log($"NetworkState.ReadServerMessage: Checking messages for {ServerId}");
            if (IncomingMessages.ContainsKey(ServerId) && IncomingMessages[ServerId].Count > 0)
                return IncomingMessages[ServerId].Dequeue();
            return null;
        }

        public static SIncomingMessage ReadClientMessage(string connectionId)
        {
            Log($"NetworkState.ReadClientMessage: Checking messages for {connectionId}");
            if (IncomingMessages.ContainsKey(connectionId) && IncomingMessages[connectionId].Count > 0)
                return IncomingMessages[connectionId].Dequeue();
            return null;
        }

        public static void Shutdown()
        {
            ConnectionAttempts = 0;
            Connected = false;
            Connections.Clear();
            foreach (var q in IncomingMessages.Values)
            {
                q.Clear();
            }
            IncomingMessages.Clear();
        }

        public static byte[] WriteMessage(OutgoingMessage message)
        {
            byte[] data;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
                {
                    message.Write(binaryWriter);
                    binaryWriter.Flush();
                    data = memoryStream.ToArray();
                }
            }
            return data;
        }

        public static void SendDiscoveryRequest(string connectionId, OutgoingMessage message)
        {
            // sends to server
            SIncomingMessage sim = new SIncomingMessage(
                inMessageType: NetIncomingMessageType.DiscoveryRequest,
                connectionId, message.MessageType, message.FarmerID, WriteMessage(message)
            );
            SendMessage(ServerId, sim);
        }
        public static void SendDiscoveryResponse(string connectionId, OutgoingMessage message)
        {
            // sent from server
            SIncomingMessage sim = new SIncomingMessage(
                inMessageType: NetIncomingMessageType.DiscoveryResponse,
                ServerId, message.MessageType, message.FarmerID, WriteMessage(message)
            );
            SendMessage(connectionId, sim);
        }
        public static void SendConnectionApproval(string connectionId, OutgoingMessage message)
        {
            // sent from server
            SIncomingMessage sim = new SIncomingMessage(
                NetIncomingMessageType.ConnectionApproval,
                ServerId, message.MessageType, message.FarmerID, WriteMessage(message)
            );
            SendMessage(connectionId, sim);
        }
        public static void SendServerMessage(string connectionId, OutgoingMessage message)
        {
            //sent from server
            SIncomingMessage sim = new SIncomingMessage(
                NetIncomingMessageType.Data,
                ServerId, message.MessageType, message.FarmerID, WriteMessage(message)
            );
            SendMessage(connectionId, sim);
        }
        public static void SendMessage(string connectionId, OutgoingMessage message)
        {
            // send to server
            SIncomingMessage sim = new SIncomingMessage(
                NetIncomingMessageType.Data,
                connectionId, message.MessageType, message.FarmerID, WriteMessage(message)
            );
            SendMessage(ServerId, sim);
        }

        public static void SendMessage(string connectionId, SIncomingMessage message)
        {
            if (!IncomingMessages.ContainsKey(connectionId))
            {
                IncomingMessages[connectionId] = new Queue<SIncomingMessage>();
            }
            IncomingMessages[connectionId].Enqueue(message);
        }

        public static void SendStatusChange(string connectionId, OutgoingMessage outgoingMessage)
        {
            SIncomingMessage sim = new SIncomingMessage(
                NetIncomingMessageType.StatusChanged,
                connectionId, outgoingMessage.MessageType, outgoingMessage.FarmerID, WriteMessage(outgoingMessage)
            );
            SendMessage(ServerId, sim);
        }

        public static void ConnectClient(string connectionId)
        {
            if (!Connected)
            {
                Connected = true;
            }
            Connections.Add(connectionId);
            Log($"NetworkState: Connected to server {connectionId}");
        }
        public static void DisconnectClient(string connectionId)
        {
            if (Connected)
            {
                Connections.Remove(connectionId);
                Log($"NetworkState: Disconnected from server {connectionId}");
            }
        }

        public static string GetNewConnectionId()
        {
            return $"Conn{ConnectionAttempts++}";
        }
    }
}