using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class Client : MonoBehaviour
{
    public static Client instance;
    public static int bufferSize = 4096;

    public string ip = "127.0.0.1";
    public int port = 42807;
    public int myID = 0;
    public TCP tcp;
    public UDP udp;

    private bool isConnected = false;
    private delegate void PacketHandler(Packet packet);
    private static Dictionary<int, PacketHandler> packetHandler;

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
        tcp = new TCP();
        udp = new UDP();
    }

    void OnApplicationQuit()
    {
        Disconnect();
    }

    public void ConnectToServer()
    {
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
            socket = new TcpClient
            {
                ReceiveBufferSize = bufferSize,
                SendBufferSize = bufferSize
            };

            receiveBuffer = new byte[bufferSize];
            socket.BeginConnect(instance.ip, instance.port, ConnectCallback, null);
        }

        private void ConnectCallback(IAsyncResult result)
        {
            socket.EndConnect(result);

            if (!socket.Connected)
            {
                return;
            }

            stream = socket.GetStream();

            packet = new Packet();

            stream.BeginRead(receiveBuffer, 0, bufferSize, ReceiveCallback, null);
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
                Debug.Log($"Could not send TCP data. Error: {ex}");                
            }
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                int bytelength = stream.EndRead(result);
                if (bytelength <= 0)
                {
                    instance.Disconnect();
                    return;
                } 
                

                byte[] data = new byte[bytelength];
                Array.Copy(receiveBuffer, data, bytelength);

                packet.Reset(HandledData(data));
                stream.BeginRead(receiveBuffer, 0, bufferSize, ReceiveCallback, null);
            }
            catch 
            {
                Disconnect();
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
                        packetHandler[packetID](packet);
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

        private void Disconnect()
        {
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
            socket = new UdpClient(localPort);

            socket.Connect(endPoint);
            socket.BeginReceive(ReceiveCallback, null);

            using (Packet packet = new Packet())
            {
                SendData(packet);
            }
        }

        public void SendData(Packet packet)
        {
            try
            {
                packet.InsertInt(instance.myID);

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

                if(data.Length < 4) 
                {
                    instance.Disconnect();
                    return;
                }
                
                HandleData(data);
            }
            catch 
            {
                Disconnect();
            }
        }

        private void HandleData(byte[] data)
        {
            using (Packet packet = new Packet(data))
            {
                int packetLength = packet.ReadInt();
                data = packet.ReadBytes(packetLength);
            }

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
            instance.Disconnect();

            endPoint = null;
            socket = null;
        }
    }

    private void InitClientData()
    {
        packetHandler = new Dictionary<int, PacketHandler>()
        {
            { (int)ServerPackets.welcome, DataHandler.Welcome },
            { (int)ServerPackets.spawnPlayers, DataHandler.SpawnPlayer },
            { (int)ServerPackets.playerPosition, DataHandler.PlayerPosition },
            { (int)ServerPackets.playerRotation, DataHandler.PlayerRotation },
        };
        Debug.Log("Packet data initialised...");
    }

    private void Disconnect()
    {
        if (isConnected)
        {
            isConnected = false;
            tcp.socket.Close();
            udp.socket.Close();

            Debug.Log("Connection with server closed");
        }
    }
}