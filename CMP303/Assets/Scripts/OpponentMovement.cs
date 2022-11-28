using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpponentMovement : MonoBehaviour
{
    void Update()
    {
        transform.position += transform.forward; 
    }
}