using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rat : MonoBehaviour
{
    public Swarm swarm;

    public LineRenderer tail;

    public float sight = 1.0f;

    int obstMask;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        processSight();
    }

    // Processes rat's line of sight
    void processSight()
    {
        //Cast a ray in front of the rat
        RaycastHit2D obstRay = Physics2D.Raycast(transform.position, transform.up, sight, LayerMask.GetMask("Obstacles"));
        Debug.DrawRay(transform.position, transform.up * sight);

        //If the ray collides with an obstacle and is of a closer distance than any other registered hit, register the hit
        if (obstRay.collider != null && (swarm.hit == null || obstRay.distance < swarm.hit!.Value.distance))
            swarm.hit = obstRay;
    }
}
