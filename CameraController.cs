using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {

    GameObject player;
    public int maxZoom;
    public int minZoom;

    void Start ()
    { 
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            transform.LookAt(player.transform);
    }
	
	// Update is called once per frame
	void Update () {
		if (!player)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }
        else
        {
            //move
            this.transform.parent.position = player.transform.position;

            //zoom
            Camera camerasettings;
            if (Input.GetAxis("Mouse ScrollWheel") != 0)
            {
                camerasettings = gameObject.GetComponent<Camera>();
                camerasettings.fieldOfView = Mathf.Clamp(camerasettings.fieldOfView - Input.GetAxis("Mouse ScrollWheel") * 50, minZoom, maxZoom);
            }

            //rotate
            if (Input.GetMouseButton(1))
            {
                transform.RotateAround(transform.parent.position, Vector3.up, Input.GetAxis("Mouse X") * 5);
            }
        }
	}
}
