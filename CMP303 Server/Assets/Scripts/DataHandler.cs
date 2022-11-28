using System;
using UnityEngine;

public class DataHandler 
{
    public static void WelcomeReceived(int fromClient, Packet packet) 
        {
        int clientID = packet.ReadInt();
        string playerName = packet.ReadString();

        Console.WriteLine($"{Server.clientList[fromClient].tcp.socket.Client.RemoteEndPoint} has connected under the name {playerName}");

        if (fromClient != clientID) Console.WriteLine($"Player {playerName} has assumed the wrong client ID. ID {clientID} should be {fromClient}");

        Server.clientList[fromClient].PlayerSetup(playerName);
    }

    public static void PlayerMovement(int fromClient, Packet packet)
    {
        bool[] inputs = new bool[packet.ReadInt()];

        for (int i = 0; i < inputs.Length; ++i) inputs[i] = packet.ReadBool();

        Quaternion rotation = packet.ReadQuaternion();

        Server.clientList[fromClient].player.SetInput(inputs, rotation);
    }
}