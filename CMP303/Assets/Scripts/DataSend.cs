using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataSend : MonoBehaviour
{
    // Send data to server via TCP
    private static void SendTCPData(Packet packet)
    {
        packet.WriteLength();
        Client.instance.tcp.SendData(packet);
    }

    // Send data to server via UDP
    private static void SendUDPData(Packet packet)
    {
        packet.WriteLength();
        Client.instance.udp.SendData(packet);
    }

    #region Packets

    public static void WelcomeReceived()
    {
        using (Packet packet = new Packet((int)ClientPackets.welcomeReceived))
        {
            // Send ID and username to server as a welcome receive confirmation 
            packet.Write(Client.instance.myID);
            packet.Write(UIManager.instance.playerName.text);

            SendTCPData(packet);
        }
    }

    public static void PlayerReady()
    {
        using (Packet packet = new Packet((int)ClientPackets.playerReady))
        {
            // Send ready status to server
            packet.Write(Client.instance.myID);
            packet.Write(UIManager.instance.isReady);

            SendTCPData(packet);
        }
    }

    public static void PlayerMovement(bool[] inputs)
    {
        using (Packet packet = new Packet((int)ClientPackets.playerMovement))
        {
            // Send user inputs and current rotation to server 
            packet.Write(inputs.Length);
            foreach (bool input in inputs)
            {
                packet.Write(input);
            }
            packet.Write(GameManager.playerList[Client.instance.myID].transform.rotation);

            SendUDPData(packet);
        }
    }

    #endregion
}