using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager instance;
    public GameObject playerPrefab;

    void Awake()
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

    void Start()
    {
        #if UNITY_EDITOR
            Debug.LogError("Cannot start server in editor.");
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Server.Start(50, 42807);
        #endif
    }

    public Player InstantiatePlayer()
    {
        return Instantiate(playerPrefab, Vector3.zero, Quaternion.identity).GetComponent<Player>();
    }
}
