using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravityAttractor : MonoBehaviour
{
    [SerializeField] private float gravity = -10f;

    public void Attract(Transform body)
    {
        Vector3 targetDirection = (body.position - transform.position).normalized;
        Vector3 bodyUp = body.up;
        body.rotation = Quaternion.FromToRotation(bodyUp, targetDirection) * body.rotation;
        body.GetComponent<Rigidbody>().AddForce(targetDirection * gravity);
    }
}
