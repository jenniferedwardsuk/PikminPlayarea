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


    public void showImage()
    {
        Color newColor = idleImage.color;
        newColor.a = 255;
        idleImage.color = newColor;
    }


    public void hideImage()
    {
        Color newColor = idleImage.color;
        newColor.a = 0;
        idleImage.color = newColor;
    }
}
