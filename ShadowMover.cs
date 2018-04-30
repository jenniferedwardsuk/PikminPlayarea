using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rotates the object's transform back and forth to cast moving shadows.
/// </summary>
public class ShadowMover : MonoBehaviour {

    Vector3 rotationTarget;
    Vector3 rotationTargetorig;
    Vector3 midpoint;
    
    void Start ()
    {
        rotationTargetorig = new Vector3(0.8f, 0, 0);
        rotationTarget = new Vector3(0.8f, 0, 0);
        midpoint = this.transform.eulerAngles;
    }
	
	void Update ()
    {
        this.transform.eulerAngles = Vector3.Lerp(this.transform.eulerAngles, midpoint + rotationTarget, 0.005f);
        
        if (this.transform.eulerAngles.x < midpoint.x + 0.1f)
        {
            rotationTarget = rotationTargetorig;
        }
        else if (this.transform.eulerAngles.x > midpoint.x + rotationTargetorig.x - 0.1f)
        {
            rotationTarget = rotationTargetorig * -1;
        }
    }
}
