using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class Server
{
    public static int MaxPlayers { get; private set; }
    public static int Port { get; private set; }

    public static Dictionary<int, Client> clientList = new Dictionary<int, Client>();
    public delegate void PacketHandler(int fromClient, Packet packet);
    public static Dictionary<int, PacketHandler> packetHandler;

    public static bool gameStarted = false;
    public static int connectedPlayers = 0;

    private static TcpListener tcpListener;
    private static UdpClient udpListener;

    public static void Start(int maxPlayers, int port)
    {
        Debug.Log("Starting server...");

        // Initialise variables
        MaxPlayers = maxPlayers;
        Port = port;
        InitServerData();

        // Initialise TCP listener
        tcpListener = new TcpListener(IPAddress.Any, Port);
        tcpListener.Start();
        tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

        // Initialise UDP listener
        udpListener = new UdpClient(Port);
        udpListener.BeginReceive(UDPReceiveCallback, null);

        Debug.Log($"Server started on {Port}.");
    }

    private static void TCPConnectCallback(IAsyncResult result)
    {
        // Initialise TCP connection with client
        TcpClient client = tcpListener.EndAcceptTcpClient(result);
        tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

        Debug.Log($"Client {client.Client.RemoteEndPoint} attempting to connect...");

        // Loop through all clients and connect the new client
        for (int i = 1; i <= MaxPlayers; ++i)
        {
            if (clientList[i].tcp.socket == null)
            {
                clientList[i].tcp.Connect(client);
                return;
            }
        }

        // Error message for when server is full
        Debug.LogError($"Client {client.Client.RemoteEndPoint} failed to connect: Server is full");
    }

    private static void UDPReceiveCallback(IAsyncResult result)
    {
        try
        {
            // Initialise packet processing variables
            IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = udpListener.EndReceive(result, ref clientEndPoint);
            udpListener.BeginReceive(UDPReceiveCallback, null);

            // Checks if packet is readable
            if (data.Length < 4) return;

            using (Packet packet = new Packet(data))
            {
                // Get clientID from packet
                int clientID = packet.ReadInt();

                // Checks for partial packet loss/corruption
                if (clientID == 0) return;

                // If client isn't already connected, set up connection
                if (clientList[clientID].udp.endPoint == null)
                {
                    clientList[clientID].udp.Connect(clientEndPoint);
                    return;
                }

                // Checks package endpoint is coming from correct place, then handles data
                if (clientList[clientID].udp.endPoint.ToString() == clientEndPoint.ToString())
                {
                    clientList[clientID].udp.HandleData(packet);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Log($"UDP data receive error: {ex}");
        }
    }

    public static void SendUDPData(IPEndPoint clientEndPoint, Packet packet)
    {
        // Attempt to send UDP data, if fail throw exception
        try
        {
            if (clientEndPoint != null) udpListener.BeginSend(packet.ToArray(), packet.Length(), clientEndPoint, null, null);
        }
        catch (Exception ex)
        {
            Debug.Log($"Error sending UDP data to client {clientEndPoint}: {ex}");
        }
    }

    private static void InitServerData()
    {
        // Fill clientList with empty sockets for connections
        for (int i = 1; i <= MaxPlayers; ++i)
        {
            clientList.Add(i, new Client(i));
        }

        // Initialise packet dictionary
        packetHandler = new Dictionary<int, PacketHandler>()
        {
            { (int)ClientPackets.welcomeReceived, DataHandler.WelcomeReceived },
            { (int)ClientPackets.playerReady, DataHandler.PlayerReady },
            { (int)ClientPackets.playerMovement, DataHandler.PlayerMovement },
        };
        Debug.Log("Packets Initialised.");
    }

    public static void Stop()
    {
        // Closes tcp and udp connections
        tcpListener.Stop();
        udpListener.Close();
    }

    public static void Restart()
    {
        // Open new server application and close this one 
        Stop();
        System.Diagnostics.Process.Start("D:/Uni Work/CMP303/Project/Server/CMP303 Server.exe");
        Application.Quit();
    }
}
