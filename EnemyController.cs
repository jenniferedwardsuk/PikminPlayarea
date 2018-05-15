using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyState { Idle, Hunting, Eating, Dead };

public class EnemyController : MonoBehaviour {

    GameController gameController;
    NavMeshAgent nav;
    Animator animator;
    GameObject currenttarget;
    public EnemyState enemyState;
    public int enemyNum;
    public Rigidbody rb;
    public Transform spawnpoint;
    public float pursuitdistance;
    float navStopDistanceBackup;
    public GameObject loot1;
    public GameObject loot2;
    public GameObject loot3;
    public float maxHealth;
    bool animatorWalkExists;
    bool animatorDyingExists;

    public AudioSource soundPlayer; // for enemy
    public AudioSource soundPlayer2; // for target agent
    public AudioClip shakeClip; // todo: shaking off attackers
    public AudioClip dieClip1;
    public AudioClip dieClip2; // todo: death by squashing
    public AudioClip biteClip;
    public AudioClip killPikClip;
    public AudioClip grabPikClip;
    
    void Start () {
        gameController = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameController>();
        nav = GetComponent<NavMeshAgent>();
        if (nav)
            navStopDistanceBackup = nav.stoppingDistance;
        health = maxHealth;

        animator = GetComponent<Animator>();
        if (animator)
            animatorWalkExists = hasAnimParameter("isWalking", animator);
        if (animator)
            animatorDyingExists = hasAnimParameter("Dying", animator);

        // todo: make into list / array
        if (loot1 && loot1.GetComponent<Rigidbody>())
        {
            loot1.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
        }        
        if (loot2 && loot2.GetComponent<Rigidbody>())
        {
            loot2.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
        }        
        if (loot3 && loot3.GetComponent<Rigidbody>())
        {
            loot3.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
        }        
    }

