using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Movement variables
    public CharacterController controller;
    public float gravity = -12f;
    public float moveSpeed = 5f;
    public float jumpSpeed = 10f;
    private float yVelocity = 0;
    private bool[] inputs = new bool[5];

    void Awake()
    {
        // Initialise movement variables with server timestep
        gravity *= Time.fixedDeltaTime * Time.fixedDeltaTime;
        moveSpeed *= Time.fixedDeltaTime;
        jumpSpeed *= Time.fixedDeltaTime;
    }

    private void InitInputs()
    {
        // Initialise input array with key inputs
        inputs[0] = Input.GetKey(KeyCode.W);
        inputs[1] = Input.GetKey(KeyCode.S);
        inputs[2] = Input.GetKey(KeyCode.A);
        inputs[3] = Input.GetKey(KeyCode.D);
        inputs[4] = Input.GetKey(KeyCode.Space);
    }   

    private void FixedUpdate()
    {
        // Collect current inputs and send to server
        InitInputs();
        DataSend.PlayerMovement(inputs);

        // Create an input direction vector to predict player movement clientside
        Vector2 inputDirection = Vector2.zero;

        if (inputs[0]) inputDirection.y += 1;
        if (inputs[1]) inputDirection.y -= 1;
        if (inputs[2]) inputDirection.x -= 1;
        if (inputs[3]) inputDirection.x += 1;

        ProcessInputs(inputDirection);
    }
    
    private void ProcessInputs(Vector2 inputDirection)
    {
        // Use inputdirection to predict the movement and move player
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

        // Process prediction movement
        controller.Move(moveDirection);

        // Use new predicted position to predict the player's direction
        transform.forward = (new Vector3(0, 1, 0) - transform.position).normalized;
    }
}
