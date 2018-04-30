using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the HP wheel above enemies.
/// </summary>
public class HPUIController : MonoBehaviour {

    GameObject mainCamera;
    EnemyController enemyController;
    public Image wheelImage;
    public Image borderImage;

    void Start () {
        mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        enemyController = this.transform.parent.gameObject.GetComponent<EnemyController>();
        setWheelOpacity(0);
    }
	
	void Update () {
        //point image at camera
        this.transform.LookAt(mainCamera.transform);
        if (!enemyController)
        {
            setWheelOpacity(0);
        }
	}

    public void updateHealthWheel(float health, float maxHealth)
    {
        //update image visibility and color
        if (maxHealth - health > 0)
        {
            setWheelOpacity(255);
            wheelImage.fillAmount = health / maxHealth;
            if (health > maxHealth / 2)
            {
                wheelImage.color = new Color(0, 255, 0);
            }
            if (health <= maxHealth / 2 && health > maxHealth / 4)
            {
                wheelImage.color = new Color(255, 255, 0);
            }
            else if (health <= maxHealth / 4)
            {
                wheelImage.color = new Color(255, 0, 0);
            }
        }
        else
        {
            setWheelOpacity(0);
        }
    }

    void setWheelOpacity(int opacity)
    {
        Color newcolor = wheelImage.color;
        newcolor.a = opacity;
        wheelImage.color = newcolor;

        newcolor = borderImage.color;
        newcolor.a = opacity;
        borderImage.color = newcolor;
    }
}
