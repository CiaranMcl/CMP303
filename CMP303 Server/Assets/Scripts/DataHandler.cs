using System;
using UnityEngine;

public class DataHandler 
{
    public static void WelcomeReceived(int fromClient, Packet packet) 
    {
        // Read client information
        int clientID = packet.ReadInt();
        string playerName = packet.ReadString();

        // Connection message
        Debug.Log($"{Server.clientList[fromClient].tcp.socket.Client.RemoteEndPoint} has connected under the name {playerName}");

        // Check if client uses correct id, if not send error message
        if (fromClient != clientID) Debug.LogError($"Player {playerName} has assumed the wrong client ID. ID {clientID} should be {fromClient}");

        // Set up new player
        Server.clientList[fromClient].PlayerSetup(playerName);
    }

    public static void PlayerReady(int fromClient, Packet packet)
    {
        // Read player information
        int clientID = packet.ReadInt();
        bool isReady = packet.ReadBool();

        // Ready message
        Debug.Log($"Player { clientID } ready status set to { isReady }");

        // Set player to ready and check if game can start
        Server.clientList[fromClient].player.isReady = isReady;
        if (!Server.gameStarted) Server.clientList[fromClient].GameSetup();
    }

    public static void PlayerMovement(int fromClient, Packet packet)
    {
        // Read input information
        bool[] inputs = new bool[packet.ReadInt()];
        for (int i = 0; i < inputs.Length; ++i) inputs[i] = packet.ReadBool();

        // Read rotation info
        Quaternion rotation = packet.ReadQuaternion();

        // Process input and rotation data
        Server.clientList[fromClient].player.SetInput(inputs, rotation);
    }
}