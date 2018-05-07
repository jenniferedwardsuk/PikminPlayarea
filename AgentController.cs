using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum AgentState { Idle, Following, Fleeing, Attacking, Fetching, Holding, Carrying, Immobilised, Midair, Drowning, Dying, Rescuing, Planted };
public enum AgentIdleAnims { None, Waving, Searching, Yawning, Somersaulting, Sitting, Sleeping};

public class AgentController : MonoBehaviour
{
    Transform player; 
    [HideInInspector] public GameController gameController; // public - is borrowed by AgentInteractor

    public AudioSource soundPlayer;
    public AudioClip dismissClip;
    public AudioClip liftClip;
    public AudioClip giveupClip;
    public AudioClip holdClip;
    public AudioClip carryClip;
    public AudioClip attackClip;
    public AudioClip noticeWhistleClip;
    public AudioClip drowningClip;
    public AudioClip drownedClip;
    public AudioClip reachedShoreClip;
    public AudioClip diedClip;
    public AudioClip throwClip;
    public AudioClip enterWaterClip;

    public NavMeshAgent nav;  
    Animator anim;  
    Rigidbody rb;
    SkinnedMeshRenderer smr;
    public CapsuleCollider triggerCollider;
    public CapsuleCollider nonTriggerCollider;

    public AgentState agentState;
    public AgentState defaultAgentState;
    [HideInInspector] public float distToGround;
    public bool throwingWait;

    public IdleUIController idleUIController;
    AgentIdleAnims randomIdleAnim = AgentIdleAnims.None;
    float randomIdleCooldown = 0;

    public int agentNum;
    public string agentColour;
    public Material blueTex;
    public Material redTex;
    public Material yellowTex;
    public Material greyblueTex;
    public Material greyredTex;
    public Material greyyellowTex;

    [HideInInspector]public Vector3 agentTarget; // nav destination
    Quaternion lastRotation;

    float navCalcTime;
    bool navPaused;

    float drownTime;
    float drownDetectionCooldown;
    bool checkingDrown;
    bool drowning;

    void Awake()
    {
        // Set up the references.
        player = GameObject.FindGameObjectWithTag("Player").transform;
        gameController = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameController>();

        nav = GetComponent<UnityEngine.AI.NavMeshAgent>();
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        smr = GetComponentInChildren<SkinnedMeshRenderer>();

        agentState = AgentState.Following;
        distToGround = triggerCollider.bounds.extents.y;
        agentTarget = this.transform.position;
        agentState = defaultAgentState;
        navCalcTime = 0;
        navPaused = false;

        drownDetectionCooldown = 0;
    }

    void Update()
    {
        if (agentState != AgentState.Following 
            && agentState != AgentState.Immobilised && agentState != AgentState.Drowning && agentState != AgentState.Dying
            && nav.enabled && agentTarget != nav.destination)
        {
            nav.SetDestination(agentTarget);
        }
        else if (player && agentState == AgentState.Following && nav.enabled)
        {
            agentTarget = this.transform.position;
            nav.speed = player.GetComponent<PikController>().speed - 0.2f;
            if (!navPaused)
                agentTarget = player.position;

            // dismiss if player too far away
            Vector3 flatposition = this.transform.position;
            flatposition.y = 0f;
            Vector3 flatplayerposition = player.position;
            flatplayerposition.y = 0f;
            if (Vector3.Distance(flatposition, flatplayerposition) > 50)
            {
                dismiss();
            }

            // stop pursuing if path to reach player is too long
            NavMeshPath path = new NavMeshPath();
            bool targetReachable = false;
            if (navCalcTime >= 1) // performance saving: only check once per second
            {
                if (navPaused)
                    agentTarget = player.position;
                targetReachable = nav.CalculatePath(agentTarget, path);
                navCalcTime = 0;
            }
            else
            {
                navCalcTime += Time.deltaTime;
            }
            if (targetReachable)
            {
                float pathDistance = 0;
                for (int i = 0; i < path.corners.Length - 2; i++)
                {
                    pathDistance += Vector3.Distance(path.corners[i], path.corners[i + 1]);
                }
                if (pathDistance > 50)
                {
                    agentTarget = this.transform.position;
                    navPaused = true;
                }
                else
                {
                    navPaused = false;
                }
            }
            nav.SetDestination(agentTarget);
        }

        if (drownDetectionCooldown > 0)
        {
            drownDetectionCooldown -= Time.deltaTime;
            if (drownDetectionCooldown < 0)
                drownDetectionCooldown = 0;
        }
    }


