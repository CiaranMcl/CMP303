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

    private static TcpListener tcpListener;
    private static UdpClient udpListener;

    public static void Start(int maxPlayers, int port) 
    {
        MaxPlayers = maxPlayers;
        Port = port;

        Console.WriteLine("Starting server...");
        InitServerData();

        tcpListener = new TcpListener(IPAddress.Any, Port);
        tcpListener.Start();
        tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

        udpListener = new UdpClient(Port);
        udpListener.BeginReceive(UDPReceiveCallback, null);

        Console.WriteLine($"Server started on {Port}.");
    }

    private static void TCPConnectCallback(IAsyncResult result) 
    {
        TcpClient client = tcpListener.EndAcceptTcpClient(result);
        tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

        Console.WriteLine($"Client {client.Client.RemoteEndPoint} attempting to connect...");

        for (int i = 1; i <= MaxPlayers; ++i) 
        {
            if (clientList[i].tcp.socket == null) 
            {
                clientList[i].tcp.Connect(client);
                return;
            }
        }

        Console.WriteLine($"Client {client.Client.RemoteEndPoint} failed to connect: Server is full");
    }

    private static void UDPReceiveCallback(IAsyncResult result) 
    {
        try
        {
            IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = udpListener.EndReceive(result, ref clientEndPoint);
            udpListener.BeginReceive(UDPReceiveCallback, null);

            if (data.Length < 4) return;

            using (Packet packet = new Packet(data)) 
            {
                int clientID = packet.ReadInt();

                if (clientID == 0) return;

                if (clientList[clientID].udp.endPoint == null) 
                {
                    clientList[clientID].udp.Connect(clientEndPoint);
                    return;
                }

                if (clientList[clientID].udp.endPoint.ToString() == clientEndPoint.ToString()) 
                {
                    clientList[clientID].udp.HandleData(packet);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UDP data receive error: {ex}");
        }
    }

    public static void SendUDPData(IPEndPoint clientEndPoint, Packet packet) 
    {
        try
        {
            if (clientEndPoint != null) udpListener.BeginSend(packet.ToArray(), packet.Length(), clientEndPoint, null, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending UDP data to client {clientEndPoint}: {ex}");
        }
    }

    private static void InitServerData() 
    {
        for (int i = 1; i <= MaxPlayers; ++i) 
        {
            clientList.Add(i, new Client(i));
        }

        packetHandler = new Dictionary<int, PacketHandler>()
        {
            { (int)ClientPackets.welcomeReceived, DataHandler.WelcomeReceived },
            { (int)ClientPackets.playerMovement, DataHandler.PlayerMovement },
        };
        Console.WriteLine("Packets Initialised.");
    }
}
