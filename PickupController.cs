using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum PickupType { Food, Treasure };

public class PickupController : MonoBehaviour {

    public float requiredAgents;
    public float maxAgents;
    public float pickupCircleRadius; //to determine size of agents' circle
    public float seedsGiven;
    public string pickupColor;
    public PickupType pickupType;
    public Vector3 destination;
    bool nearDestination;

    Transform redonionspawnpoint;
    Transform blueonionspawnpoint;
    Transform yellowonionspawnpoint;

    [HideInInspector] public List<KeyValuePair<float, GameObject>> carryPoints;

    public PickupUIController pickupUIController;
    GameObject leaderPik;
    public float leaderPikIndex = -2;

    void Start () {
        //set up carry points with angles and agent slots
        carryPoints = new List<KeyValuePair<float, GameObject>>();
        float carryDegrees = 360 / (maxAgents);
        for (int i = 0; i < maxAgents; i++)
        {
            carryPoints.Add(new KeyValuePair<float, GameObject>(i * carryDegrees, null));
        }

        //pickupUIController = GetComponentInChildren<PickupUIController>();
        if (!pickupUIController)
        {
            Debug.LogError("Pickup UI controller not found for " + this.gameObject);
        }
        else
        {
            pickupUIController.setTopText(0, 0);
            pickupUIController.setBottomText(requiredAgents, 0);
        }   

        //populate onion spawnpoints
        GameController gameController = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameController>();
        if (gameController)
        {
            redonionspawnpoint = gameController.redOnionSpawnPoint;
            blueonionspawnpoint = gameController.blueOnionSpawnPoint;
            yellowonionspawnpoint = gameController.yellowOnionSpawnPoint;
        }
        else
        {
            Debug.LogError("Game controller not found for pickup");
        }
        destination = new Vector3(0, 1, 0);
    }
	
	void Update () {
        if (nearDestination)
        {
            checkIfReachedDestination(); // can't do this in an ontriggerenter - it needs to be centred
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "OnionCenter" && pickupType == PickupType.Food)
        {
            nearDestination = true;
        }
    }

    private void checkIfReachedDestination()
    {
        List<GameObject> validDestinations = new List<GameObject>();
        if (pickupType == PickupType.Food)
        {
            validDestinations = GameObject.FindGameObjectsWithTag("OnionCenter").ToList<GameObject>();
        }
        else if (pickupType == PickupType.Treasure)
        {
            // todo: treasure pickups
        }
        bool done = false;
        float minDistance = 100f;
        foreach(GameObject dest in validDestinations)
        {
            if (!done)
            {
                Vector3 flattenedDestination = dest.transform.position;
                flattenedDestination.y = 0f;
                Vector3 flattenedPickup = this.transform.position;
                flattenedPickup.y = 0f;
                if (Vector3.Distance(flattenedDestination, flattenedPickup) < 0.5f)
                {
                    done = true;
                    doCollection();
                }
                if (Vector3.Distance(flattenedDestination, flattenedPickup) < minDistance)
                {
                    minDistance = Vector3.Distance(flattenedDestination, flattenedPickup);
                }
            }
        }
        if (minDistance > 20f)
        {
            nearDestination = false;
        }
    }

    void doCollection()
    {
        this.gameObject.tag = "Untagged";
        // remove carrying agents
        for (int i = 0; i < carryPoints.Count; i++)
        {
            if (carryPoints[i].Value != null)
            {
                AgentInteractor agentInteractor = carryPoints[i].Value.GetComponentInChildren<AgentInteractor>();
                if (agentInteractor)
                {
                    agentInteractor.dropobject();
                }
            }
        }
        if (this.gameObject.transform.parent) // on enemy bodies the pickup controller is on a child object 
        {
            Destroy(this.gameObject.transform.parent.gameObject);
        }
        else // on plain items the pickup controller is on the top object
        {
            Destroy(this.gameObject);
        }

        // todo: pulled-up-into-onion anim, spawn pik seeds or create new piks in onion, etc
    }

    public bool checkIfCarryable()
    {
        bool carryable = false;
        carryable = carryPoints.FindAll(x => x.Value != null).Count >= requiredAgents;
        return carryable;
    }

    public bool maxAgentsReached()
    {
        bool maxReached = false;
        maxReached = carryPoints.FindAll(x => x.Value != null).Count >= maxAgents;
        return maxReached;
    }

