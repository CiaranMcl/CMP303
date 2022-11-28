using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataSend : MonoBehaviour
{
    #region TCPData

    public static void SendTCPData(int toClient, Packet packet) 
    {
        packet.WriteLength();
        Server.clientList[toClient].tcp.SendData(packet);
    }

    private static void SendTCPDataToAll(Packet packet) 
    {
        packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; ++i) 
        {
            Server.clientList[i].tcp.SendData(packet);
        }
    }

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

    public static void SendUDPData(int toClient, Packet packet)
    {
        packet.WriteLength();
        Server.clientList[toClient].udp.SendData(packet);
    }

    private static void SendUDPDataToAll(Packet packet)
    {
        packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; ++i)
        {
            Server.clientList[i].udp.SendData(packet);
        }
    }

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
            packet.Write(msg);
            packet.Write(toClient);

            SendTCPData(toClient, packet);
        }
    }

    public static void SpawnPlayers(int toClient, Player player) 
    {
        using (Packet packet = new Packet((int)ServerPackets.spawnPlayers)) 
        {
            packet.Write(player.id);
            packet.Write(player.username);
            packet.Write(player.transform.position);
            packet.Write(player.transform.rotation);

            SendTCPData(toClient, packet);
        }
    }

    public static void PlayerPosition(Player player) 
    {
        using (Packet packet = new Packet((int)ServerPackets.playerPosition)) 
        {
            packet.Write(player.id);
            packet.Write(player.transform.position);

            SendUDPDataToAll(packet);
        }
    }

    public static void PlayerRotation(Player player)
    {
        using (Packet packet = new Packet((int)ServerPackets.playerRotation))
        {
            packet.Write(player.id);
            packet.Write(player.transform.rotation);

            SendUDPDataToAll(player.id, packet);
        }
    }

    #endregion  
}
