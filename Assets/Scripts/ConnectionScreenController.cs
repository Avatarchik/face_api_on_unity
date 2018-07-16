using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Threading;

public class ConnectionScreenController : MonoBehaviour {

    // The singleton instance.
    public static ConnectionScreenController instance = null;

    public InputField input;

    public Text rosInputText;
    public Text rosStatusText;
    public Text rosInputPlaceholder;

    public Button rosConnectButton;
    public Button continueButton;

    public CanvasGroup rosConnectButtonGrp;
    public CanvasGroup continueButtonGrp;

    private GameController gameController;

    void Awake()
    {
        // Enforce singleton pattern.
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Logger.Log("duplicate ConnectionScreenController, destroying");
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);
    }

	// Use this for initialization
	void Start ()
    {
        gameController = GameController.instance;
        rosInputPlaceholder.text = Constants.DEFAULT_ROSBRIDGE_IP;
	}

    public void OnStartClick()
    {
        gameObject.SetActive(false);
        gameController.AddTask(GameState.STARTED);
        SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }

    public void OnROSConnectClick()
    {
        Logger.Log("Ros Connect Button clicked");
        string rosbridgeIp = Constants.DEFAULT_ROSBRIDGE_IP;
        // If user entered a different IP, use it, otherwise stick to default.
        if (this.rosInputText.text != "")
        {
            rosbridgeIp = this.rosInputText.text;
            Logger.Log("Trying to connect to roscore at " + rosbridgeIp);
        }

        this.input.text = rosbridgeIp;

        if (gameController.GetRosManager() == null || !gameController.GetRosManager().IsConnected())
        {
            gameController.SetRosManager(new RosManager(rosbridgeIp, Constants.DEFAULT_ROSBRIDGE_PORT, gameController));
            if (gameController.GetRosManager().Connect())
            {
                // If connection successful, update status text.
                this.rosStatusText.text = "Connected to bridge, waiting on Hello World Ack...";
                this.rosStatusText.color = Color.yellow;
                this.HideElement(rosConnectButton.gameObject, rosConnectButtonGrp);
                //this.ShowElement(continueButton.gameObject, continueButtonGrp);
                // Set up the command handlers, happens the first time connection is established.
                gameController.RegisterRosMessageHandlers();
                Thread.Sleep(Constants.ROS_CONNECT_DELAY_MS); // Wait for a bit to make sure connection is established.
                gameController.GetRosManager().SendHelloWorldAction().Invoke();
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
    }

    public void ShowContinueButton()
    {
        this.rosStatusText.text = "Fully connected!";
        this.rosStatusText.color = Color.green;
        this.ShowElement(continueButton.gameObject, continueButtonGrp);
    }

    private void HideElement(GameObject obj, CanvasGroup grp)
    {
        grp.alpha = 0;
        obj.SetActive(false);
    }

    private void ShowElement(GameObject obj, CanvasGroup grp)
    {
        grp.alpha = 1;
        obj.SetActive(true);
    }
}
