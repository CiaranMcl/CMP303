using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (Rigidbody))]
public class GravityBody : MonoBehaviour
{
    GravityAttractor gravCentre;
    Rigidbody body;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        gravCentre = GameObject.FindGameObjectWithTag("Gravity").GetComponent<GravityAttractor>();

        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezeRotation;
    }

    void FixedUpdate()
    {
        gravCentre.Attract(transform);
    }
}