using System.Net;
using UnityEngine;

public class DataHandler : MonoBehaviour
{
    public static void Welcome(Packet packet)
    {
        // Read welcome message and player ID info
        string msg = packet.ReadString();
        int playerID = packet.ReadInt();

        // Print read information
        Debug.Log(msg);
        Debug.Log($"Your ID is: {playerID}");

        // Process id info and send received message to server
        Client.instance.myID = playerID;
        DataSend.WelcomeReceived();

        // Start UDP connection
        Client.instance.udp.Connect(((IPEndPoint)Client.instance.tcp.socket.Client.LocalEndPoint).Port);
    }

    public static void PlayerJoined(Packet packet)
    {
        // Read player identity and transform data
        int id = packet.ReadInt();
        string username = packet.ReadString();
        Vector3 position = packet.ReadVector3();
        Vector3 playerForward = packet.ReadVector3();

        // Initialise new player
        GameManager.instance.PlayerJoined(id, username, position, playerForward);
    }

    public static void GameStart(Packet packet)
    {
        // Read tick from server
        int tick = packet.ReadInt();

        // Start the game on the same tick as server
        Client.instance.tick = tick;
        GameManager.playerList[Client.instance.myID].isAlive = true;
        GameManager.gameStarted = true;
        UIManager.instance.GameStart();
    }

    public static void PoleSpin(Packet packet)
    {
        // Read pole rotational data
        float rotation = packet.ReadFloat();

        // Process rotational data
        GameManager.instance.PoleSpin(rotation);
    }

    public static void PlayerPosition(Packet packet)
    {
        // Read server tick and player id + positional data
        int serverTick = packet.ReadInt();
        int id = packet.ReadInt();
        Vector3 position = packet.ReadVector3();

        // Sync client tick to server 
        Client.instance.tick = serverTick;

        // Process positional data and set rotation using position so player faces towards 0, 1 ,0
        GameManager.playerList[id].transform.position = position;
        GameManager.playerList[id].transform.forward = (new Vector3(0, 1, 0) - position).normalized;
    }

    public static void PlayerRotation(Packet packet)
    {
        // Read server tick and player id + rotational data
        int serverTick = packet.ReadInt();
        int id = packet.ReadInt();
        Quaternion rotation = packet.ReadQuaternion();

        // Check if packet arrived on same tick so ensure client and server are synced, then process rotational data
        if (serverTick == Client.instance.tick) GameManager.playerList[id].transform.rotation = rotation;
    }

    public static void PlayerDead(Packet packet)
    {
        // Read dead player's ID and ID of winner if applicable, if not winID = 0
        int id = packet.ReadInt();
        int winID = packet.ReadInt();

        // If this client is the winner, run win method
        if (winID == Client.instance.myID) GameManager.instance.Win();

        // Kill player on client's side
        GameManager.playerList[id].isAlive = false;
        GameManager.instance.PlayerDied(id);
    }
}
