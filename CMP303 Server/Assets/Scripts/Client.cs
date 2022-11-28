using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class Client
{
    public static int bufferSize = 4096;

    public int playerID;
    public Player player;
    public TCP tcp;
    public UDP udp;

    public Client(int clientID)
    {
        playerID = clientID;
        tcp = new TCP(playerID);
        udp = new UDP(playerID);
    }

    public class TCP
    {
        public TcpClient socket;
        private readonly int id;
        private NetworkStream stream;
        private Packet packet;
        private byte[] receiveBuffer;
        public TCP(int id)
        {
            this.id = id;
        }

        public void Connect(TcpClient socket)
        {
            this.socket = socket;
            this.socket.ReceiveBufferSize = bufferSize;
            this.socket.SendBufferSize = bufferSize;

            stream = socket.GetStream();
            packet = new Packet();
            receiveBuffer = new byte[bufferSize];

            stream.BeginRead(receiveBuffer, 0, bufferSize, ReceiveCallback, null);

            DataSend.Welcome(id, "Successfully Connected!");
        }

        public void SendData(Packet packet)
        {
            try
            {
                if (socket != null)
                {
                    stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"Couldn't send data to client ID {id}. Reason: {ex}");
            }
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                int bytelength = stream.EndRead(result);
                if (bytelength <= 0)
                {
                    Server.clientList[id].Disconnect();
                    return;
                }

                byte[] data = new byte[bytelength];
                Array.Copy(receiveBuffer, data, bytelength);

                packet.Reset(HandledData(data));
                stream.BeginRead(receiveBuffer, 0, bufferSize, ReceiveCallback, null);
            }
            catch (Exception ex)
            {
                Debug.Log($"Error receiving TCP data: {ex}");
                Debug.Log($"Disconnecting player: {Server.clientList[id].player.username}");
                Server.clientList[id].Disconnect();
            }
        }

        private bool HandledData(byte[] data)
        {
            int packetLength = 0;

            packet.SetBytes(data);

            if (packet.UnreadLength() >= 4)
            {
                packetLength = packet.ReadInt();
                if (packetLength <= 0) return true;
            }

            while (packetLength > 0 && packetLength <= packet.UnreadLength())
            {
                byte[] packetBytes = packet.ReadBytes(packetLength);
                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (Packet packet = new Packet(packetBytes))
                    {
                        int packetID = packet.ReadInt();
                        Server.packetHandler[packetID](id, packet);
                    }
                });

                packetLength = 0;

                if (packet.UnreadLength() >= 4)
                {
                    packetLength = packet.ReadInt();
                    if (packetLength <= 0) return true;
                }
            }

            if (packetLength <= 1) return true;

            return false;
        }

        public void Disconnect() 
        {
            socket.Close();
            stream = null;
            packet = null;
            receiveBuffer = null;
            socket = null;
        }
    }

    public class UDP
    {
        public IPEndPoint endPoint;
        private int id;


        public UDP(int id)
        {
            this.id = id;
        }

        public void Connect(IPEndPoint endPoint)
        {
            this.endPoint = endPoint;
        }

        public void SendData(Packet packet)
        {
            Server.SendUDPData(endPoint, packet);
        }

        public void HandleData(Packet packetData)
        {
            int packetLength = packetData.ReadInt();
            byte[] packetBytes = packetData.ReadBytes(packetLength);

            ThreadManager.ExecuteOnMainThread(() =>
            {
                using (Packet packet = new Packet(packetBytes))
                {
                    int packetID = packet.ReadInt();
                    Server.packetHandler[packetID](id, packet);
                }
            });
        }
        public void Disconnect()
        {
            endPoint = null;
        }
    }

    public void PlayerSetup(string playerName) 
    {
        player = NetworkManager.instance.InstantiatePlayer();
        player.InitPlayer(playerID, playerName);

        foreach (Client client in Server.clientList.Values)
        {
            if (client.player != null) 
            {
                if (client.playerID != playerID) DataSend.SpawnPlayers(playerID, client.player);
            }
        }

        foreach (Client client in Server.clientList.Values) 
        {
            if (client.player != null) DataSend.SpawnPlayers(client.playerID, player);
        }
    }

    public void Disconnect() 
    {
        Debug.Log($"{player.username} disconnected");

        UnityEngine.Object.Destroy(player.gameObject);
        player = null;
        tcp.Disconnect();
        udp.Disconnect();
    }
}