    void FixedUpdate()
    {
        checkiffalling();

        StartCoroutine(Animating());
    }

    public void dismiss()
    {
        dismiss(true); // play sound by default
    }

    public void dismiss(bool playSound)
    {
        if (playSound)
        {
            soundPlayer.loop = false;
            soundPlayer.clip = dismissClip;
            soundPlayer.Play();
        }

        agentState = AgentState.Idle;        // todo: agents should separate into colour groups
        agentTarget = transform.position;
        this.nonTriggerCollider.enabled = true;

        AgentInteractor agentInteractor = GetComponentInChildren<AgentInteractor>();
        if (agentInteractor) // check for interactables nearby
        {
            StartCoroutine(agentInteractor.CheckForActions());
        }

        if (agentColour == "blue")
            smr.material = greyblueTex;
        else if (agentColour == "yellow")
            smr.material = greyyellowTex;
        else
            smr.material = greyredTex;

        idleUIController.showImage();
        gameController.updatePikNumbersAndUI();
    }

    bool idleAnimFired = true;
    int idleRunSeed = 0;
    // int tripcount = 0;
    IEnumerator Animating()
    {
        // play run animation if moving // todo: play escape animation instead, if fleeing
        if ((nav.enabled && nav.remainingDistance >= nav.stoppingDistance)
            || agentState == AgentState.Drowning) // todo: this is temporary until drowning animation is ready
        {
            anim.SetBool("IsWalking", true);
            lastRotation = transform.rotation;

            //chance of tripping  // todo: in progress
            //System.Random randIdle = new System.Random(agentNum * 100 + runcount);
            //runcount = randIdle.Next(100);
            //if (randIdle.Next(1000 + tripcount) < 10)
            //{
            //    tripcount += 1;
            //    anim.SetTrigger("Tripping");
            //    nav.velocity = new Vector3(0,0,0);
            //    nav.SetDestination(this.transform.position + new Vector3(1,0,1));
            //    //nav.enabled = false;
            //    float falltime = 0;
            //    while (falltime < 3)
            //    {
            //        nav.velocity = new Vector3(0, 0, 0);
            //        yield return new WaitForSeconds(0);
            //        falltime += Time.deltaTime;
            //    }
            //    //nav.enabled = true;
            //    nav.SetDestination(player.transform.position);
            //}

        }
        else
        {
            anim.SetBool("IsWalking", false);
            transform.rotation = lastRotation; // lock agent rotation when standing still
        }

        // play idle animations if idling or waiting
        if (agentState == AgentState.Idle
            || (agentState == AgentState.Following && nav.enabled && nav.remainingDistance <= nav.stoppingDistance))
        {
            if (randomIdleCooldown <= 0) // if cooldown has run out, play chosen idle animation and reset
            {
                if (randomIdleAnim == AgentIdleAnims.None) // set a new idle animation and cooldown if not prepared already
                {
                    yield return new WaitForSeconds(((agentNum * agentNum) / (agentNum * 50))); // staggers timings across agents to make different agents randomise differently
                    System.Random randIdle = new System.Random(agentNum * 100 + idleRunSeed);
                    idleRunSeed = randIdle.Next(100);
                    if (randIdle.Next(100) < 10) // one in 10 chance of picking an idle animation
                    {
                        System.Array allIdles = System.Enum.GetValues(typeof(AgentIdleAnims));
                        randomIdleAnim = (AgentIdleAnims)allIdles.GetValue(randIdle.Next(allIdles.Length - 1) + 1);
                    }
                    randomIdleCooldown = randIdle.Next(2) + 2;
                    idleAnimFired = false;
                }
                else
                {
                    if (!idleAnimFired)
                    {
                        anim.SetTrigger(randomIdleAnim.ToString());
                        idleAnimFired = true;
                    }
                    randomIdleAnim = AgentIdleAnims.None;
                }
            }
            randomIdleCooldown -= Time.deltaTime;
        }
    }
    

