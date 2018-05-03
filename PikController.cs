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

    // separated audio sources to allow sound overlap
    public AudioSource throwClip;
    public AudioSource prepareThrowClip;
    public AudioSource whistleStartClip;
    public AudioSource whistleClip;
    
    GameController gameController;
    GameObject cursor;
    GameObject whistleCircle;
    MeshRenderer whistlemesh;
    public bool throwing;
    float throwingCooldownTime = 0.2f;
    public bool whistling;

    public float distToGround;

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

        gameController = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameController>();

        distToGround = GetComponent<CapsuleCollider>().bounds.extents.y;
    }

    void FixedUpdate()
    {
        if (rb)
            rb.velocity = new Vector3(0, 0, 0); //prevent collision interference with player-controlled movement

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Move(h, v);

        // On left-click: turn player to face the mouse cursor and try to throw an agent
        if (Input.GetMouseButton(0))
        {
            Turning();
            if (!throwing)
            {
                StartCoroutine("TryThrow");
            }
        }

        if (Input.GetKeyDown("q"))
        {
            dismiss();
        }

        if (Input.GetKey("e"))
        {
            activateWhistle();
        }
        else
        {
            deactivateWhistle();
        }

        // Animate the player.
        Animating(h, v);       
    }

    void activateWhistle()
    {
        if (!whistling)
        {
            whistleStartClip.Play();
            whistleClip.Play();
        }
        whistling = true;
        whistlemesh.material.SetColor("_TintColor", new Color32(255, 255, 255, 255)); //todo: whistle particle effects
        if (whistleCircle.transform.localScale.x < 20)
            whistleCircle.transform.localScale += whistleCircle.transform.localScale * 4f * Time.deltaTime;

        whistleCircle.transform.RotateAround(whistleCircle.transform.position, new Vector3(0, 1, 0), 5);
        if (whistleCircle.transform.localScale.x < 1)
            whistleCircle.transform.localScale = new Vector3(1, 1, 1);
    }

    void deactivateWhistle()
    {
        whistleStartClip.Stop();
        whistleClip.Stop();
        whistling = false;
        whistlemesh.material.SetColor("_TintColor", new Color32(255, 255, 255, 0));
        whistleCircle.transform.localScale = new Vector3(1, 1, 1);
    }

    void dismiss()
    {
        GameObject[] followingAgents = getAllFollowingAgents();
        for (int i = 0; i < followingAgents.Length; i++)
        {
            if (followingAgents[i].GetComponent<AgentController>())
                followingAgents[i].GetComponent<AgentController>().dismiss();
        }
    }

    void Move(float h, float v)
    {
        movement = transform.forward * v + transform.right * h;
        movement = movement.normalized * speed * Time.deltaTime;
        this.transform.localPosition += movement;

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

    IEnumerator TryThrow()
    {
        throwing = true;
        while (throwing)
        {
            GameObject grabbedpik = gameController.getNearestEnabledAgent(this.gameObject, false, true, false);
            if (grabbedpik)
            {
                Vector3 heldposition = this.transform.position;
                if (heldposition.x >= 0)
                    heldposition.x += 1;
                else
                    heldposition.x -= 1;
                if (heldposition.z >= 0)
                    heldposition.z += 1;
                else
                    heldposition.z -= 1;

                //grabbedpik.GetComponent<AgentController>().triggercollider.enabled = false;
                grabbedpik.GetComponent<AgentController>().agentState = AgentState.Immobilised;
                grabbedpik.transform.position = heldposition; // todo: replace with agent destination and wait for their approach in coroutine
                grabbedpik.GetComponent<NavMeshAgent>().enabled = false;

                //prepareThrowClip.Play(); // todo: throw preparation will only apply when the player can hold down the button to postpone a throw
                yield return new WaitForSeconds(0.1f);

                grabbedpik.GetComponent<AgentController>().throwingWait = true;
                grabbedpik.GetComponent<AgentController>().agentState = AgentState.Idle;

                if (grabbedpik.GetComponentInChildren<AgentInteractor>())
                    grabbedpik.GetComponentInChildren<AgentInteractor>().interactionSphere.enabled = false;
                grabbedpik.GetComponent<NavMeshAgent>().enabled = false;

                // apply force to thrown agent: upward + directional towards cursor
                grabbedpik.GetComponent<Rigidbody>().AddForce(
                    new Vector3(0, 30, 0) * grabbedpik.GetComponent<Rigidbody>().mass + (cursor.transform.position - transform.position).normalized * 100,
                    ForceMode.Impulse);
                throwClip.Play();
                grabbedpik.GetComponent<AgentController>().agentState = AgentState.Midair;

                yield return new WaitForSeconds(throwingCooldownTime);

                grabbedpik.GetComponent<AgentController>().throwingWait = false;
                grabbedpik.GetComponent<AgentController>().nonTriggerCollider.enabled = true;
                throwing = false;

            }
            else
            {
                yield return new WaitForSeconds(0.2f);
                throwing = false;
            }
        }
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