    public KeyValuePair<int, Vector3> getFreeCarryIndexAndOffset(GameObject agent)
    {
        KeyValuePair<int, Vector3> returnValue;

        // get first empty carry point
        int carryPointIndex = -1;
        KeyValuePair<float, GameObject> foundSpot = new KeyValuePair<float, GameObject>(-1, null);
        foundSpot = carryPoints.Find(x => x.Value == null);
        if (foundSpot.Key != -1)
            carryPointIndex = carryPoints.IndexOf(foundSpot);

        if (carryPointIndex != -1) // empty spot was found
        {
            // determine floor level of pickup's transform to use for carry point so that agents can reach it on the navmesh
            int floorMask = LayerMask.GetMask("Floor");
            RaycastHit floorHit;
            bool hitfloor = Physics.Raycast(this.transform.position, -Vector3.up, out floorHit, 100f, floorMask); // check down from pickup for floor
            if (!hitfloor) // floor not hit? pickup's transform may be at precisely floor level
            {
                hitfloor = Physics.Raycast(this.transform.position + Vector3.up * 10, -Vector3.up, out floorHit, 100f, floorMask); // check down from above pickup for floor
            }
            if (!hitfloor) // floor still not hit? pickup may be submerged
            {
                hitfloor = Physics.Raycast(this.transform.position, Vector3.up, out floorHit, 100f, floorMask); // check up from pickup for floor
            }
            if (!hitfloor) // very submerged??
            {
                hitfloor = Physics.Raycast(this.transform.position - Vector3.up * 10, Vector3.up, out floorHit, 100f, floorMask); // check up from below pickup for floor
            }

            if (hitfloor)
            {
                float yoffset = floorHit.point.y;
                // determine x/z offset from carry point center
                Vector3 carryOffset = new Vector3(
                    pickupCircleRadius * Mathf.Cos(carryPoints[carryPointIndex].Key * Mathf.Deg2Rad),
                    yoffset,
                    pickupCircleRadius * Mathf.Sin(carryPoints[carryPointIndex].Key * Mathf.Deg2Rad));
                // todo: to allow for sloped floors, set the y offset from the carry point's location instead of the pickup's transform
                returnValue = new KeyValuePair<int, Vector3>(carryPointIndex, carryOffset);
            }
            else
            {
                Debug.LogError("Couldn't find floor for pickup carry point at position " + this.transform.position);
                returnValue = new KeyValuePair<int, Vector3>(-1, new Vector3(0, 0, 0));
            }
        }
        else
        {
            returnValue = new KeyValuePair<int, Vector3>(-1, new Vector3(0, 0, 0));
        }
        return returnValue;
    }

    public bool checkIfCarryPointOccupied(int carryPointIndex)
    {
        bool occupied = true;
        if (carryPoints.Count > 0 && carryPointIndex < carryPoints.Count && carryPointIndex >= 0)
        {
            occupied = carryPoints[carryPointIndex].Value != null;
        }
        return occupied;
    }

    public bool assignAgentToCarryPoint(GameObject agent, int carryPointIndex)
    {
        if (carryPointIndex > carryPoints.Count)
        {
            Debug.LogError("Agent requested nonexistent carry point");
            return false;
        }
        else if (carryPoints[carryPointIndex].Value != null) // another agent already took the spot
        {
            return false;
        }
        else
        { 
            // assign agent to carry point
            carryPoints[carryPointIndex] = new KeyValuePair<float, GameObject>(carryPoints[carryPointIndex].Key, agent);
            if (!leaderPik && carryPoints.FindAll(x => x.Value != null).Count >= requiredAgents)
            {
                setNewLeaderPik();
            }
            updateUIText();
            return true;
        }
    }

    public bool removeAgentFromCarryPoint(GameObject agent, int carryPointIndex)
    {
        if (carryPointIndex == -1)
        {
            Debug.LogError("Agent trying to leave default carry point");
            return false;
        }
        else if (carryPointIndex > carryPoints.Count)
        {
            Debug.LogError("Agent trying to leave nonexistent carry point");
            return false;
        }
        else if (carryPoints[carryPointIndex].Value == null 
            || carryPoints[carryPointIndex].Value != agent) // agent already left spot
        {
            Debug.LogError("Agent trying to leave empty/wrong carry point");
            return false;
        }
        else
        {
            // remove agent from carry point
            GameObject removedAgent = carryPoints[carryPointIndex].Value;
            carryPoints[carryPointIndex] = new KeyValuePair<float, GameObject>(carryPoints[carryPointIndex].Key, null);
            if (removedAgent && removedAgent == leaderPik)
            {
                setNewLeaderPik();
            }
            updateUIText();
            return true;
        }
    }