    void checkiffalling()
    {
        Vector3 gravforce = this.GetComponent<ConstantForce>().force;
        
        if (gameController.isGrounded(this.gameObject, distToGround) && !throwingWait) //is on the ground and not getting thrown
        {
            if (rb)
                rb.velocity = new Vector3(0, 0, 0);
        }

        if (gameController.isGrounded(this.gameObject, distToGround) && !nav.enabled && !throwingWait) //has just landed after being thrown
        {
            gravforce = new Vector3(0, 0, 0);
            this.GetComponent<ConstantForce>().force = gravforce;
            if (agentState == AgentState.Idle)
                dismiss();
            nav.enabled = true;
        }
        else if (!gameController.isGrounded(this.gameObject, distToGround) && !nav.enabled) //is in midair after being thrown
        {
            gravforce.y -= 10;
            if (gravforce.y < -100)
                gravforce.y = -100;
            this.GetComponent<ConstantForce>().force = gravforce;
        }
    }

    
    private void OnTriggerEnter(Collider other)
    {
        // ignore interaction sphere collisions - only want agent body collisions
        if (other.isTrigger 
            && (other.GetType() == typeof(SphereCollider) || other.GetType() == typeof(BoxCollider) || other.GetType() == typeof(CapsuleCollider) 
                || (other.GetType() == typeof(MeshCollider) && ((MeshCollider)other).convex)) // types that can use ClosestPoint
            && Vector3.Distance(other.ClosestPoint(this.transform.position), this.transform.position) < 1) 
        {
            // whistle collision
            if (other.gameObject.tag == "Whistle" && player && player.GetComponent<PikController>().whistling 
                && agentState != AgentState.Immobilised && agentState != AgentState.Drowning)
            {
                if (agentState != AgentState.Following)
                {
                    if (agentState == AgentState.Carrying || agentState == AgentState.Holding)
                    {
                        fetchInteractor().dropobject();
                    }

                    soundPlayer.loop = false;
                    soundPlayer.clip = noticeWhistleClip;
                    soundPlayer.Play();
                    anim.SetTrigger("Surprised");

                    activate();
                    agentState = AgentState.Following;
                }
                gameController.updatePikNumbersAndUI();
            }
            if (other.gameObject.tag == "Whistle" && player && player.GetComponent<PikController>().whistling
                && agentState == AgentState.Drowning)
            {
                StartCoroutine(SwimToPlayer());
            }

            //water collision
            if (other.gameObject.tag == "Water")
            {
                if (agentColour != "blue")
                {
                    if (drownDetectionCooldown == 0)
                    {
                        if (!checkingDrown)
                        {
                            StartCoroutine(checkIfDrowning(other.gameObject));
                        }
                        drownDetectionCooldown = 1;
                    }
                }
                else
                {
                    soundPlayer.clip = enterWaterClip;
                    soundPlayer.Play();
                }
            }
        }
    }


    IEnumerator SwimToPlayer()
    {
        while (player && player.GetComponent<PikController>().whistling && agentState == AgentState.Drowning)
        {
            this.transform.position = Vector3.Lerp(this.transform.position, player.transform.position, 0.005f);
            this.transform.LookAt(player.transform);
            yield return new WaitForSeconds(0);
        }
        if (agentState != AgentState.Drowning)
        {
            activate();
            agentState = AgentState.Following;
        }
    }


