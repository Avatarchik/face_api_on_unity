using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ConnectionScreenController : MonoBehaviour {
    
    private GameController gameController;

	// Use this for initialization
	void Start ()
    {
        gameController = GameController.instance;
	}
	
	// Update is called once per frame
	void Update ()
    {
		
	}

    public void OnConnectClick()
    {
        gameController.AddTask(GameState.STARTED);
        SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }
    /*
    private void OnROSConnectClick()
    {
        Debug.Log("Ros Connect Button clicked");
        string rosbridgeIp = Constants.DEFAULT_ROSBRIDGE_IP;
        // If user entered a different IP, use it, otherwise stick to default.
        if (this.rosInputText.text != "")
        {
            rosbridgeIp = this.rosInputText.text;
            Logger.Log("Trying to connect to roscore at " + rosbridgeIp);
        }
        if (this.rosManager == null || !this.rosManager.isConnected())
        {
            this.rosManager = new RosManager(rosbridgeIp, Constants.DEFAULT_ROSBRIDGE_PORT, this);
            this.storyManager.SetRosManager(this.rosManager);
            if (this.rosManager.Connect())
            {
                // If connection successful, update status text.
                this.rosStatusText.text = "Connected!";
                this.rosStatusText.color = Color.green;
                this.hideElement(this.rosConnectButton.gameObject);
                this.showElement(this.enterLibraryButton.gameObject);
                // Set up the command handlers, happens the first time connection is established.
                this.registerRosMessageHandlers();
                Thread.Sleep(1000); // Wait for a bit to make sure connection is established.
                this.rosManager.SendHelloWorldAction().Invoke();
                Logger.Log("Sent hello ping message");
            }
            else
            {
                this.rosStatusText.text = "Failed to connect, try again.";
                this.rosStatusText.color = Color.red;
            }
        }
        else
        {
            Logger.Log("Already connected to ROS, not trying to connect again");
        }
    }*/
}
