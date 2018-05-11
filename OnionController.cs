using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnionController : MonoBehaviour {

    public int storedPiksCount;
    public GameObject pikSeed;
    public Material pikSeedMaterial;
    public Transform spawnLocation;

    GameController gameController;

    public string onionColour;
    bool spawning;
    int spawnQueueCount;

    void Start ()
    {
        GameObject gameControllerObject = GameObject.FindGameObjectWithTag("GameController");
        if (gameControllerObject)
        {
            gameController = gameControllerObject.GetComponent<GameController>();
            if (!gameController)
            {
                Debug.LogError("GameController script not found for onion");
            }
        }
        else
        { 
            Debug.LogError("GameController object not found for onion");
        }
    }
	
	void Update () {
		
	}

    public void spawnPikmin(int spawnCount, string pickupColour)
    {
        if (pickupColour == onionColour)
        {
            spawnCount *= 2;
        }
        spawnQueueCount += spawnCount;
        if (!spawning)
        {
            StartCoroutine(Spawn());
        }
    }

    IEnumerator Spawn()
    {
        spawning = true;
        bool limitReached = gameController.fieldPiksCount >= 100;

        while (spawnQueueCount > 0)
        {
            if (!limitReached)
            {
                GameObject newPikSeed = Instantiate(pikSeed, spawnLocation.position, Quaternion.identity);
                newPikSeed.GetComponent<MeshRenderer>().material = pikSeedMaterial;
                newPikSeed.GetComponent<SeedController>().seedColour = onionColour;

                int spawnAngle = Random.Range((int)1, (int)120) * 3;
                Vector3 seedRotation = newPikSeed.transform.eulerAngles;
                seedRotation.x = 20;
                seedRotation.y = spawnAngle;
                newPikSeed.transform.eulerAngles = seedRotation;

                newPikSeed.GetComponent<Rigidbody>().AddForce(newPikSeed.transform.up * 8, ForceMode.Impulse);
                newPikSeed.GetComponent<Rigidbody>().AddTorque(newPikSeed.transform.right * 2.5f, ForceMode.Impulse);
                // todo: sfx
                yield return new WaitForSeconds(0.2f);
            }
            else
            {
                storedPiksCount += 1;
                // todo: sfx
                yield return new WaitForSeconds(0.2f);
            }
            gameController.updatePikNumbersAndUI();

            limitReached = gameController.fieldPiksCount >= 100;
            spawnQueueCount -= 1;
            if (spawnQueueCount < 0)
                spawnQueueCount = 0;
        }

        spawning = false;
    }
}
