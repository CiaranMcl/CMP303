using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public static Dictionary<int, PlayerManager> playerList = new Dictionary<int, PlayerManager>();

    public GameObject localPlayerPrefab;
    public GameObject playerPrefab;

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

    public void SpawnPlayer(int id, string username, Vector3 position, Quaternion rotation)
    {
        GameObject player;
        if (id == Client.instance.myID) player = Instantiate(localPlayerPrefab, position, rotation);
        else player = Instantiate(playerPrefab, position, rotation);

        player.GetComponent<PlayerManager>().id = id;
        player.GetComponent<PlayerManager>().username = username;

        playerList.Add(id, player.GetComponent<PlayerManager>());
    }
}