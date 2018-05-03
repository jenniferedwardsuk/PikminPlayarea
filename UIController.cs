using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour {

    public Text followingCount;
    public Text fieldCount;
    public Text totalCount;
    GameController gameController;

	// Use this for initialization
	void Start () {
        //gameController = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameController>();
        //if (gameController)
        //{
        //    gameController.setUI();
        //}
        //else
        //{
        //    Debug.Log("Game controller not found for setting UI");
        //}
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void UpdateUINumbers(int followingPiksCount, int fieldPiksCount, int totalPiksCount)
    {
        followingCount.text = followingPiksCount.ToString();
        fieldCount.text = fieldPiksCount.ToString();
        totalCount.text = totalPiksCount.ToString();
    }
}
