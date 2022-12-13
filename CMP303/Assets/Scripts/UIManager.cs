using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    // Singleton instance
    public static UIManager instance;

    //Start menu
    public GameObject startMenu;
    public TMP_InputField playerName;
    //Lobby menu
    public GameObject lobbyMenu;
    public Button readyButton;
    //Win/Death menu
    public GameObject winMenu;
    public GameObject deathMenu;
    public Button restartButton;
    public Button exitButton;

    // Status variables
    public bool isReady = false;

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

    public void ConnectToServer()
    {
        // Disable start menu and enable lobby menu 
        startMenu.SetActive(false);
        playerName.interactable = false;
        lobbyMenu.SetActive(true);
        readyButton.interactable = true;
        Client.instance.ConnectToServer();
    }

    public void ReadyUp()
    {
        // Flips player's ready status
        isReady = !isReady;

        // Change UI colour
        if (isReady) readyButton.GetComponent<Image>().color = Color.green;
        else readyButton.GetComponent<Image>().color = Color.red;

        // Send ready status to server
        DataSend.PlayerReady();

        // Checks if game started via the ready status update
        if (GameManager.gameStarted) GameStart();
    }

    public void GameStart()
    {
        // Disable lobby UI
        lobbyMenu.SetActive(false);
        readyButton.interactable = false;
    }

    public void Win()
    {
        // Enable win UI
        winMenu.SetActive(true);
        exitButton.gameObject.SetActive(true);
        exitButton.interactable = true;
        restartButton.gameObject.SetActive(true);
        restartButton.interactable = true;
    }

    public void Death()
    {
        // Enable death UI
        deathMenu.SetActive(true);
        exitButton.gameObject.SetActive(true);
        exitButton.interactable = true;
        restartButton.gameObject.SetActive(true);
        restartButton.interactable = true;
    }

    public void Restart()
    {
        // Open new client application and close this one 
        Client.instance.Disconnect();
        System.Diagnostics.Process.Start("D:/Uni Work/CMP303/Project/Client/CMP303.exe");
        Application.Quit();
    }

    public void Exit()
    {
        // Disconnect connection then quit
        Client.instance.Disconnect();
        Application.Quit();
    }
}
