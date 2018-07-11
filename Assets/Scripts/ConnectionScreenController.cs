using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ConnectionScreenController : MonoBehaviour {
    
    private GameController gameController;

	// Use this for initialization
	void Start () {
        gameController = GameController.instance;
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void OnConnectClick()
    {
        gameController.ChangeState("started");
        SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }
}
