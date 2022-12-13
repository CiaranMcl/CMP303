using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class Client : MonoBehaviour
{
    // Singleton instance
    public static Client instance;

    // Network variables
    public static int bufferSize = 4096;
    public TCP tcp;
    public UDP udp;
    
    // Server connection variables
    public string ip = "127.0.0.1";
    public int port = 42807;
    public int tick = 0;
    
    // Identification variables
    public int myID;

    // Packet handling variables
    private bool isConnected = false;
    private delegate void PacketHandler(Packet packet);
    private static Dictionary<int, PacketHandler> packetHandler;

    // Singleton initialiser
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != null)
        {
            Debug.Log("Instance initalised, destroying object");
            Destroy(this);
        }
    }

    void Start()
    {
        // Initialise TCP and UDP
        tcp = new TCP();
        udp = new UDP();
    }

    void OnApplicationQuit()
    {
        // Disconnect from server on quit
        Disconnect();
    }

    void FixedUpdate()
    {
        // If game is running, increase the current tick value
        if (GameManager.gameStarted) tick++;
        // Cap tick to avoid data issues for long running matches
        if (tick >= 50000) tick = 0;
    }

    public void ConnectToServer()
    {
        // Start connection to server
        InitClientData();
        isConnected = true;
        tcp.Connect();
    }

    public class TCP
    {
        public TcpClient socket;

        private NetworkStream stream;
        private Packet packet;
        private byte[] receiveBuffer;

        public void Connect()
        {
            // Initialise socket variables
            socket = new TcpClient
            {
                ReceiveBufferSize = bufferSize,
                SendBufferSize = bufferSize
            };

            // Start connection
            receiveBuffer = new byte[bufferSize];
            socket.BeginConnect(instance.ip, instance.port, ConnectCallback, null);
        }

        private void ConnectCallback(IAsyncResult result)
        {
            socket.EndConnect(result);

            // Check if connected, if true assign value to stream 
            if (!socket.Connected)
            {
                return;
            }
            stream = socket.GetStream();

            // Begin reading TCP data
            packet = new Packet();
            stream.BeginRead(receiveBuffer, 0, bufferSize, ReceiveCallback, null);
        }

        public void SendData(Packet packet)
        {
            try
            {
                // If socket exists, send data
                if (socket != null)
                {
                    stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"Could not send TCP data. Error: {ex}");                
            }
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                int bytelength = stream.EndRead(result);

                // Disconnect from server if the packet is unreadable
                if (bytelength <= 0)
                {
                    instance.Disconnect();
                    return;
                } 
                
                // Create data from packet
                byte[] data = new byte[bytelength];
                Array.Copy(receiveBuffer, data, bytelength);

                // If data is correctly processed, packet is reset and data is read
                packet.Reset(HandledData(data));
                stream.BeginRead(receiveBuffer, 0, bufferSize, ReceiveCallback, null);
            }
            catch 
            {
                // Disconnect if TCP connection error
                Disconnect();
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
                // If packet processed fully, return true
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
                        packetHandler[packetID](packet);
                    }
                });

                packetLength = 0;

                // Check if packet is readable
                if (packet.UnreadLength() >= 4)
                {
                    packetLength = packet.ReadInt();
                    // If packet processed fully, return true
                    if (packetLength <= 0) return true;
                }
            }

            // If packet processed fully, return true
            if (packetLength <= 1) return true;

            // If packet not processed fully, don't reset
            return false;
        }

        private void Disconnect()
        {
            // Disconnect TCP connection
            instance.Disconnect();

            stream = null;
            packet = null;
            receiveBuffer = null;
            socket = null;
        }
    }

    public class UDP
    {
        public UdpClient socket;
        public IPEndPoint endPoint;

        public UDP()
        {
            endPoint = new IPEndPoint(IPAddress.Parse(instance.ip), instance.port);
        }

        public void Connect(int localPort)
        {
            // Initialise socket
            socket = new UdpClient(localPort);

            // Start connection with server
            socket.Connect(endPoint);
            socket.BeginReceive(ReceiveCallback, null);

            // Send initial packet to let server know of connection
            using (Packet packet = new Packet())
            {
                SendData(packet);
            }
        }

        public void SendData(Packet packet)
        {
            try
            {
                // Inserts client ID at the start of the buffer
                packet.InsertInt(instance.myID);

                // If socket exists, send packet
                if (socket != null)
                {
                    socket.BeginSend(packet.ToArray(), packet.Length(), null, null);
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"UDP output error: {ex}");
            }
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                byte[] data = socket.EndReceive(result, ref endPoint);
                socket.BeginReceive(ReceiveCallback, null);

                // Disconnect the client if the packet is unreadable
                if(data.Length < 4) 
                {
                    instance.Disconnect();
                    return;
                }
                
                // Handle packet data
                HandleData(data);
            }
            catch 
            {
                // Upon UDP error, disconnect from server
                Disconnect();
            }
        }

        private void HandleData(byte[] data)
        {
            // Creates packet using data
            using (Packet packet = new Packet(data))
            {
                int packetLength = packet.ReadInt();
                data = packet.ReadBytes(packetLength);
            }

            // Processes packet data on main thread
            ThreadManager.ExecuteOnMainThread(() => 
            {
                using (Packet packet = new Packet(data))
                {
                    int packetID = packet.ReadInt();
                    packetHandler[packetID](packet);
                }
            });
        }

        private void Disconnect()
        {
            // Disconnect UDP connection
            instance.Disconnect();

            endPoint = null;
            socket = null;
        }
    }

    private void InitClientData()
    {
        // Initialise packet dictionary
        packetHandler = new Dictionary<int, PacketHandler>()
        {
            { (int)ServerPackets.welcome, DataHandler.Welcome },
            { (int)ServerPackets.playerJoined, DataHandler.PlayerJoined },
            { (int)ServerPackets.gameStart, DataHandler.GameStart },
            { (int)ServerPackets.poleSpin, DataHandler.PoleSpin },
            { (int)ServerPackets.playerPosition, DataHandler.PlayerPosition },
            { (int)ServerPackets.playerRotation, DataHandler.PlayerRotation },
            { (int)ServerPackets.playerDead, DataHandler.PlayerDead },
        };
        Debug.Log("Packet data initialised...");
    }

    public void Disconnect()
    {
        // If client is connected, disconnect them from the server
        if (isConnected)
        {
            isConnected = false;
            tcp.socket.Close();
            udp.socket.Close();

            Debug.Log("Connection with server closed");
        }
    }
}