using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SeedController : MonoBehaviour {

    public string seedColour;
    public GameObject pikAgentRed;
    public GameObject pikAgentBlue;
    public GameObject pikAgentYellow;
    GameController gameController;

    void Start ()
    {
        GameObject gameControllerObject = GameObject.FindGameObjectWithTag("GameController");
        if (gameControllerObject)
        {
            gameController = gameControllerObject.GetComponent<GameController>();
            if (!gameController)
                Debug.LogError("Game controller script not found for seed controller");
        }
        else
        { 
            Debug.LogError("Game controller object not found for seed controller");
        }
    }
	

	void Update () {
		
	}


    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == 8) // floor layer
        {
            GameObject newAgentColour;
            if (seedColour == "red")
            {
                newAgentColour = pikAgentRed; 
            }
            else if (seedColour == "yellow")
            {
                newAgentColour = pikAgentYellow;
            }
            else
            {
                newAgentColour = pikAgentBlue;
            }
            gameController.setAgentHeadMesh(newAgentColour, false);
            GameObject newAgent = //Instantiate(newAgentColour, getPlantedPosition(), Quaternion.identity);  // todo: getPlantedPosition is in progress
                                Instantiate(newAgentColour, this.transform.position - new Vector3(0, 1, 0), Quaternion.identity);
            newAgent.GetComponent<AgentController>().Plant();

            Destroy(this.gameObject);
        }
    }

    Vector3 getPlantedPosition()
    {
        Vector3 plantedPosition = new Vector3(0,0,0);

        int floorMask = LayerMask.GetMask("Floor");
        RaycastHit floorHit;
        bool hitFloor = Physics.Raycast(this.transform.position, -this.transform.up, out floorHit, 100f, floorMask);
        if (!hitFloor)
            hitFloor = Physics.Raycast(this.transform.position - new Vector3(0,2,0), this.transform.up, out floorHit, 100f, floorMask);
        if (!hitFloor)
            Debug.Log("floor not detected for planting");
        plantedPosition = floorHit.point;
        plantedPosition.y -= 2f;

        return plantedPosition;
    }
}
