using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataSend : MonoBehaviour
{
    #region TCPData

    // Sends TCP data to one client
    public static void SendTCPData(int toClient, Packet packet) 
    {
        packet.WriteLength();
        Server.clientList[toClient].tcp.SendData(packet);
    }

    // Sends TCP data to all clients
    private static void SendTCPDataToAll(Packet packet) 
    {
        packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; ++i) 
        {
            Server.clientList[i].tcp.SendData(packet);
        }
    }

    // Sends TCP data to all clients but one
    private static void SendTCPDataToAll(int exceptionClient, Packet packet) 
    {
        packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; ++i)
        {
            if (i != exceptionClient) Server.clientList[i].tcp.SendData(packet);
        }
    }

    #endregion

    #region UDPData

    // Sends UDP data to one client
    public static void SendUDPData(int toClient, Packet packet)
    {
        packet.WriteLength();
        Server.clientList[toClient].udp.SendData(packet);
    }

    // Sends UDP data to all clients
    private static void SendUDPDataToAll(Packet packet)
    {
        packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; ++i)
        {
            Server.clientList[i].udp.SendData(packet);
        }
    }

    // Sends UDP data to all clients but one
    private static void SendUDPDataToAll(int exceptionClient, Packet packet)
    {
        packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; ++i)
        {
            if (i != exceptionClient) Server.clientList[i].udp.SendData(packet);
        }
    }

    #endregion

    #region packets

    public static void Welcome(int toClient, string msg) 
    {
        using (Packet packet = new Packet((int)ServerPackets.welcome)) 
        {
            // Write welcome message and send to connected client
            packet.Write(msg);
            packet.Write(toClient);

            SendTCPData(toClient, packet);
        }
    }

    public static void PlayerJoined(int toClient, Player player, Vector3 playerForward)
    {
        using (Packet packet = new Packet((int)ServerPackets.playerJoined)) 
        {
            // Write new player info and send to players
            packet.Write(player.id);
            packet.Write(player.username);
            packet.Write(player.transform.position);
            packet.Write(playerForward);

            SendTCPData(toClient, packet);
        }
    }

    public static void GameStart(int toClient) 
    {
        using (Packet packet = new Packet((int)ServerPackets.gameStart)) 
        {
            // Send game start message and write what tick game starts on
            packet.Write(NetworkManager.instance.tick);
            SendTCPData(toClient, packet);
        }
    }

    public static void PoleSpin(float rotation)
    {
        using (Packet packet = new Packet((int)ServerPackets.gameStart)) 
        {
            // Sending the pole's rotational data to all players
            packet.Write(rotation);
            SendUDPDataToAll(packet);
        }
    }

    public static void PlayerPosition(Player player) 
    {
        using (Packet packet = new Packet((int)ServerPackets.playerPosition)) 
        {
            // Sending current tick and player positional data to all clients
            packet.Write(NetworkManager.instance.tick);
            packet.Write(player.id);
            packet.Write(player.transform.position);

            SendUDPDataToAll(packet);
        }
    }

    public static void PlayerRotation(Player player)
    {
        using (Packet packet = new Packet((int)ServerPackets.playerRotation))
        {
            // Sending server tick and player rotational data to all but the subject player
            packet.Write(NetworkManager.instance.tick);
            packet.Write(player.id);
            packet.Write(player.transform.rotation);

            SendUDPDataToAll(player.id, packet);
        }
    }

    public static void PlayerDead(Player player, int winID)
    {
        using (Packet packet = new Packet((int)ServerPackets.playerDead))
        {
            // Sending dead player information and the ID of the winner if applicable, else winID = 0
            packet.Write(player.id);
            packet.Write(winID);

            SendUDPDataToAll(packet);
        }
    }

    #endregion  
}