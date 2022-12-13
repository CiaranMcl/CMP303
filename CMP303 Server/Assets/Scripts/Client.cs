using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class Client
{
    // Networking variables
    public static int bufferSize = 4096;
    public TCP tcp;
    public UDP udp;

    // Player variables
    private int playerID;
    public Player player;

    public Client(int clientID)
    {
        // Initialising client 
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
            // Initialises connection variables
            this.socket = socket;
            this.socket.ReceiveBufferSize = bufferSize;
            this.socket.SendBufferSize = bufferSize;

            // Creating packet and buffer variables
            stream = socket.GetStream();
            packet = new Packet();
            receiveBuffer = new byte[bufferSize];

            stream.BeginRead(receiveBuffer, 0, bufferSize, ReceiveCallback, null);

            // Once connection is made, sends welcome packet
            DataSend.Welcome(id, "Successfully Connected!");
        }

        public void SendData(Packet packet)
        {
            try
            {
                // If the socket exists, start sending the packet
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

                // Disconnect the client if the packet is unreadable
                if (bytelength <= 0)
                {
                    Server.clientList[id].Disconnect();
                    return;
                }

                // Create data from packet
                byte[] data = new byte[bytelength];
                Array.Copy(receiveBuffer, data, bytelength);

                // If data is correctly processed, packet is reset and data is read
                packet.Reset(HandledData(data));
                stream.BeginRead(receiveBuffer, 0, bufferSize, ReceiveCallback, null);
            }
            catch (Exception ex)
            {
                // Upon TCP error, disconnect the client
                Debug.Log($"Error receiving TCP data: {ex}");
                Debug.Log($"Disconnecting player: {Server.clientList[id].player.username}");
                Server.clientList[id].Disconnect();
            }
        }

        private bool HandledData(byte[] data)
        {
            int packetLength = 0;

            packet.SetBytes(data);

            // Check if packet is readable
            if (packet.UnreadLength() >= 4)
            {
                packetLength = packet.ReadInt();
                // If packet processed correctly, return true
                if (packetLength <= 0) return true;
            }

            // Check if packet is complete
            while (packetLength > 0 && packetLength <= packet.UnreadLength())
            {
                // Process packet on main thread using packet data
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

                // Check if packet is readable
                if (packet.UnreadLength() >= 4)
                {
                    packetLength = packet.ReadInt();
                    // If packet processed correctly, return true
                    if (packetLength <= 0) return true;
                }
            }

            // If packet processed correctly, return true
            if (packetLength <= 1) return true;

            return false;
        }

        public void Disconnect()
        {
            // Disconnect TCP socket
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
            // Initialise UDP connection
            this.endPoint = endPoint;
        }

        public void SendData(Packet packet)
        {
            // Send data packet 
            Server.SendUDPData(endPoint, packet);
        }

        public void HandleData(Packet packetData)
        {
            // Initialise packet variables
            int packetLength = packetData.ReadInt();
            byte[] packetBytes = packetData.ReadBytes(packetLength);

            // Process packet on main thread
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
            // Disconnect UDP
            endPoint = null;
        }
    }

    public void PlayerSetup(string playerName)
    {
        // Setting player pos across circle using playerID
        Vector3 playerPos;
        var x = 0 + 6 * Math.Cos(2 * Math.PI * (playerID - 1) / Server.MaxPlayers);
        var z = 0 + 6 * Math.Sin(2 * Math.PI * (playerID - 1) / Server.MaxPlayers); 
        playerPos = new Vector3(((float)x), 1, ((float)z));
        
        // Setting the direction the player faces using forward vector towards centre of map
        Vector3 playerForward;
        playerForward = (new Vector3(0, 1, 0) - playerPos).normalized;

        // Create player game object
        player = NetworkManager.instance.InstantiatePlayer(playerPos);
        player.InitPlayer(playerID, playerName);

        // Send player joining info about every other player but itself
        foreach (Client client in Server.clientList.Values)
        {
            if (client.player != null && client.playerID != playerID)
            {
                DataSend.PlayerJoined(playerID, client.player, playerForward);
            }
        }

        // Send itself's player info to all other clients
        foreach (Client client in Server.clientList.Values)
        {
            if (client.player != null)
            {
                DataSend.PlayerJoined(client.playerID, player, playerForward);
            }
        }

        // Increases connected playercount
        Server.connectedPlayers++;
    }

    public void GameSetup()
    {
        int playersReady = 0;

        // Checks how many players are ready
        foreach (Client client in Server.clientList.Values)
        {
            if (client.player != null && client.player.isReady) playersReady++;
        }

        // Checks if all players are ready
        if (playersReady == Server.connectedPlayers)
        {
            foreach (Client client in Server.clientList.Values)
            {
                if (client.player != null)
                {
                    // Sets players as live and sends gamestart package
                    client.player.isAlive = true;
                    DataSend.GameStart(client.playerID);
                } 
            }

            // Starts the game
            Server.gameStarted = true;
        }
    }

    public void Disconnect()
    {
        // Disconnects this client
        Debug.Log($"{player.username} disconnected");

        ThreadManager.ExecuteOnMainThread(() =>
        {
            UnityEngine.Object.Destroy(player.gameObject);
            player = null;
        });

        tcp.Disconnect();
        udp.Disconnect();
    }
}