using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AgentInteractor : MonoBehaviour {
    
    public SphereCollider interactionSphere;
    public UnityEngine.AI.NavMeshAgent agentnav;
    AgentController agentcontroller;
    public float attackcooldowntime;
    float currentcooldowntime;
    float agentspeed;
    Vector3 agentDestination;
    
    GameObject attacktarget;
    EnemyController attacktargetcontroller;

    GameObject pickuptarget;
    Vector3 carrytargetoffset;
    KeyValuePair<int, Vector3> pickupCarryIndexAndPositionOffset;
    
    int errorcount = 0;
    float navStopDistanceBackup;
    
    void Awake ()
    {
        if (this.transform.parent.gameObject.GetComponent<AgentController>())
        {
            agentcontroller = this.transform.parent.gameObject.GetComponent<AgentController>();
        }
        else
        {
            Debug.Log("Agent controller not found for " + transform.parent.gameObject);
        }
        currentcooldowntime = attackcooldowntime * 0.5f;
        navStopDistanceBackup = agentnav.stoppingDistance;
        agentspeed = agentnav.speed;
    }
	
	void Update () {
        if (agentcontroller.gameController.isGrounded(agentcontroller.gameObject, agentcontroller.distToGround))
        {
            if (agentcontroller.agentState != AgentState.Idle) // idle state expected to set its own destination via dismiss
                agentcontroller.setDelayedDestination(agentDestination);

            if (agentcontroller.agentState == AgentState.Following) // todo: move this to start-following (whistle) method
            {
                //clear variables for other actions:
                attacktarget = null;
                attacktargetcontroller = null;              
                pickuptarget = null;
                pickupCarryIndexAndPositionOffset = new KeyValuePair<int, Vector3>(-1, new Vector3(0, 0, 0));
                carrytargetoffset = new Vector3(0, 0, 0);
                agentnav.stoppingDistance = navStopDistanceBackup;
            }
            else if (agentcontroller.agentState == AgentState.Attacking)
            {
                if (attacktarget)
                    tryattack(attacktarget);
                else
                {
                    agentnav.stoppingDistance = navStopDistanceBackup;
                    agentcontroller.dismiss();
                }
            }
            else if (agentcontroller.agentState == AgentState.Fetching)
            {
                agentnav.stoppingDistance = 0;
                tryfetch();
            }
            else if (agentcontroller.agentState == AgentState.Holding)
            {
                maintainhold();
            }
            else if (agentcontroller.agentState == AgentState.Carrying)
            {
                docarry();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        errorcount += 1;
        if (agentcontroller) // todo: disable trigger when thrown, until isgrounded
        {
            if ((other.gameObject.tag == "Enemy" || (other.transform.parent != null && other.transform.parent.gameObject.tag == "Enemy"))
                && agentcontroller.agentState == AgentState.Idle)
            {
                agentcontroller.activate();
                agentnav.stoppingDistance = 0;
                agentcontroller.agentState = AgentState.Attacking;
                attacktarget = other.gameObject;
                if (other.gameObject.GetComponent<EnemyController>())
                {
                    attacktargetcontroller = other.gameObject.GetComponent<EnemyController>();
                }
                else if (other.transform.parent != null && other.transform.parent.gameObject.GetComponent<EnemyController>())
                {
                    attacktargetcontroller = other.transform.parent.gameObject.GetComponent<EnemyController>();
                }
                else
                {
                    Debug.Log("Couldn't find enemy controller for " + attacktarget);
                }
                tryattack(attacktarget);
            }
            if (other.gameObject.tag == "Pickup" && agentcontroller.agentState == AgentState.Idle)
            {
                agentcontroller.activate();
                trypickup(other.gameObject);
            }
        }
        else
        {
            Debug.Log("Agent controller not found for " + transform.parent.gameObject);
        }
    }

    // todo: attacking and carrying animations

    void tryattack(GameObject target)
    {
        //attacking while clinging
        if (Vector3.Distance(target.transform.position, this.transform.position) < 3 // todo: replace with x check and z check using scale to avoid hardcoding
            && this.transform.position.y > target.transform.position.y) // todo: y check needs refining to allow low grabs
        {
            // cling and attack
            if (currentcooldowntime <= 0) // deal damage
            {
                if (!attacktargetcontroller || attacktargetcontroller.health <= 0)
                {
                    agentnav.stoppingDistance = navStopDistanceBackup;
                    agentcontroller.dismiss(); // todo: try to notice and lift enemy body - that'll be in idle look-for-activity method
                }
                else
                {
                    if (agentcontroller)
                    {
                        agentcontroller.soundPlayer.loop = false;
                        agentcontroller.soundPlayer.clip = agentcontroller.attackClip;
                        agentcontroller.soundPlayer.Play();
                    }
                    attacktargetcontroller.health = attacktargetcontroller.health - 1;
                }
                currentcooldowntime = attackcooldowntime;
            }
            else // preparing for next attack
            {
                currentcooldowntime = currentcooldowntime - Time.deltaTime;
            }
        }
        //attacking from the ground
        else if (Vector3.Distance(target.transform.position, this.transform.position) < 3)
        {
            // ground attack, hop, repeat
            if (currentcooldowntime <= 0) // deal damage
            {
                if (!attacktargetcontroller || attacktargetcontroller.health <= 0)
                {
                    agentnav.stoppingDistance = navStopDistanceBackup;
                    agentcontroller.dismiss();
                }
                else
                {
                    Debug.Log("target health is " + attacktargetcontroller.health);
                    if (agentcontroller)
                    {
                        agentcontroller.soundPlayer.loop = false;
                        agentcontroller.soundPlayer.clip = agentcontroller.attackClip;
                        agentcontroller.soundPlayer.Play();
                    }
                    attacktargetcontroller.health = attacktargetcontroller.health - 1;
                }
                currentcooldowntime = attackcooldowntime;
            }
            else // preparing for next attack
            {
                currentcooldowntime = currentcooldowntime - Time.deltaTime;
                // todo: change position (hop)
            }
        }
        // approach enemy
        else
        {
            currentcooldowntime = attackcooldowntime * 0.5f; // reset preparation time
            if (target)
                agentDestination = target.transform.position;
            else
            {
                agentnav.stoppingDistance = navStopDistanceBackup;
                agentcontroller.dismiss();
            }
        }
    }

    void trypickup(GameObject target)
    {
        // set pickup info, get pickup controller, find target pickup point
        pickuptarget = target;
        PickupController targetController = target.GetComponentInChildren<PickupController>();
        if (targetController)
        {
            if (targetController.maxAgentsReached()) // if no free spots to carry at then give up
            {
                // todo: 'give up' animation
                if (agentcontroller)
                {
                    agentcontroller.soundPlayer.loop = false;
                    agentcontroller.soundPlayer.clip = agentcontroller.giveupClip;
                    agentcontroller.soundPlayer.Play();
                }
                pickuptarget = null;
                pickupCarryIndexAndPositionOffset = new KeyValuePair<int, Vector3>(-1, new Vector3(0, 0, 0));
                agentcontroller.agentState = AgentState.Idle;
                agentcontroller.dismiss(); // todo: if player nearby, follow instead of idling
            }
            else // otherwise get target carry point
            {
                pickupCarryIndexAndPositionOffset = targetController.getFreeCarryIndexAndOffset(this.gameObject);
                agentcontroller.agentState = AgentState.Fetching;
            }
        }
        else
        {
            Debug.Log("Couldn't find pickup controller for " + target);
            pickuptarget = null;
            pickupCarryIndexAndPositionOffset = new KeyValuePair<int, Vector3>(-1, new Vector3(0,0,0));
            agentcontroller.agentState = AgentState.Idle;
            agentcontroller.dismiss(); // todo: if player nearby, follow instead of idling
        }
    }

    void tryfetch() // todo: implement a sub method to simplify this one
    {
        GameObject target = pickuptarget;

        // todo: if on top of pickup, move to the side

        // if fetch target exists
        if (pickuptarget && pickupCarryIndexAndPositionOffset.Key != -1)
        {
            PickupController targetController = target.GetComponentInChildren<PickupController>();
            // if at carry point location
            if (Vector3.Distance(this.transform.position, target.transform.position + pickupCarryIndexAndPositionOffset.Value) < 1)//Vector3.Distance(target.transform.position, this.transform.position) < target.transform.localScale.x)
            {
                if (targetController)
                {
                    // try to assign agent to carry point
                    if (targetController.assignAgentToCarryPoint(this.gameObject, pickupCarryIndexAndPositionOffset.Key))
                    {
                        if (agentcontroller)
                        {
                            // todo: need to locate the right sound effect
                            // agentcontroller.soundPlayer.clip = agentcontroller.holdClip;
                            // agentcontroller.soundPlayer.Play();
                        }
                        agentcontroller.agentState = AgentState.Holding;
                        agentDestination = target.transform.position + pickupCarryIndexAndPositionOffset.Value;
                        if (targetController.checkIfCarryable()) // if enough agents carrying, start moving
                            dopickup(target);

                    }
                    else // if another agent took the spot already
                    {
                        if (targetController.maxAgentsReached()) // if no free spots left, give up
                        {
                            // todo: 'give up' animation
                            if (agentcontroller)
                            {
                                // todo: need to locate the right sound effect
                                //agentcontroller.soundPlayer.clip = agentcontroller.giveupClip;
                                //agentcontroller.soundPlayer.Play();
                            }
                            pickuptarget = null;
                            pickupCarryIndexAndPositionOffset = new KeyValuePair<int, Vector3>(-1, new Vector3(0, 0, 0));
                            agentnav.stoppingDistance = navStopDistanceBackup;
                        }
                        else // otherwise get new target carry point
                        {
                            pickupCarryIndexAndPositionOffset = targetController.getFreeCarryIndexAndOffset(this.gameObject);
                            // todo: give up after failing a number of times
                            agentDestination = target.transform.position + pickupCarryIndexAndPositionOffset.Value;
                        }
                    }
                }
            }
            else // not at carry point location yet
            {
                Debug.Log("pik is approaching pickup. distance: " + Vector3.Distance(this.transform.position, target.transform.position + pickupCarryIndexAndPositionOffset.Value) + " with required distance 1");

                // check whether carry point was taken already - if so, get new carry point
                if (targetController.checkIfCarryPointOccupied(pickupCarryIndexAndPositionOffset.Key))
                {
                    pickupCarryIndexAndPositionOffset = targetController.getFreeCarryIndexAndOffset(this.gameObject);
                    // todo: give up after failing a number of times
                }

                // approach carry point
                agentDestination = target.transform.position + pickupCarryIndexAndPositionOffset.Value;
            }
        }
    }

    void maintainhold()
    {
        agentDestination = pickuptarget.transform.position + pickupCarryIndexAndPositionOffset.Value;
        if (agentcontroller) 
        {
            //todo: need to locate the right sound effect, and delay between repeats
            //agentcontroller.soundPlayer.clip = agentcontroller.holdClip;
            //agentcontroller.soundPlayer.Play();
        }
    }

    void dopickup(GameObject target)
    {
        // disable collisions on pickup object
        if (target.GetComponent<Rigidbody>()) // for basic pickups
        {
            target.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotation;//RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ;
        }
        if (target.transform.parent && target.transform.parent.gameObject.GetComponent<Rigidbody>()) // for enemy body pickups
        {
            target.transform.parent.gameObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotation; //RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ;
        }

        // lift up object
        Vector3 targetposition = target.transform.position;
        carrytargetoffset = new Vector3(
            targetposition.x - transform.position.x,
            targetposition.y - transform.position.y + 0.5f,
            targetposition.z - transform.position.z);

        agentcontroller.agentState = AgentState.Carrying;
        if (agentcontroller)
        {
            agentcontroller.soundPlayer.loop = false;
            agentcontroller.soundPlayer.clip = agentcontroller.liftClip;
            agentcontroller.soundPlayer.Play();
        }
        agentnav.speed = agentspeed * 0.5f;
        agentnav.stoppingDistance = navStopDistanceBackup;
        
        startingcarry = true;
    }

    bool startingcarry;
    void docarry() // todo: move this to pickup controller, pair all but one agent to the pickup itself rather than the pickup's destination
    {
        agentnav.stoppingDistance = 0.1f;

        // get destination, check that destination is still correct
        PickupController targetController = pickuptarget.GetComponentInChildren<PickupController>();
        agentDestination = targetController.getPickupDestination() - carrytargetoffset;

        // update pickuptarget's position
        Transform pickuptargetparent = pickuptarget.transform.parent;
        pickuptargetparent.transform.position = this.transform.position + carrytargetoffset; // todo: what if agent falls off? check real game
        if (agentcontroller && startingcarry)
        {
            agentcontroller.soundPlayer.loop = true;
            agentcontroller.soundPlayer.clip = agentcontroller.carryClip;
            agentcontroller.soundPlayer.Play();
            startingcarry = false;
        }
        
        // when destination is reached, drop object
        if (Vector3.Distance(transform.position, agentDestination) <= agentnav.stoppingDistance * 3)
        {
            dropobject();
        }
    }

    public void dropobject()
    {
        // remove agent from pickup
        PickupController targetController = pickuptarget.GetComponentInChildren<PickupController>();
        if (targetController)
        {
            Debug.Log("removing agent from pickup");
            targetController.removeAgentFromCarryPoint(this.gameObject, pickupCarryIndexAndPositionOffset.Key);
        }
        else
        {
            Debug.Log("Couldn't find pickup controller");
        }
        agentnav.speed = agentspeed * 2;

        // reset pickup's rigidbody constraints
        if (pickuptarget.GetComponent<Rigidbody>())
        {
            pickuptarget.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
        }
        if (pickuptarget.transform.parent.GetComponent<Rigidbody>())
        {
            pickuptarget.transform.parent.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
        }

        // clear pickup target
        pickuptarget = null;

        // dismiss agent
        agentcontroller.agentState = AgentState.Idle;
        agentcontroller.dismiss(); // todo: if player nearby, follow instead of idling
    }
}