    void setNewLeaderPik()
    {
        List<KeyValuePair<float, GameObject>> carryingAgents = carryPoints.FindAll(x => x.Value != null);
        int lastAgentIndex = carryingAgents.Count - 1;
        KeyValuePair<float, GameObject> lastCarryingAgent = new KeyValuePair<float, GameObject>();
        if (lastAgentIndex >= 0)
        {
            lastCarryingAgent = carryingAgents[lastAgentIndex];
        }
        if (lastCarryingAgent.Value && carryingAgents.Count >= requiredAgents)
        {
            leaderPik = lastCarryingAgent.Value;
            leaderPikIndex = carryPoints.IndexOf(lastCarryingAgent);
        }
        else
        {
            leaderPik = null;
            leaderPikIndex = -2;
        }
    }

    void updateUIText()
    {
        if (pickupUIController)
        {
            int carryCount = carryPoints.FindAll(x => x.Value != null).Count;
            Color textColour = new Color32(50, 50, 50, 255);
            if (carryCount >= requiredAgents)
            {
                getPickupDestination();
                if (destination == yellowonionspawnpoint.position)
                {
                    textColour = new Color32(200, 200, 0, 255);
                }
                else if (destination == redonionspawnpoint.position)
                {
                    textColour = new Color32(200, 0, 0, 255);
                }
                else if (destination == blueonionspawnpoint.position)
                {
                    textColour = new Color32(0, 0, 200, 255);
                }
                else
                {
                    textColour = new Color32(200, 200, 200, 255);
                }
            }
            byte opacity = 255;
            if (carryCount <= 0)
            {
                opacity = 0;
            }
            pickupUIController.setTopText(carryCount, opacity, textColour);
        }
    }

    public int getCurrentCarryCount()
    {
        int carryCount = carryPoints.FindAll(x => x.Value != null).Count;
        return carryCount;
    }

    public int[] getPikColourCounts()
    {
        List<KeyValuePair<float, GameObject>> carryPointsWithAgents = carryPoints.FindAll(x => x.Value != null);
        int bluecount = 0;
        int redcount = 0;
        int yellowcount = 0;
        AgentController agentController;
        for (int i = 0; i < carryPointsWithAgents.Count; i++)
        {
            agentController = null;
            agentController = carryPointsWithAgents[i].Value.transform.parent.GetComponent<AgentController>();
            if (agentController)
            {
                if (agentController.agentColour == "blue")
                    bluecount += 1;
                if (agentController.agentColour == "red")
                    redcount += 1;
                if (agentController.agentColour == "yellow")
                    yellowcount += 1;
            }
        }

        int[] pikColours = new int[3] { bluecount, redcount, yellowcount };        
        return pikColours;
    }

    public Vector3 getPickupDestination()
    {
        if (pickupType == PickupType.Food)
        {
            int[] pikColours = getPikColourCounts();
            int bluecount = pikColours[0];
            int redcount = pikColours[1];
            int yellowcount = pikColours[2];

            if (bluecount > redcount && bluecount > yellowcount)
            {
                destination = blueonionspawnpoint.position;
            }
            else if (redcount > bluecount && redcount > yellowcount)
            {
                destination = redonionspawnpoint.position;
            }
            else if (yellowcount > bluecount && yellowcount > redcount)
            {
                destination = yellowonionspawnpoint.position;
            }
            else // one or more of the colours are tied in number
            {
                // todo: check onion populations to tiebreak
                // for now: picking randomly, but only if valid destination wasn't already decided 
                System.Random rand = new System.Random();
                if (bluecount == redcount && redcount == yellowcount) // all three tied
                {
                    if (destination == new Vector3(0, 1, 0))
                    {
                        int randomNum = rand.Next(3);
                        if (randomNum == 0)
                            destination = blueonionspawnpoint.position;
                        else if (randomNum == 1)
                            destination = redonionspawnpoint.position;
                        else
                            destination = yellowonionspawnpoint.position;
                    }
                }
                else if (bluecount == redcount 
                    && (destination == new Vector3(0, 1, 0) || destination == yellowonionspawnpoint.position))
                {
                    if (rand.Next(2) == 0)
                        destination = blueonionspawnpoint.position;
                    else
                        destination = redonionspawnpoint.position;
                }
                else if (yellowcount == redcount
                    && (destination == new Vector3(0, 1, 0) || destination == blueonionspawnpoint.position))
                {
                    if (rand.Next(2) == 0)
                        destination = yellowonionspawnpoint.position;
                    else
                        destination = redonionspawnpoint.position;
                }
                else if (yellowcount == bluecount
                    && (destination == new Vector3(0, 1, 0) || destination == redonionspawnpoint.position))
                {
                    if (rand.Next(2) == 0)
                        destination = yellowonionspawnpoint.position;
                    else
                        destination = blueonionspawnpoint.position;
                }
                
            }
        }
        else if (pickupType == PickupType.Treasure)
        {
            // todo
        }
        return destination;
    }
}