    IEnumerator checkIfDrowning(GameObject waterBody)
    {
        checkingDrown = true;
        Vector3 waterLevelAgentPosition = this.transform.position;
        waterLevelAgentPosition.y = waterBody.transform.position.y;
        //int floorMask = LayerMask.GetMask("Floor");

        while (Vector3.Distance(waterBody.GetComponent<MeshCollider>().ClosestPoint(waterLevelAgentPosition), waterLevelAgentPosition) < 1 
            && agentState != AgentState.Drowning) // keep checking while agent is near water but not drowning
        {
            waterLevelAgentPosition = this.transform.position;
            waterLevelAgentPosition.y = waterBody.transform.position.y;
            if ( Vector3.Distance(waterBody.GetComponent<MeshCollider>().ClosestPoint(waterLevelAgentPosition), waterLevelAgentPosition) < 1 // agent is near water
                && (this.transform.position.y < waterBody.transform.position.y)) //and not above the surface
            {
                if (!drowning)
                {
                    dismiss(false);
                    agentState = AgentState.Drowning;
                    StartCoroutine(Drown(waterBody));
                    yield return new WaitForSeconds(0.25f);
                }
                else
                {
                    yield return new WaitForSeconds(0.25f);
                }
            }
            else // agent is in water but not out of depth
            {
                yield return new WaitForSeconds(0.25f);
            }
        }
        checkingDrown = false;
    }

    [HideInInspector] public bool grabbedByBlue;
    IEnumerator Drown(GameObject waterBody)
    {
        drowning = true;
        grabbedByBlue = false;
        nav.enabled = false;
        rb.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotation;

        if (soundPlayer.enabled && (soundPlayer.clip != drowningClip || !soundPlayer.isPlaying))
        {
            soundPlayer.loop = true;
            soundPlayer.clip = drowningClip;
            soundPlayer.Play();
        }

        Vector3 waterLevelAgentPosition = this.transform.position;
        waterLevelAgentPosition.y = waterBody.transform.position.y - 0.2f;
        this.transform.position = waterLevelAgentPosition;
        drownTime = 0;

        while (Vector3.Distance(waterBody.GetComponent<MeshCollider>().ClosestPoint(waterLevelAgentPosition), waterLevelAgentPosition) < 1 
            && (!gameController.isGrounded(this.gameObject, distToGround)
                || (gameController.isGrounded(this.gameObject, distToGround) && this.transform.position.y < waterLevelAgentPosition.y)))
        {
            if (!grabbedByBlue)
            {
                // move agent towards water surface and away from water border
                waterLevelAgentPosition = this.transform.position;
                waterLevelAgentPosition.y = waterBody.transform.position.y - 0.2f;
                this.transform.position = waterLevelAgentPosition;
            }

            if (drownTime == 0) 
            {
                this.transform.position += (waterBody.transform.position - this.transform.position).normalized;
            }

            // toggle trigger collider in order to be noticed by any nearby idle blue agents for rescue
            triggerCollider.enabled = true;
            yield return new WaitForSeconds(0.05f);
            triggerCollider.enabled = false;
            yield return new WaitForSeconds(0.05f);
            triggerCollider.enabled = true;

            drownTime += Time.deltaTime;
            if (drownTime >= 0.8f) // equates to approx five seconds due to time delay of collider toggle
            {
                agentTarget = this.transform.position;
                agentState = AgentState.Dying;
                soundPlayer.loop = false;
                soundPlayer.clip = drownedClip;
                soundPlayer.Play();
                // todo: create drowned/sinking animation
                yield return new WaitForSeconds(1.8f);

                soundPlayer.loop = false;
                soundPlayer.clip = diedClip;
                soundPlayer.Play();
                for (int i = 0; i < this.gameObject.transform.childCount; i++) // hide agent while death sound plays // todo: show ghost
                {
                    this.gameObject.transform.GetChild(i).gameObject.SetActive(false);
                }
                yield return new WaitForSeconds(1);
                Destroy(this.gameObject);
            }
        }
        if (this.gameObject && agentState != AgentState.Dying) // reached ground before drowning
        {
            nav.enabled = true;
            agentTarget = this.transform.position;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            agentState = AgentState.Idle; // dismiss isn't needed - already called when agent entered water

            if (soundPlayer.clip != reachedShoreClip) // to ensure this part is only run once
            {
                this.transform.position -= (waterBody.transform.position - this.transform.position).normalized * 2; // move away from water
                if (soundPlayer.enabled && (soundPlayer.clip != reachedShoreClip || !soundPlayer.isPlaying))
                {
                    soundPlayer.loop = false;
                    soundPlayer.clip = reachedShoreClip;
                    soundPlayer.Play();
                }
            }
            yield return new WaitForSeconds(1);
        }
        drowning = false;
    }

