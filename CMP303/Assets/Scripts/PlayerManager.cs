using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    // ID variables
    [System.NonSerialized] public int id;
    [System.NonSerialized] public string username;
    // Status variables
    [System.NonSerialized] public bool isAlive = false;
}
