using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour {

    public Text followingCount;
    public Text fieldCount;
    public Text totalCount;
    GameController gameController;
    
	void Start () {

	}
	
	void Update () {
		
	}

    public void UpdateUINumbers(int followingPiksCount, int fieldPiksCount, int totalPiksCount)
    {
        followingCount.text = followingPiksCount.ToString();
        fieldCount.text = fieldPiksCount.ToString();
        totalCount.text = totalPiksCount.ToString();
    }
}
