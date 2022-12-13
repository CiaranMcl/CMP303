using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    // Identifier variables
    public int id;
    public string username;

    // Status variables
    public bool isReady = false;
    public bool isAlive = false;

    // Movement variables
    public CharacterController controller;
    public float gravity = -12f;
    public float moveSpeed = 5f;
    public float jumpSpeed = 10f;

    private bool[] inputs;
    private float yVelocity = 0;

    void Awake()
    {
        // Initialise movement variables with server timestep
        gravity *= Time.fixedDeltaTime * Time.fixedDeltaTime;
        moveSpeed *= Time.fixedDeltaTime;
        jumpSpeed *= Time.fixedDeltaTime;
    }

    public void InitPlayer(int id, string username)
    {
        // Initialise id variables
        this.id = id;
        this.username = username;

        inputs = new bool[5];
    }

    public void FixedUpdate()
    {
        // Creates move direction using inputs
        Vector2 inputDirection = Vector2.zero;

        if (inputs[0]) inputDirection.y += 1;
        if (inputs[1]) inputDirection.y -= 1;
        if (inputs[2]) inputDirection.x -= 1;
        if (inputs[3]) inputDirection.x += 1;

        // Runs status checks on game and player before moving
        if (Server.gameStarted && isAlive) Move(inputDirection);
    }

    void OnTriggerEnter(Collider other)
    {
        // If player touches the pole, kill them
        if (other.tag == "Pole")
        {
            Die();
        }
    }

    private void Move(Vector2 inputDirection) 
    {
        // Use inputdirection to create a movement vector
        Vector3 moveDirection = transform.right * inputDirection.x + transform.forward * inputDirection.y;
        moveDirection *= moveSpeed;

        // Jump check and gravity reset
        if (controller.isGrounded) 
        {
            yVelocity = 0;
            if (inputs[4]) yVelocity = jumpSpeed;
        }
        yVelocity += gravity;
        moveDirection.y = yVelocity;

        // Move player 
        controller.Move(moveDirection);

        // Send new transform data
        DataSend.PlayerPosition(this);
        DataSend.PlayerRotation(this);

        // If player falls below -5, kill them
        if (controller.transform.position.y < -5f) Die();
    }

    public void SetInput(bool[] inputs, Quaternion rotation)
    {
        // Set inputs and rotation
        this.inputs = inputs;
        transform.rotation = rotation;
    }

    void Die()
    {
        // Kill player
        isAlive = false;

        int deathCount = 0;
        int winID = 0;

        // Check how many players are dead
        foreach (Client client in Server.clientList.Values)
        {
            if (client.player != null && !client.player.isAlive) deathCount++;
            else if (client.player != null) winID = client.player.id;
        }

        // If all players but one are dead, send winner's ID and restart server 
        if (deathCount >= Server.connectedPlayers - 1)
        {
            DataSend.PlayerDead(this, winID);
            Server.gameStarted = false;
            Invoke(nameof(InitServerRestart), 2);
            return;
        } 
        
        // Send player death info
        DataSend.PlayerDead(this, 0);
    }

    // UNFINISHED FEATURE
    // This method exists to delay the restart past 2 seconds to give time for lagging players to catch up
    void InitServerRestart()
    {
        //Server.Restart();
    }
}
