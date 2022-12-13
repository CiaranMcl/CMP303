using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager instance;
    public GameObject playerPrefab;
    public int tick = 0;

    public GameObject spinPole;
    private float poleRot = 0;

    // Singleton initialiser
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
        // Limit framerate for performance
        Application.targetFrameRate = 20;
        
        // Start server
        Server.Start(12, 42807);
    }

    void FixedUpdate()
    {
        // If game is active increase tickrate and handle non player object movement
        if (Server.gameStarted)
        {
            tick++;
            PoleRot();
        }

        // Loops tick value in case server runs for a while
        if (tick >= 50000) tick = 0;
    }

    private void PoleRot()
    {
        // Exponential speed increase using the server's tickrate
        float rotation = 0.052f * ((int)(tick / 300) + 1);
        if (rotation >= 3.14159f) rotation = 3.14159f;
        poleRot += rotation;
        if (poleRot >= 180) poleRot = -179;
        spinPole.transform.Rotate(0, poleRot, 0); 

        // Send rotation data to players      
        DataSend.PoleSpin(poleRot);
    }

    private void OnApplicationQuit()
    {
        // Closes connections before exit
        Server.Stop();
    }

    public Player InstantiatePlayer(Vector3 position)
    {
        // Creates player object
        return Instantiate(playerPrefab, position, Quaternion.identity).GetComponent<Player>();
    }
}