    int errornum = 1;
    [HideInInspector]public bool rescuing;
    public IEnumerator WaterRescue(GameObject drowningAgent)
    {
        Debug.Log(errornum + " trying rescue");
        errornum++;
        rescuing = true;
        float navStopDist = nav.stoppingDistance;
        while (this.agentColour == "blue" && agentState == AgentState.Idle
            && drowningAgent && drowningAgent.GetComponent<AgentController>().agentState == AgentState.Drowning
            && !drowningAgent.GetComponent<AgentController>().grabbedByBlue)
        {
            agentTarget = drowningAgent.transform.position;
            nav.stoppingDistance = 0;
            Vector3 flatposition = this.transform.position;
            flatposition.y = 0f;
            Vector3 flatAgentposition = drowningAgent.transform.position;
            flatAgentposition.y = 0f;
            if (Vector3.Distance(flatposition, flatAgentposition) < 0.4f)
            {
                drowningAgent.GetComponent<AgentController>().grabbedByBlue = true;
                drowningAgent.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;

                // find lowest nearby land
                Vector3 landingSite = new Vector3(0, 0, 0);
                float minDistance = 100;
                int degreesToCheck = 5;
                for (int i = 0; i < 360; i += degreesToCheck)
                {
                    this.transform.eulerAngles += new Vector3(degreesToCheck, 0, 0);
                    int floorMask = LayerMask.GetMask("Floor");
                    RaycastHit floorHit;
                    bool hitfloor = Physics.Raycast(this.transform.position + this.transform.forward * 10, Vector3.up, out floorHit, 100f, floorMask);
                    if (hitfloor)
                    {
                        if (floorHit.distance < minDistance)
                        {
                            minDistance = floorHit.distance;
                            landingSite = floorHit.point;
                        }
                    }
                }
                // apply force to throw agent
                Vector3 verticalForce = new Vector3(0, 30, 0) * drowningAgent.GetComponent<Rigidbody>().mass;
                Vector3 horizontalForce = (landingSite - transform.position).normalized * 10; 
                horizontalForce *= Vector3.Distance(landingSite, transform.position) / 10; // max throw distance is 10
                Vector3 throwVector = verticalForce + horizontalForce;
                drowningAgent.GetComponent<Rigidbody>().AddForce(throwVector, ForceMode.Impulse);

                soundPlayer.clip = throwClip;
                soundPlayer.Play();

                drowningAgent = null;
                yield return new WaitForSeconds(1);
            }
            else
            {
                yield return new WaitForSeconds(0);
            }
        }
        agentTarget = this.transform.position;
        nav.stoppingDistance = navStopDist;
        rescuing = false;
    }

    public void activate()
    {
        if (agentColour == "blue")
            smr.material = blueTex;
        else if (agentColour == "yellow")
            smr.material = yellowTex;
        else
            smr.material = redTex;

        idleUIController.hideImage();
    }

    //position to be used as the nav's destination when the agent is grounded and ready
    public void setDelayedDestination(Vector3 position)
    {
        agentTarget = position;
    }


    public AgentInteractor fetchInteractor()
    {
        AgentInteractor interactor = this.gameObject.GetComponentInChildren<AgentInteractor>();
        return interactor;
    }
}