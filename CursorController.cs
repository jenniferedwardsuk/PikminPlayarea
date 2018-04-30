using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorController : MonoBehaviour {

    int floorMask;                      // A layer mask so that a ray can be cast just at gameobjects on the floor layer.
    float camRayLength = 100f;          // The length of the ray from the camera into the scene.
    GameObject player;

    // Use this for initialization
    void Start ()
    {
        // Create a layer mask for the floor layer.
        floorMask = LayerMask.GetMask("Floor");
        player = GameObject.FindGameObjectWithTag("Player");
    }
	
	// Update is called once per frame
	void Update () {
        // Create a ray from the mouse cursor on screen in the direction of the camera.
        if (player)
        {
            Ray camRay = Camera.main.ScreenPointToRay(Input.mousePosition);

            // Create a RaycastHit variable to store information about what was hit by the ray.
            RaycastHit floorHit;

            // Perform the raycast and if it hits something on the floor layer...
            if (Physics.Raycast(camRay, out floorHit, camRayLength, floorMask))
            {
                // Create a vector from the player to the point on the floor the raycast from the mouse hit.
                Vector3 playerToMouse = floorHit.point - player.transform.position;

                // Ensure the vector is entirely along the floor plane.
                playerToMouse.y = player.transform.position.y + 1;

                // Create a quaternion (rotation) based on looking down the vector from the player to the mouse.
                //Quaternion newRotation = Quaternion.LookRotation(playerToMouse);

                //float xboundary = playerToMouse.normalized.x * 10 + player.transform.position.x;
                //float yboundary = playerToMouse.normalized.y * 10 + player.transform.position.y;
                //float zboundary = playerToMouse.normalized.z * 10 + player.transform.position.z;

                Vector3 positionhitclamped = playerToMouse + player.transform.position;

                if (Mathf.Abs(Vector3.Distance(positionhitclamped, player.transform.position)) > 10) //constrain cursor to within 10-unit radius around player
                    positionhitclamped = playerToMouse.normalized * 10 + player.transform.position;

                positionhitclamped.y = player.transform.position.y + 1;

                Ray clampedRay = Camera.main.ScreenPointToRay(Camera.main.WorldToScreenPoint(positionhitclamped));
                RaycastHit floorHitclamped;
                if (Physics.Raycast(clampedRay, out floorHitclamped, camRayLength, floorMask)) //hit floor at clamped position to get terrain height
                {
                    positionhitclamped.y = Mathf.Max(positionhitclamped.y, floorHitclamped.point.y + 1f);
                    //this.transform.Rotate(new Vector3(0,0,newRotation.eulerAngles.z));
                    //this.transform.localRotation = Quaternion.Euler(0, 0, newRotation.z);
                    this.transform.parent.LookAt(new Vector3(player.transform.position.x, transform.parent.position.y, player.transform.position.z));
                    //this.transform.parent.rotation = Quaternion.Euler(0, transform.parent.rotation.y, transform.parent.rotation.z);
                }

                this.transform.parent.position = positionhitclamped;
            }
        }
    }
}