    bool hasAnimParameter(string paramName, Animator animator)
    {
        foreach(AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        return false;
    }

    void Update ()
    {
        if (enemyState == EnemyState.Dead && this.gameObject.tag != "Untagged")
        {
            Die();
        }
        else
        {
            if (enemyState == EnemyState.Hunting && !currenttarget)
                chooseNearestTarget();
            if (currenttarget)
            {
                Hunt();
            }
            else
            {
                GoHome();
            }

            Animating();
        }
    }

    private void FixedUpdate()
    {
        if (rb)
        {
            rb.velocity = new Vector3(0, 0, 0); // prevent rigidbody collisions interfering with nav agent
        }
    }

    void Hunt()
    {
        if (enemyState != EnemyState.Eating && enemyState != EnemyState.Dead)
        {
            chooseNearestTarget(); // make sure still pursuing nearest target
            if (currenttarget)
            {
                nav.SetDestination(currenttarget.transform.position);

                if (Vector3.Distance(currenttarget.transform.position, transform.position) > pursuitdistance) // target is now out of range
                {
                    currenttarget = null;
                    chooseNearestTarget(); // look for new target
                    if (!currenttarget) // nothing? go home
                    {
                        GoHome();
                    }
                    // todo: territory sphere
                }
                else if (currenttarget.transform.position != spawnpoint.position
                    && Vector3.Distance(currenttarget.transform.position, transform.position) < 2)
                // target is within biting distance (todo: min distance is 1 for dwarfbulborb due to collider size)
                {
                    StartCoroutine(doattack());
                }
            }
            else // no target
            {
                GoHome();
            }
        }
    }

    void Die()
    {
        if (currenttarget)
        {
            if (currenttarget.GetComponent<AgentController>())
            {
                currenttarget.GetComponent<AgentController>().agentState = AgentState.Idle;
            }
        }

        soundPlayer.clip = dieClip1;
        soundPlayer.Play();

        droploot();

        // only movable enemies have these components
        float distToGround = 0;
        if (nav && nav.enabled)
        {
            nav.SetDestination(this.transform.position);
            nav.isStopped = true;
        }
        if (GetComponent<Rigidbody>())
            GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotation;
        if (GetComponent<SphereCollider>())
            GetComponent<SphereCollider>().enabled = false;
        if (GetComponent<BoxCollider>())
        {
            GetComponent<BoxCollider>().enabled = false;
        }

        // flip enemy over  // todo: death animation instead
        if (animatorDyingExists)
        {
            animator.enabled = false;
            GetComponent<Animation>().Play();
        }
        else
        {
            animator.enabled = false;
        }
        if (this.gameObject.GetComponentInChildren<PickupController>())
        {
            GameObject enemyBody = this.gameObject.GetComponentInChildren<PickupController>().gameObject;
            Vector3 bodyrotation = enemyBody.transform.eulerAngles;
            bodyrotation.x += 180;
            enemyBody.transform.eulerAngles = bodyrotation;
            enemyBody.tag = "Pickup";
            if (enemyBody.GetComponent<CapsuleCollider>())
                enemyBody.GetComponent<CapsuleCollider>().enabled = true;
            if (enemyBody.GetComponent<NavMeshObstacle>())
                enemyBody.GetComponent<NavMeshObstacle>().enabled = true;
        }
        this.gameObject.tag = "Untagged";
        Destroy(this);
    }

    void droploot()
    {
        System.Random lootchance = new System.Random(enemyNum);
        Transform lootspawnpoint = this.transform;
        Vector3 lootposition = lootspawnpoint.position;
        lootposition.x += 2;
        lootposition.y += 2;
        lootposition.z += 2;
        lootspawnpoint.position = lootposition;
        if (loot1 && lootchance.Next(100) < 50)
        {
            loot1.transform.parent = null;
            Instantiate(loot1, lootspawnpoint.position, lootspawnpoint.rotation);
        }
        if (loot2 && lootchance.Next(100) < 10)
        {
            lootposition.z -= 4;
            lootspawnpoint.position = lootposition;
            loot2.transform.parent = null;
            Instantiate(loot2, lootspawnpoint.position, lootspawnpoint.rotation);
        }
        if (loot3 && lootchance.Next(100) < 5)
        {
            lootposition.x -= 4;
            lootspawnpoint.position = lootposition;
            loot3.transform.parent = null;
            Instantiate(loot3, lootspawnpoint.position, lootspawnpoint.rotation);
        }

    }

    float _health;
    public float health
    {
        get
        {
            return _health;
        }
        set
        {
            _health = value;
            if (_health <= 0)
            {
                _health = 0;
                enemyState = EnemyState.Dead;
            }
            HPUIController HPUI = this.gameObject.GetComponentInChildren<HPUIController>();
            if (HPUI)
            {
                HPUI.updateHealthWheel(_health, maxHealth);
            }
        }
    }

    void GoHome()
    {
        if (nav)
        {
            nav.SetDestination(spawnpoint.position);
            nav.stoppingDistance = navStopDistanceBackup;
        }
        enemyState = EnemyState.Idle;
    }

    IEnumerator doattack()
    {
        //todo: if miss, animation: fall on face

        enemyState = EnemyState.Eating;
        // grab target
        if (currenttarget.GetComponent<AgentController>())
        {
            soundPlayer.clip = biteClip;
            soundPlayer.Play();
            soundPlayer2.clip = grabPikClip;
            soundPlayer2.Play();
            // deactivate target
            currenttarget.GetComponent<AgentController>().agentState = AgentState.Immobilised;
            // remove target from object if carrying
            if (currenttarget.GetComponent<AgentController>().fetchInteractor())
            {
                if (currenttarget.GetComponent<AgentController>().agentState == AgentState.Carrying)
                {
                    currenttarget.GetComponent<AgentController>().fetchInteractor().dropobject();
                }
            }
            // hold target in place
            if (currenttarget.GetComponent<AgentController>().nav && currenttarget.GetComponent<AgentController>().nav.enabled)
            {
                currenttarget.GetComponent<AgentController>().nav.destination = currenttarget.transform.position;
                //currenttarget.GetComponent<AgentController>().nav.enabled = false;
            }
        }
        // attack animation delay // todo
        yield return new WaitForSeconds(2);
        // eat target
        soundPlayer2.clip = killPikClip;
        soundPlayer2.Play();
        Destroy(currenttarget);
        gameController.updatePikNumbersAndUI();
        enemyState = EnemyState.Hunting;
    }

    void Animating()
    {
        if (nav && nav.enabled && nav.remainingDistance >= nav.stoppingDistance && animatorWalkExists)
        {
            animator.SetBool("IsWalking", true);
        }
        else if (animatorWalkExists)
        {
            animator.SetBool("IsWalking", false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((other.tag == "Agent" || other.tag == "Player")
            && enemyState != EnemyState.Eating
            && enemyState != EnemyState.Dead)
        {
            enemyState = EnemyState.Hunting;
            if (nav)
                nav.stoppingDistance = 0;
            chooseNearestTarget();
        }
    }

    void chooseNearestTarget()
    {
        currenttarget = gameController.getNearestEnabledAgent(checker:this.gameObject, includePlayer:true, followingAgentsOnly:false, groundedOnly:true);
        if (currenttarget)
        {
            if (Vector3.Distance(currenttarget.transform.position, transform.position) > pursuitdistance)
                currenttarget = null;
        }
    }
}
