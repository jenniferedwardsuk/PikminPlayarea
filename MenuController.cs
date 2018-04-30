using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shows/hides the controls menu. Will later manage other menu options.
/// </summary>
public class MenuController : MonoBehaviour {

    public Canvas MenuCanvas;
    
	void Start () {	}
	
	void Update () {
		if (Input.GetKeyDown(KeyCode.Return))
        {
            MenuCanvas.enabled = !MenuCanvas.enabled;
        }
	}
}
