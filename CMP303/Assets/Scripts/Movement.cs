using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    Rigidbody body;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.W)) transform.position += (transform.forward / 10);
        if (Input.GetKey(KeyCode.S)) transform.position -= (transform.forward / 10);
        if (Input.GetKey(KeyCode.A)) transform.position -= (transform.right / 10);
        if (Input.GetKey(KeyCode.D)) transform.position += (transform.right / 10);

        if (Input.GetKeyDown(KeyCode.Space)) transform.position += (transform.up * 5);
    }
}
