using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public int id;
    public string username;

    public float moveSpeed = 5f / Constants.TICKRATE;
    public bool[] inputs;

    public void InitPlayer(int id, string username)
    {
        this.id = id;
        this.username = username;

        inputs = new bool[4];
    }
    public void Update()
    {
        Vector2 inputDirection = Vector2.zero;

        if (inputs[0]) inputDirection.y += 1;
        if (inputs[1]) inputDirection.y -= 1;
        if (inputs[2]) inputDirection.x += 1;
        if (inputs[3]) inputDirection.x -= 1;

        Move(inputDirection);
    }
    private void Move(Vector2 inputDirection) 
    {
        Vector3 moveDirection = transform.right * inputDirection.x + transform.forward * inputDirection.y;
        transform.position += moveDirection * moveSpeed;

        DataSend.PlayerPosition(this);
        DataSend.PlayerRotation(this);
    }

    public void SetInput(bool[] inputs, Quaternion rotation)
    {
        this.inputs = inputs;
        transform.rotation = rotation;
    }
}
