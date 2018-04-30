using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GameController : MonoBehaviour {

    public GameObject bluePik;
    public GameObject yellowPik;
    public GameObject redPik;

    public Transform redOnionSpawnPoint;
    public Transform blueOnionSpawnPoint;
    public Transform yellowOnionSpawnPoint;

    public Material leafmat;
    public Material budmat;
    public Material flowermat;
    public Mesh leafmesh;
    public Mesh budmesh;
    public Mesh flowermesh;

    public float spawnagentcount;


    void Start()
    {
        //spawn subordinate pikmin
        System.Random pikchooser = new System.Random();
        int agentnumber = 1;
        int pikColourChoice;
        for (int i = 0; i < spawnagentcount; i++)
        {
            pikColourChoice = pikchooser.Next(3) + 1;
            Vector3 point = Random.insideUnitCircle * 5;
            Vector3 location = new Vector3(point.x, 0, point.y);

            GameObject chosenpik = null;
            if (pikColourChoice == 1)
            {
                chosenpik = redPik;
            }
            else if (pikColourChoice == 2)
            {
                chosenpik = yellowPik;
            }
            else if (pikColourChoice == 3)
            {
                chosenpik = bluePik;
            }

            if (chosenpik)
            {
                Transform childLeaf = chosenpik.transform.FindDeepChild("PikminLeaf");
                foreach (Transform child in childLeaf.transform)
                {
                    if (child.name == "mesh0")
                    {
                        int headChoice = pikchooser.Next(3) + 1;
                        SkinnedMeshRenderer headmesh = child.gameObject.GetComponent<SkinnedMeshRenderer>();
                        if (headChoice == 1)
                        {
                            headmesh.material = leafmat;
                            headmesh.sharedMesh = leafmesh;
                        }
                        else if (headChoice == 2)
                        {
                            headmesh.material = budmat;
                            headmesh.sharedMesh = budmesh;
                        }
                        else if (headChoice == 3)
                        {
                            headmesh.material = flowermat;
                            headmesh.sharedMesh = flowermesh;
                        }
                    }
                }
                chosenpik.GetComponent<AgentController>().agentNum = agentnumber;
                agentnumber += 1;
                Instantiate(chosenpik, location, Quaternion.identity);
            }
        }

        //give player pikmin a flower
        GameObject playerpikmin = GameObject.FindGameObjectWithTag("Player");
        Transform playerLeaf = playerpikmin.transform.FindDeepChild("PikminLeaf");
        foreach (Transform child in playerLeaf)
        {
            if (child.name == "mesh0")
            {
                SkinnedMeshRenderer headmesh = child.gameObject.GetComponent<SkinnedMeshRenderer>();
                headmesh.material = flowermat;
                headmesh.sharedMesh = flowermesh;
            }
        }
        
    }
    
    void Update() {

    }

    /// <summary>
    /// Check whether an object is on the floor.
    /// </summary>
    /// <param name="gameObj">Object to check.</param>
    /// <param name="objHeightToGround">Height of object's transform from the ground when standing.</param>
    /// <returns></returns>
    public bool isGrounded(GameObject gameObj, float objHeightToGround)
    {
        int floorMask = LayerMask.GetMask("Floor");
        return Physics.Raycast(gameObj.transform.position, -Vector3.up, objHeightToGround + 0.1f, floorMask);
    }

    public GameObject getNearestEnabledAgent(GameObject checker, bool includePlayer, bool followingAgentsOnly, bool groundedOnly)
    {
        GameObject agent = null;
        GameObject[] allAgents = GameObject.FindGameObjectsWithTag("Agent");
        if (includePlayer)
        {
            GameObject[] playerobjects = GameObject.FindGameObjectsWithTag("Player");
            allAgents = allAgents.Concat(playerobjects).ToArray();
        }
        float mindistance = 100;
        Vector3 flatCheckerPos = checker.transform.position;
        flatCheckerPos.y = 0f; //ignores height when checking
        Vector3 flatAgentPos;
        for (int i = 0; i < allAgents.Length; i++)
        {
            flatAgentPos = allAgents[i].transform.position;
            flatAgentPos.y = 0f;
            if (Vector3.Distance(flatCheckerPos, flatAgentPos) < mindistance) 
            {
                if (!followingAgentsOnly 
                    || (followingAgentsOnly && allAgents[i].GetComponent<AgentController>() && allAgents[i].GetComponent<AgentController>().agentState == AgentState.Following))
                {
                    if (!groundedOnly
                        || (groundedOnly && allAgents[i].GetComponent<AgentController>() && isGrounded(allAgents[i], allAgents[i].GetComponent<AgentController>().distToGround)) //agents
                        || (groundedOnly && allAgents[i].GetComponent<PikController>() && isGrounded(allAgents[i], allAgents[i].GetComponent<PikController>().distToGround))) //player
                    {
                        mindistance = Vector3.Distance(flatCheckerPos, flatAgentPos);
                        agent = allAgents[i];
                    }
                }
            }
        }
        return agent;
    }
}

public static class TransformDeepChildExtension
{
    public static Transform FindDeepChild(this Transform aParent, string aName)
    {
        foreach (Transform child in aParent)
        {
            if (child.name == aName)
                return child;
            var result = child.FindDeepChild(aName);
            if (result != null)
                return result;
        }
        return null;
    }
}