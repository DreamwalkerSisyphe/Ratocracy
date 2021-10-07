using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Swarm : MonoBehaviour
{
    public byte size = 1;

    public GameObject ratPrefab;
    List<Rat> rats = new List<Rat>();

    Transform knot;

    public float startAngle = 0;
    Quaternion direction;
    public float moveSpeed = 3f;
    public float rotateSpeed = 6f;
    public float sightDistance = 1.0f;
    public float radiusBuffer = 0.01f;
    public float knotModifier = 0.2f;
    public bool rightHanded = true;

    public RaycastHit2D? hit; //For tracking and preventing duplicate raycast detections across multiple rats within the same frame

    public bool paused = false;

    // Start is called before the first frame update
    void Start()
    {
        knot = transform.Find("Knot"); //Initialize knot transform
        direction = Quaternion.Euler(new Vector3(0, 0, startAngle)); //Initialize direction

        //Create a number of rats equal to intial swarm size
        for (int i = 0; i < size; i++)
        {
            //Instantiate rat and make the swarm it's parent
            GameObject rat = Instantiate(ratPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            rat.transform.parent = transform;
            
            //Add the rat to the list
            Rat r = rat.GetComponent<Rat>();
            rats.Add(r);

            //Assign the rat's swarm and tail
            r.swarm = this;
            r.tail = r.GetComponent<LineRenderer>();

            //Set the rat's view distance
            r.sight = sightDistance;
        }
        formation();
        changeSpeed(moveSpeed);
        setDirection(direction);
    }

    // Update is called once per frame
    void Update()
    {
        // Pause debug //

        if (Input.GetKeyDown(KeyCode.P))
            paused = !paused;

        if (paused)
            return;

        //             //

        processHits();
        //Move the swarm in direction
        transform.Translate(new Vector3(-Mathf.Sin(Mathf.Deg2Rad * direction.eulerAngles.z), 
            Mathf.Cos(Mathf.Deg2Rad * direction.eulerAngles.z), 0) * Time.deltaTime * moveSpeed);
    }

    // Puts the rats in formation based on number of rats
    void formation()
    {
        if (rats.Count == 0) //No rats, hide knot and do nothing
            knot.gameObject.SetActive(false);
        else if (rats.Count == 1) //If only a single rat, hide knot, and move the rat to the center
        {
            knot.gameObject.SetActive(false);
            rats[0].transform.localPosition = new Vector3(0, 0, 0);
        }
        else
        {
            //If there is only two rats, move the knot to be more equally spaced between their tails
            if (rats.Count == 2)
                knot.localPosition = new Vector3(0.3f * Mathf.Sin(Mathf.Deg2Rad * direction.eulerAngles.z),
                    -0.3f * Mathf.Cos(Mathf.Deg2Rad * direction.eulerAngles.z), 0);
            else
                knot.localPosition = new Vector3(0, 0, 0); //Otherwise, recenter it

            //Make the knot visible and scale it based on swarm size
            knot.gameObject.SetActive(true);
            knot.localScale = new Vector3(knotModifier, knotModifier, 1) * rats.Count;

            //Calculate radius
            float radius = (ratPrefab.GetComponent<Renderer>().bounds.size.y / (2 * Mathf.Sin(Mathf.PI / rats.Count))) + radiusBuffer;

            //Iterate through the rats and spread them evenly in a ring
            for (int i = 0; i < rats.Count; i++)
            {
                //Spread the rats out by an equidistant angle
                rats[i].transform.localPosition = new Vector3(radius * Mathf.Sin(2 * Mathf.PI / rats.Count * (i + 0.5f)), 
                    radius * Mathf.Cos(2 * Mathf.PI / rats.Count * (i + 0.5f)), 0);

                //Attach tails to knot
                rats[i].tail.SetPosition(1, rats[i].transform.InverseTransformPoint(knot.position));
            }
        }
    }

    // Changes the speed of the entire swarm
    public void changeSpeed(float newSpeed)
    {
        moveSpeed = newSpeed; //Update the speed

        //Update the rat walk animation speed accordingly
        foreach (Rat r in rats)
            r.GetComponent<Animator>().speed = moveSpeed;
    }

    // Changes the direction of the entire swarm
    public void setDirection(Quaternion newDirection)
    {
        direction = newDirection; //Update swarm direction

        //Iterate through each rat to update its rotation
        foreach (Rat r in rats)
        {
            r.transform.rotation = direction; //Update rat direction

            //Readjust tails if a knot is present (more than one rat)
            if (rats.Count > 1)
            {
                if (rats.Count == 2) //Recenter knot to be equally imbetween the two rats
                    knot.localPosition = new Vector3(0.3f * Mathf.Sin(Mathf.Deg2Rad * direction.eulerAngles.z),
                    -0.3f * Mathf.Cos(Mathf.Deg2Rad * direction.eulerAngles.z), 0);
                r.tail.SetPosition(1, r.transform.InverseTransformPoint(knot.position));
            }
        }
    }

    // Adjust the direction by a certain number of degrees
    public void adjustDirection(float degrees)
    {
        setDirection(Quaternion.Euler(direction.eulerAngles + new Vector3(0, 0, degrees)));
    }

    // Processes registered hits
    void processHits()
    {
        if (hit != null)
        {
            //Get the point just before the hit position
            Vector3 beforeHit = hit!.Value.point - new Vector2(-Mathf.Sin(Mathf.Deg2Rad * direction.eulerAngles.z),
            Mathf.Cos(Mathf.Deg2Rad * direction.eulerAngles.z)).normalized * hit!.Value.distance * 0.1f;

            //Cast rays to the left and right of the hit point
            RaycastHit2D left = Physics2D.Raycast(beforeHit, -rats[0].transform.right, sightDistance, LayerMask.GetMask("Obstacles"));
            RaycastHit2D right = Physics2D.Raycast(beforeHit, rats[0].transform.right, sightDistance, LayerMask.GetMask("Obstacles"));
            Debug.DrawRay(hit!.Value.point, -rats[0].transform.right * sightDistance, Color.red, 0.5f);
            Debug.DrawRay(hit!.Value.point, rats[0].transform.right * sightDistance, Color.green, 0.5f);

            //Turn in the direction of the farthest collision (or turn based on right/left-handedness if they're equal)
            if ((left.distance > right.distance && right.collider != null) || (left.collider == null && right.collider != null))
                //Rotate left based on speed of movement and distance to collision
                adjustDirection(rotateSpeed * moveSpeed * (1 / hit!.Value.distance));
            else if ((left.distance < right.distance && left.collider != null) || (left.collider != null && right.collider == null))
                //Rotate right based on speed of movement and distance to collision
                adjustDirection(-rotateSpeed * moveSpeed * (1 / hit!.Value.distance));
            else
                //Rotate in a random direction based on speed of movement and distance to collision
                adjustDirection((rightHanded ? -1 : 1) * moveSpeed * hit!.Value.distance);

            //Refresh hit tracker
            hit = null;
        }
    }
}
