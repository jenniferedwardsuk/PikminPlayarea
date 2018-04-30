using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum AgentState { Idle, Following, Attacking, Fetching, Holding, Carrying, Immobilised };
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

    Vector3 agentTarget; // nav destination
    Quaternion lastRotation;


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
    }


    void Update()
    {
        if (player)
        {
            if (nav.enabled && agentState == AgentState.Following)
            {
                agentTarget = player.position;
                nav.SetDestination(agentTarget);
                nav.speed = player.GetComponent<PikController>().speed - 0.2f;
            }
        }

        if (agentState != AgentState.Following && nav.enabled && agentTarget != nav.destination)
        {
            nav.SetDestination(agentTarget);
        }
    }


    void FixedUpdate()
    {
        checkiffalling();

        StartCoroutine(Animating());
    }


    public void dismiss()
    {
        soundPlayer.loop = false;
        soundPlayer.clip = dismissClip;
        soundPlayer.Play();
        agentState = AgentState.Idle;        // todo: agents should separate into colour groups
        agentTarget = transform.position;
        this.nonTriggerCollider.enabled = true;
        
        if (agentColour == "blue")
            smr.material = greyblueTex;
        else if (agentColour == "yellow")
            smr.material = greyyellowTex;
        else
            smr.material = greyredTex;
    }

    bool idleAnimFired = true;
    int idleRunSeed = 0;
    // int tripcount = 0;
    IEnumerator Animating()
    {
        // play run animation if moving // todo: play escape animation instead if fleeing
        if (nav.enabled && nav.remainingDistance >= nav.stoppingDistance)
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

        //play idle animations if idling or waiting
        if (agentState == AgentState.Idle
            || (agentState == AgentState.Following && nav.enabled && nav.remainingDistance <= nav.stoppingDistance))
        {
            if (randomIdleCooldown <= 0) //if cooldown has run out, play chosen idle animation and reset
            {
                if (randomIdleAnim == AgentIdleAnims.None) //set a new idle animation and cooldown if not prepared already
                {
                    yield return new WaitForSeconds(((agentNum * agentNum) / (agentNum * 50))); //staggers timings across agents to make different agents randomise differently
                    System.Random randIdle = new System.Random(agentNum * 100 + idleRunSeed);
                    idleRunSeed = randIdle.Next(100);
                    if (randIdle.Next(100) < 10) //one in 10 chance of picking an idle animation
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
        // todo: another else for !grounded but not thrown, i.e. falling off something
    }

    
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Whistle" && player && player.GetComponent<PikController>().whistling)
        {
            soundPlayer.loop = false;
            soundPlayer.clip = noticeWhistleClip;
            soundPlayer.Play();
            
            if (agentState == AgentState.Carrying || agentState == AgentState.Holding)
            {
                fetchInteractor().dropobject();
            }
            activate();
            if (agentState != AgentState.Following)
                anim.SetTrigger("Surprised");
            agentState = AgentState.Following;
        }
    }


    public void activate()
    {
        if (agentColour == "blue")
            smr.material = blueTex;
        else if (agentColour == "yellow")
            smr.material = yellowTex;
        else
            smr.material = redTex;
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