using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Singleton instance
    public static GameManager instance;

    // Game status variable
    public static bool gameStarted;

    // Player list variable
    public static Dictionary<int, PlayerManager> playerList = new Dictionary<int, PlayerManager>();

    // Player prefab variables
    public GameObject localPlayerPrefab;
    public GameObject playerPrefab;

    // Spinning pole variables
    public GameObject spinPole;
    private float poleRot = 0;

    // Singleton initialiser
    public void Awake()
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

    public void FixedUpdate()
    {
        // If game has started, predict spinning pole's rotation clientside
        if (gameStarted)
        {
            float rotation = 0.052f * ((int)(Client.instance.tick / 300) + 1);
            if (rotation >= 3.14159f) rotation = 3.14159f;
            poleRot += rotation;
            if (poleRot >= 180) poleRot = -179;
            spinPole.transform.Rotate(0, poleRot, 0);
        }
    }

    public void PoleSpin(float rotation)
    {
        // Sync spinning pole's rotation with server
        spinPole.transform.Rotate(0, rotation, 0);
    }

    public void PlayerJoined(int id, string username, Vector3 position, Vector3 playerForward)
    {
        // Create player GameObject as either a player or enemy prefab
        GameObject player;
        if (id == Client.instance.myID) player = Instantiate(localPlayerPrefab, position, Quaternion.identity);
        else player = Instantiate(playerPrefab, position, Quaternion.identity);

        // Set transform rotation to centre of map
        player.transform.forward = playerForward;

        // Initialise identification variables
        player.GetComponent<PlayerManager>().id = id;
        player.GetComponent<PlayerManager>().username = username;

        // Add player to playerlist
        playerList.Add(id, player.GetComponent<PlayerManager>());
    }

    public void PlayerDied(int id)
    {
        // Initialise dead player's info into temp variable for deletion
        GameObject player;
        player = playerList[id].gameObject;

        // If death was the local player, run death method and disconnect from server, else, remove dead player from playerlist and delete object
        if (id == Client.instance.myID) 
        {
            UIManager.instance.Death();
            Client.instance.Disconnect();
        } 
        else playerList.Remove(id);
        Destroy(player);
    }

    public void Win()
    {
        // Delete local player's gameobject, run win method
        GameObject player;
        player = playerList[Client.instance.myID].gameObject;
        Destroy(player);
        UIManager.instance.Win();
    }
}