using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the X/X numbers when a pickup item is being carried.
/// </summary>
public class PickupUIController : MonoBehaviour {

    public Canvas textCanvas;
    public Text topNumber;
    public Text middleBar;
    public Text bottomNumber;

    GameObject mainCamera;
    PickupController pickupController;
    
    void Start ()
    {
        mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        pickupController = this.transform.parent.GetComponent<PickupController>();
        setTextOpacity(0);
    }
	
	void Update ()
    {
        //point canvas at camera
        this.transform.LookAt(mainCamera.transform);
        if (!pickupController)
        {
            setTextOpacity(0);
        }
    }

    public void setTopText(float newText, byte opacity)
    {
        Color textColour = new Color32(50, 50, 50, 255);
        setTopText(newText, opacity, textColour);
    }
    public void setTopText(float newText, byte opacity, Color32 textColour)
    {
        topNumber.text = newText.ToString();
        setTextColor(textColour);
        setTextOpacity(opacity);
        //todo: check if toptext > bottomtext, if so use large toptext size, otherwise use normal size
    }

    public void setBottomText(float newText, byte opacity)
    {
        Color textColour = new Color32(50, 50, 50, 255);
        setBottomText(newText, opacity, textColour);
    }
    public void setBottomText(float newText, byte opacity, Color32 textColour)
    {
        bottomNumber.text = newText.ToString();
        setTextColor(textColour);
        setTextOpacity(opacity);
    }

    void setTextOpacity(byte opacity)
    {
        Color32 newcolor = topNumber.color;
        newcolor.a = opacity;
        topNumber.color = newcolor;

        newcolor = middleBar.color;
        newcolor.a = opacity;
        middleBar.color = newcolor;

        newcolor = bottomNumber.color;
        newcolor.a = opacity;
        bottomNumber.color = newcolor;
    }

    void setTextColor(Color newColour)
    {
        topNumber.color = newColour;
        middleBar.color = newColour;
        bottomNumber.color = newColour;
    }
}
