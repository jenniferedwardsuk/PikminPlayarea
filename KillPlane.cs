using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillPlane : MonoBehaviour {

    public Vector3 playerSpawnpoint;

    void Start () {
		
	}

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            other.gameObject.transform.position = playerSpawnpoint;
        }
        else
        {
            Destroy(GetTopParent(other.gameObject));
        }
    }

    GameObject GetTopParent(GameObject target)
    {
        while (target.transform.parent)
        {
            target = target.transform.parent.gameObject;
        }
        GameObject parent = target;
        return parent;
    }
}
