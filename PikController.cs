using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PikController : MonoBehaviour {

    public float speed = 6f;    // The speed that the player will move at.
    Vector3 movement;           // Player's movement direction.

    Animator anim;
    Rigidbody rb;
    int floorMask;
    float camRayLength = 100f; 
    Quaternion lastRotation;    // Used to stop the player's rotation when they stop moving.

    // separate audio sources to allow sound overlap
    public AudioSource whistleStartSFX;
    public AudioSource whistleSFX;
    public AudioSource throwingSFX;
    public AudioSource miscSoundPlayer;
    public AudioClip throwClip;
    public AudioClip prepareThrowClip;
    public AudioClip enterWaterClip;
    public AudioClip unplantClip;

    GameController gameController;
    GameObject cursor;
    public GameObject cursorMesh;
    GameObject whistleCircle;
    MeshRenderer whistlemesh;
    public bool whistling;
    float whistleTimeout;
    public Vector3 yellowDismissPosition;
    public Vector3 redDismissPosition;
    public Vector3 blueDismissPosition;

    bool prepareThrow;
    public bool throwing;
    float throwingCooldownTime = 0.2f;
    GameObject grabbedpik;
    string nearestAgentColor;

    public bool unplanting;
    float unplantingCooldownTime = 0.2f;

    public float distToGround;
    ConstantForce constForce;

    void Awake()
    {
        floorMask = LayerMask.GetMask("Floor");        
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        cursor = GameObject.FindGameObjectWithTag("Cursor");
        whistleCircle = GameObject.FindGameObjectWithTag("Whistle");
        if (whistleCircle)
            whistlemesh = whistleCircle.GetComponent<MeshRenderer>();
        whistling = false;
        whistleTimeout = 0;

        gameController = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameController>();

        distToGround = GetComponent<CapsuleCollider>().bounds.extents.y;
        constForce = this.GetComponent<ConstantForce>();

        nearestAgentColor = "none";
    }

    private void Update()
    {
        if (grabbedpik)
        {
            if (nearestAgentColor != grabbedpik.GetComponent<AgentController>().agentColour)
            {
                nearestAgentColor = grabbedpik.GetComponent<AgentController>().agentColour;
                setCursorColour(nearestAgentColor);
            }
        }
        else
        {
            GameObject nearestAgent = gameController.getNearestEnabledAgent(this.gameObject, includePlayer: false, followingAgentsOnly: true, groundedOnly: true);
            if (nearestAgent)
            {
                if (nearestAgentColor != nearestAgent.GetComponent<AgentController>().agentColour)
                {
                    nearestAgentColor = nearestAgent.GetComponent<AgentController>().agentColour;
                    setCursorColour(nearestAgentColor);
                }
            }
            else
            {
                nearestAgentColor = "none";
                setCursorColour(nearestAgentColor);
            }
        }

        // On left-click: turn player to face the mouse cursor and try to throw an agent
        if (Input.GetMouseButton(0))
        {
            Turning();
            GameObject nearestPlantedAgent = getPlantedNearby();
            if (nearestPlantedAgent && !unplanting)
            {
                StartCoroutine(DoUnplant(nearestPlantedAgent));
            }
            else if (!throwing && !grabbedpik && !unplanting)
            {
                prepareThrow = true;
                StartCoroutine("TryPrepareThrow");
            }
        }
        if (!Input.GetMouseButton(0) && !throwing && prepareThrow)
        {
            StartCoroutine(TryThrow());
            prepareThrow = false;
        }

        if (Input.GetKeyDown("q"))
        {
            dismiss();
        }

        if (Input.GetKey("e"))
        {
            if (whistleTimeout < 1.56f)
            {
                activateWhistle();
                whistleTimeout += Time.deltaTime;
            }
            else
            {
                deactivateWhistle();
            }
        }
        else
        {
            deactivateWhistle();
            whistleTimeout = 0;
        }

    }

    void FixedUpdate()
    {

        if (!gameController.isGrounded(this.gameObject, distToGround))
        {
            Vector3 gravForce = constForce.force;
            gravForce.y -= 20;
            constForce.force = gravForce;
        }
        else
        {
            constForce.force = new Vector3(0, -20, 0);
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        
        if (rb)
            rb.velocity = new Vector3(0, 0, 0); // prevent collision interference with player-controlled movement

        Move(h, v);
        Animating(h, v);
    }

    GameObject getPlantedNearby()
    {
        GameObject nearestPlanted = null;
        float minDistance = 2;
        Vector3 playerFlatPosition = this.transform.position;
        playerFlatPosition.y = 0;
        
        // nb: can't reduce this to onion proximity as agents may be planted elsewhere (after battles)
        GameObject[] allagents = GameObject.FindGameObjectsWithTag("Agent");
        for (int i = 0; i < allagents.Length; i++)
        {
            if (allagents[i].GetComponent<AgentController>().agentState == AgentState.Planted)
            {
                Vector3 agentFlatPosition = allagents[i].transform.position;
                agentFlatPosition.y = 0;
                float agentDistance = Vector3.Distance(playerFlatPosition, agentFlatPosition);
                if (agentDistance < minDistance)
                {
                    nearestPlanted = allagents[i];
                    minDistance = agentDistance;
                }
            }
        }

        return nearestPlanted;
    }
    
    void activateWhistle()
    {
        if (!whistling)
        {
            whistleStartSFX.Play();
            whistleSFX.Play();
        }
        whistling = true;
        Color whistleColor = whistlemesh.material.color;
        whistleColor.a = 1;
        whistlemesh.material.color = whistleColor;

        if (whistleCircle.transform.localScale.x < 20)
            whistleCircle.transform.localScale += whistleCircle.transform.localScale * 4f * Time.deltaTime;

        whistleCircle.transform.RotateAround(whistleCircle.transform.position, new Vector3(0, 1, 0), 5);
        if (whistleCircle.transform.localScale.x < 1)
            whistleCircle.transform.localScale = new Vector3(1, 1, 1);
    }

    void deactivateWhistle()
    {
        whistleStartSFX.Stop();
        whistleSFX.Stop();
        whistling = false;
        Color whistleColor = whistlemesh.material.color;
        whistleColor.a = 0;
        whistlemesh.material.color = whistleColor;
        whistleCircle.transform.localScale = new Vector3(1, 1, 1);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Water")
        {
            miscSoundPlayer.clip = enterWaterClip;
            miscSoundPlayer.Play();
        }
        else if (other.gameObject.layer == 8) // floor
        {
            miscSoundPlayer.clip = null; // todo: choose footstep SFX
        }
    }
    

    void dismiss()
    {
        yellowDismissPosition = this.transform.position - this.transform.forward * 6 - this.transform.right * 10;
        redDismissPosition = this.transform.position - this.transform.forward * 10;
        blueDismissPosition = this.transform.position - this.transform.forward * 6 + this.transform.right * 10;

        GameObject[] followingAgents = getAllFollowingAgents();
        if (followingAgents.Length > 0)
        {
            string firstAgentColour = followingAgents[0].GetComponent<AgentController>().agentColour;
            bool moveIntoGroups = false;
            for (int i = 0; i < followingAgents.Length; i++)
            {
                if (followingAgents[i].GetComponent<AgentController>().agentColour != firstAgentColour)
                    moveIntoGroups = true;
                if (followingAgents[i].GetComponent<AgentController>())
                    followingAgents[i].GetComponent<AgentController>().dismiss(moveIntoGroups, true);
            }
        }
    }

    Vector3 StopIfWall(Vector3 movementDirection)
    {
        Vector3 newMovementDirection = movementDirection;
        Vector3 destinationPoint = this.transform.position + movementDirection;
        int floorMask = LayerMask.GetMask("Floor");
        RaycastHit floorHit;
        bool hitfloor = Physics.Raycast(this.transform.position, destinationPoint - this.transform.position, out floorHit, 1f, floorMask);
        if (hitfloor)
        {
            if (floorHit.collider.gameObject.tag == "Wall")
            {
                newMovementDirection = new Vector3(0, 0, 0);
            }
        }
        return newMovementDirection;
    }

    void Move(float h, float v)
    {
        movement = transform.forward * v + transform.right * h;
        movement = movement.normalized * speed * Time.deltaTime;
        movement = StopIfWall(movement);
        this.transform.localPosition += movement;

        if (movement.x == 0 && movement.y == 0 && movement.z == 0)
        {
            h = 0;
            v = 0;
        }

        // save latest rotation when moving or turning
        if (h != 0 || v != 0) 
            lastRotation = this.transform.rotation;
        // rotate to face movement direction if going forwards or turning
        if (v > 0 || h != 0) 
        {
            if (v > 0)
            {
                this.transform.rotation = Quaternion.Slerp(this.transform.rotation, Quaternion.LookRotation(movement), 0.15f);
            }
            else // without this, rotation is faster when moving backwards
            {
                this.transform.rotation = Quaternion.Slerp(this.transform.rotation, Quaternion.LookRotation(movement), 0.05f);
            }
        }
        else
        {
            transform.rotation = lastRotation; // lock player rotation when standing still
        }
        
    }

    void Turning()
    {
        //turn player to look at mouse cursor
        Ray camRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit floorHit;       
        if (Physics.Raycast(camRay, out floorHit, camRayLength, floorMask))
        {
            Vector3 playerToMouse = floorHit.point - transform.position;
            playerToMouse.y = 0f;
            Quaternion newRotation = Quaternion.LookRotation(playerToMouse);
            rb.MoveRotation(newRotation);
        }
    }

    void Animating(float h, float v)
    {
        bool walking = h != 0f || v != 0f;
        anim.SetBool("IsWalking", walking);
    }

    IEnumerator TryPrepareThrow()
    {
        grabbedpik = gameController.getNearestEnabledAgent(this.gameObject, false, true, false);
        if (grabbedpik)
        {
            grabbedpik.GetComponent<AgentController>().agentState = AgentState.Immobilised;
            grabbedpik.GetComponent<NavMeshAgent>().enabled = false;
            throwingSFX.clip = prepareThrowClip;
            throwingSFX.Play();
        }
        while (grabbedpik && !throwing)
        {
            Vector3 heldposition = this.transform.position - this.transform.forward;           
            grabbedpik.transform.position = heldposition; // todo: replace with agent destination and wait for their approach

            yield return new WaitForSeconds(0);
        }
    }

    IEnumerator TryThrow()
    {
        throwing = true;
        if (grabbedpik)
        {
            grabbedpik.GetComponent<AgentController>().throwingWait = true;
            grabbedpik.GetComponent<AgentController>().agentState = AgentState.Idle;

            if (grabbedpik.GetComponentInChildren<AgentInteractor>())
                grabbedpik.GetComponentInChildren<AgentInteractor>().interactionSphere.enabled = false;
            grabbedpik.GetComponent<NavMeshAgent>().enabled = false;

            // apply force to thrown agent
            Vector3 verticalForce = new Vector3(0, 30, 0) * grabbedpik.GetComponent<Rigidbody>().mass;
            Vector3 horizontalForce = (cursor.transform.position - transform.position).normalized * 65; // 65 = power needed for thrown agent to reach cursor at full extent
            horizontalForce *= Vector3.Distance(cursor.transform.position, transform.position) / 10; // distance from player to cursor varies between 1 and 10
            Vector3 throwVector = verticalForce + horizontalForce;
            grabbedpik.GetComponent<Rigidbody>().AddForce(throwVector, ForceMode.Impulse);
            
            throwingSFX.clip = throwClip;
            throwingSFX.Play();
            grabbedpik.GetComponent<AgentController>().agentState = AgentState.Midair;

            yield return new WaitForSeconds(throwingCooldownTime);

            grabbedpik.GetComponent<AgentController>().throwingWait = false;
            grabbedpik.GetComponent<AgentController>().nonTriggerCollider.enabled = true;
            grabbedpik = null;
            throwing = false;
        }
        else
        {
            throwing = false;
        }   
    }

    IEnumerator DoUnplant(GameObject nearestPlantedAgent)
    {
        unplanting = true;

        nearestPlantedAgent.GetComponent<AgentController>().Unplant();
        nearestPlantedAgent.transform.position += new Vector3(0, 1, 0); // lift agent out of ground

        nearestPlantedAgent.GetComponent<AgentController>().throwingWait = true;
        nearestPlantedAgent.GetComponent<AgentController>().newUnplant = true;
        if (nearestPlantedAgent.GetComponentInChildren<AgentInteractor>())
            nearestPlantedAgent.GetComponentInChildren<AgentInteractor>().interactionSphere.enabled = false;
        nearestPlantedAgent.GetComponent<NavMeshAgent>().enabled = false;

        // apply force to planted agent
        Vector3 verticalForce = new Vector3(0, 15, 0) * nearestPlantedAgent.GetComponent<Rigidbody>().mass;
        Vector3 horizontalForce = (this.transform.position - nearestPlantedAgent.transform.position).normalized * 10; 
        Vector3 throwVector = verticalForce + horizontalForce;
        nearestPlantedAgent.GetComponent<Rigidbody>().AddForce(throwVector, ForceMode.Impulse);

        throwingSFX.clip = unplantClip;
        throwingSFX.Play();
        nearestPlantedAgent.GetComponent<AgentController>().agentState = AgentState.Midair;

        yield return new WaitForSeconds(unplantingCooldownTime);
        nearestPlantedAgent.GetComponent<AgentController>().throwingWait = false;
        unplanting = false;
    }


    void setCursorColour(string agentColour)
    {
        Color32 cursorColor;
        if (agentColour == "blue")
        {
            cursorColor = new Color(0, 0, 1, 1);
        }
        else if (agentColour == "red")
        {
            cursorColor = new Color(1, 0, 0, 1);
        }
        else if (agentColour == "yellow")
        {
            cursorColor = new Color(1, 1, 0, 1);
        }
        else
        {
            cursorColor = new Color(1, 1, 1, 1);
        }
        cursorMesh.GetComponent<MeshRenderer>().material.color = cursorColor; // if using additive: SetColor("_TintColor", cursorColor);
    }

    GameObject[] getAllFollowingAgents()
    {
        List<GameObject> agents = new List<GameObject>();
        GameObject[] allagents = GameObject.FindGameObjectsWithTag("Agent");
        for (int i = 0; i < allagents.Length; i++)
        {
            if (allagents[i].GetComponent<AgentController>().agentState == AgentState.Following)
            {
                agents.Add(allagents[i]);
            }
        }
        return agents.ToArray();
    }
}
