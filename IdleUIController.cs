using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IdleUIController : MonoBehaviour {

    GameObject mainCamera;
    public Image idleImage;
    
    void Start ()
    {
        mainCamera = GameObject.FindGameObjectWithTag("MainCamera");

    }
	

	void Update ()
    {
        // point image at camera
        this.transform.LookAt(mainCamera.transform);
    }

    public void showImage(float transparency)
    {
        Color newColor = idleImage.color;
        newColor.a = transparency;
        idleImage.color = newColor;
    }
}
