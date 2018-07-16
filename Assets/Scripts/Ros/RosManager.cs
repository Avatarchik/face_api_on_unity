// This class is a high level abstraction of dealing with Ros, to separate ROS
// logic from GameController.
//
// RosManager heavily relies on RosbridgeUtilities and RosbridgeWebSocketClient.

using UnityEngine;
using System;
using System.Threading;
using System.Collections.Generic;
using MiniJSON;

public class RosManager {
    
    private readonly GameController gameController; // Keep a reference to the game controller.
    private readonly RosbridgeWebSocketClient rosClient;
    // TODO: note that for now only one handler can be registered per command.
    private Dictionary<FaceIDCommand, GameState> commandHandlers;
    private bool connected;

    System.Timers.Timer publishStateTimer =
        new System.Timers.Timer(Constants.FACEID_STATE_PUBLISH_DELAY_MS);

    // Constructor.
    public RosManager(string rosIP, string portNum, GameController gameController) {
        Logger.Log("RosManager constructor");
        this.gameController = gameController;

        this.rosClient = new RosbridgeWebSocketClient(rosIP, portNum);
        this.rosClient.receivedMsgEvent += this.OnMessageReceived;
        this.commandHandlers = new Dictionary<FaceIDCommand, GameState>();
    }

    public bool Connect() {
        // If the client disconnects then reconnects, make sure to readvertise our topics to make
        // sure we're not being ignored.
        this.rosClient.OnReconnectSuccess(this.SetupPubSub);

        if (!this.rosClient.SetupSocket()) {
            Logger.Log("Failed to set up socket");
            return false;
        }

        // Advertise ROS topic subscription/publication and set connected=true on success.
        this.SetupPubSub();

        // If connection successful, begin sending state messages.
        if (this.connected) {
            Logger.Log("Starting to send state messages");
            this.publishStateTimer.Elapsed += this.SendFaceIDState;
            this.publishStateTimer.Start();
        }

        return this.connected;
    }

    public bool IsConnected() {
        return this.connected;
    }

    public void CloseConnection() {
        this.rosClient.CloseSocket();
        this.connected = false;
    }

    public void StopSendingFaceIDState() {
        this.publishStateTimer.Stop();
    }

    // Registers a message handler for a particular command the app might receive from the controller. 
    public void RegisterHandler(FaceIDCommand command, GameState dest) {
        this.commandHandlers.Add(command, dest);
    }

    private void SetupPubSub() {
        Logger.Log("-- Setup Pub/Sub for Ros Manager --");
        string eventPubMessage = RosbridgeUtilities.GetROSJsonAdvertiseMsg(
            Constants.FACEID_EVENT_TOPIC, Constants.FACEID_EVENT_MESSAGE_TYPE);
        string statePubMessage = RosbridgeUtilities.GetROSJsonAdvertiseMsg(
            Constants.FACEID_STATE_TOPIC, Constants.FACEID_STATE_MESSAGE_TYPE);   
        string subMessage = RosbridgeUtilities.GetROSJsonSubscribeMsg(
            Constants.FACEID_COMMAND_TOPIC, Constants.FACEID_COMMAND_MESSAGE_TYPE);

        // Send all advertisements to publish and subscribe to appropriate channels.
        this.connected = this.rosClient.SendMessage(eventPubMessage) &&
            this.rosClient.SendMessage(statePubMessage) &&
            this.rosClient.SendMessage(subMessage);
    }

    private void OnMessageReceived(object sender, int cmd, object properties) {
        Logger.Log("ROS Manager received and will handle message for command " + cmd);

        FaceIDCommand command = (FaceIDCommand)Enum.Parse(typeof(FaceIDCommand), cmd.ToString());

        // First need to decode, then do something with it. 
        if (this.commandHandlers.ContainsKey(command)) {
            if (properties == null) {
                gameController.AddTask(commandHandlers[command]);
            } else {
                gameController.AddTask(commandHandlers[command], (Dictionary<string, object>)properties);
            }
        } else {
            // Fail fast! Failure here means FaceIDCommand struct is not up to date.
            throw new Exception("Don't know how to handle this command: " + command);
        }
    }

    //
    // Note that these all return Action so that they can be set as click handlers.
    //

    // Simple message to verify connection when we initialize connection to ROS.
    public Action SendHelloWorldAction() {
        return () => {
            this.SendEventMessageToController(FaceIDEventType.HELLO_WORLD, "");
            Logger.Log("Sent hello world action");
        };
    }

    // TODO: add various Actions here that represent the app's "public" state

    // Send FaceIDEvent message until received, in a new thread.
    private void SendEventMessageToController(FaceIDEventType messageType, string message) {
        Thread t = new Thread(() => {
            Dictionary<string, object> publish = new Dictionary<string, object>();
            publish.Add("topic", Constants.FACEID_EVENT_TOPIC);
            publish.Add("op", "publish");
            // Build data to send.
            Dictionary<string, object> data = new Dictionary<string, object>();
            data.Add("event_type", (int)messageType);
            data.Add("header", RosbridgeUtilities.GetROSHeader());
            data.Add("message", message);
            publish.Add("msg", data);
            Logger.Log("Sending event ROS message: " + Json.Serialize(publish));
            bool sent = false;
            while (!sent) {
                Logger.Log("Sending again...");
                sent = this.rosClient.SendMessage(Json.Serialize(publish));
            }    
        });
        t.Start();
    }

    // Public wrapper to send FaceID state at a specific time, when a timely update
    // is necessary. For example, after next page, need to make sure controller has
    // seen an updated evaluating_sentence_index before trying to send the next sentence.
    public void SendFaceIDState() {
        this.SendFaceIDState(null, null);
    }

    // Send a message representing FaceID state to the controller.
    // Doesn't need to return Action because it's only used as a timer elapsed handler.
    private void SendFaceIDState(object _, System.Timers.ElapsedEventArgs __) {
        Dictionary<string, object> publish = new Dictionary<string, object>();
        publish.Add("topic", Constants.FACEID_STATE_TOPIC);
        publish.Add("op", "publish");

        // TODO: could devise a better scheme to make sure states are sent in order.
        // Can also use the sequence numbers provided in the header. Probably overkill.
        Dictionary<string, object> data = FaceIDStateManager.GetRosMessageData();
        data.Add("header", RosbridgeUtilities.GetROSHeader());
        // Don't allow audio_file to be null, ROS will get upset.
        if (data["audio_file"] == null) {
            data["audio_file"] = "";
        }
        publish.Add("msg", data);

        bool success = this.rosClient.SendMessage(Json.Serialize(publish));
        if (!success) {
            // Logger.Log("Failed to send FaceIDState message: " + Json.Serialize((publish)));
        }       
    }
}
