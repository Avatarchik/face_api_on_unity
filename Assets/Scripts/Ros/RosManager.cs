// This class is a high level abstraction of dealing with Ros, to separate ROS
// logic from GameController.
//
// RosManager heavily relies on RosbridgeUtilities and RosbridgeWebSocketClient.

using UnityEngine;
using System;
using System.Threading;
using System.Collections.Generic;
using MiniJSON;
using Messages.face_id_app_msgs;
using Messages.face_msgs;

public class RosManager
{
    
    private readonly GameController gameController; // Keep a reference to the game controller.
    private readonly RosbridgeWebSocketClient rosClient;
    // TODO: note that for now only one handler can be registered per command.
    // note: the sbyte key is expected to correspond to the constants in Messages.face_id_app_msgs.FaceIDCommand 
    private Dictionary<sbyte, GameState> commandHandlers;
    private bool connected;

    System.Timers.Timer publishStateTimer =
        new System.Timers.Timer(Constants.FACEID_STATE_PUBLISH_DELAY_MS);

    // Constructor.
    public RosManager(string rosIP, string portNum, GameController gameController) {
        Logger.Log("RosManager constructor");
        this.gameController = gameController;

        this.rosClient = new RosbridgeWebSocketClient(rosIP, portNum);
        this.rosClient.receivedMsgEvent += this.OnMessageReceived;
        this.commandHandlers = new Dictionary<sbyte, GameState>();
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
    // note: command is expected to correspond to the constants in Messages.face_id_app_msgs.FaceIDCommand
    public void RegisterHandler(sbyte command, GameState dest) {
        this.commandHandlers.Add(command, dest);
    }

    private void SetupPubSub() {
        Logger.Log("-- Setup Pub/Sub for Ros Manager --");
        string eventPubMessage = RosbridgeUtilities.GetROSJsonAdvertiseMsg(
            Constants.FACEID_EVENT_TOPIC, Constants.FACEID_EVENT_MESSAGE_TYPE);
        string statePubMessage = RosbridgeUtilities.GetROSJsonAdvertiseMsg(
            Constants.FACEID_STATE_TOPIC, Constants.FACEID_STATE_MESSAGE_TYPE);
        string trainingStatePubMessage = RosbridgeUtilities.GetROSJsonAdvertiseMsg(
            Constants.FACEID_TRAINING_STATE_TOPIC, Constants.FACEID_TRAINING_STATE_MESSAGE_TYPE);
        string apiRequestMessage = RosbridgeUtilities.GetROSJsonAdvertiseMsg(
            Constants.FACEAPIREQUEST_TOPIC, Constants.FACEAPIREQUEST_MESSAGE_TYPE);
        string apiResponseMessage = RosbridgeUtilities.GetROSJsonAdvertiseMsg(
            Constants.FACEAPIRESPONSE_TOPIC, Constants.FACEAPIRESPONSE_MESSAGE_TYPE);
        string subMessage = RosbridgeUtilities.GetROSJsonSubscribeMsg(
            Constants.FACEID_COMMAND_TOPIC, Constants.FACEID_COMMAND_MESSAGE_TYPE);

        // Send all advertisements to publish and subscribe to appropriate channels.
        this.connected = this.rosClient.SendMessage(eventPubMessage) &&
            this.rosClient.SendMessage(statePubMessage) &&
            this.rosClient.SendMessage(subMessage) &&
            this.rosClient.SendMessage(trainingStatePubMessage) &&
            this.rosClient.SendMessage(apiRequestMessage) &&
            this.rosClient.SendMessage(apiResponseMessage);
    }

    private void OnMessageReceived(object sender, int cmd, object properties) {
        Logger.Log("ROS Manager received and will handle message for command #" + cmd);

        sbyte convertedCmd = (sbyte)cmd;
        // First need to decode, then do something with it. 
        if (this.commandHandlers.ContainsKey(convertedCmd)) {
            if (properties == null) {
                gameController.AddTask(commandHandlers[convertedCmd]);
            } else {
                gameController.AddTask(commandHandlers[convertedCmd], (Dictionary<string, object>)properties);
            }
        } else {
            // Fail fast! Failure here means FaceIDCommand struct is not up to date.
            throw new Exception("Don't know how to handle this command: #" + cmd);
        }
    }

    //
    // Note that these all return Action so that they can be set as click handlers.
    //

    // Simple message to verify connection when we initialize connection to ROS.
    public Action SendHelloWorldAction() {
        return () => {
            FaceIDEvent idEvent = new FaceIDEvent
            {
                event_type = FaceIDEvent.HELLO_WORLD,
                message = ""
            };
            this.SendEventMessageToController(idEvent);
            Logger.Log("Sent hello world action");
        };
    }

    public Action SendGroupIDRequestAction()
    {
        return () => {
            FaceIDEvent idEvent = new FaceIDEvent
            {
                event_type = FaceIDEvent.REQUEST_GROUP_ID,
                message = ""
            };
            this.SendEventMessageToController(idEvent);
            Logger.Log("Sent Group ID request action");
        };
    }

    public Action SendAcceptLoginAction(string loginName, int sessionNum)
    {
        return () => {

            string serialized = Json.Serialize(new Dictionary<string, object>
            {
                { "loginName", loginName },
                { "sessionNum", sessionNum }
            });

            FaceIDEvent idEvent = new FaceIDEvent
            {
                event_type = FaceIDEvent.WELCOME_LOGIN,
                message = serialized
            };
            this.SendEventMessageToController(idEvent);
            Logger.Log("Sent Accept Login request action");
        };
    }

    public Action SendRejectLoginAction(string attemptedLogin, bool knownPerp, string perpName="")
    {
        return () => {

            string serialized = Json.Serialize(new Dictionary<string, object>
            {
                { "attemptedLogin", attemptedLogin },
                { "knownPerp", knownPerp },
                { "perpName", perpName }
            });

            FaceIDEvent idEvent = new FaceIDEvent
            {
                event_type = FaceIDEvent.REJECT_LOGIN,
                message = serialized
            };
            this.SendEventMessageToController(idEvent);
            Logger.Log("Sent Reject Login request action");
        };
    }


    // TODO: add various Actions here that represent the app's "public" state

    // Send FaceIDEvent message until received, in a new thread.
    private void SendEventMessageToController(FaceIDEvent eventMsg) {
        Thread t = new Thread(() => {
            Dictionary<string, object> publish = new Dictionary<string, object>();
            publish.Add("topic", Constants.FACEID_EVENT_TOPIC);
            publish.Add("op", "publish");
            // Build data to send.
            Dictionary<string, object> data = new Dictionary<string, object>();
            data.Add("header", RosbridgeUtilities.GetROSHeader());
            data.Add("event_type", eventMsg.event_type);
            data.Add("message", eventMsg.message);
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

    // Send a message representing a FaceAPI Request to the controller.
    // Sends until success, in a new thread.
    public void SendFaceAPIRequestAction(FaceAPIRequest apiRequest)
    {
        Thread thread = new Thread(() => {
            Dictionary<string, object> publish = new Dictionary<string, object>();
            publish.Add("topic", Constants.FACEAPIREQUEST_TOPIC);
            publish.Add("op", "publish");

            Dictionary<string, object> data = new Dictionary<string, object>();
            data.Add("header", RosbridgeUtilities.GetROSHeader());
            data.Add("app", Constants.FACE_MSGS_APP_NAME);
            data.Add("location", Constants.FACE_MSGS_LOCATION);
            data.Add("api_subscription_key", Util.ReadJsonParamFromStr(Constants.API_ACCESS_KEY, "subscriptionKey"));
            data.Add("request_method", apiRequest.request_method);
            data.Add("request_type", apiRequest.request_type);

            data.Add("content_type", apiRequest.content_type);
            data.Add("request_parameters", apiRequest.request_parameters);
            data.Add("request_body", apiRequest.request_body);

            publish.Add("msg", data);
            Logger.Log("Sending FaceAPIRequest ROS message: " + Json.Serialize(publish));
            bool sent = false;
            while (!sent)
            {
                sent = this.rosClient.SendMessage(Json.Serialize(publish));
            }
            Logger.Log("Successfully sent FaceAPIRequest ROS message.");
        });
        thread.Start();
    }

    // Send a message representing a FaceAPI Response to the controller.
    // Sends until success, in a new thread.
    public void SendFaceAPIResponseAction(FaceAPIResponse apiResponse)
    {
        Thread thread = new Thread(() => {
            Dictionary<string, object> publish = new Dictionary<string, object>();
            publish.Add("topic", Constants.FACEAPIRESPONSE_TOPIC);
            publish.Add("op", "publish");

            Dictionary<string, object> data = new Dictionary<string, object>();
            data.Add("header", RosbridgeUtilities.GetROSHeader());
            data.Add("app", Constants.FACE_MSGS_APP_NAME);
            data.Add("response_type", apiResponse.response_type);
            data.Add("response", apiResponse.response);

            publish.Add("msg", data);
            Logger.Log("Sending FaceAPIResponse ROS message: " + Json.Serialize(publish));
            bool sent = false;
            while (!sent)
            {
                sent = this.rosClient.SendMessage(Json.Serialize(publish));
            }
            Logger.Log("Successfully sent FaceAPIResponse ROS message.");
        });
        thread.Start();
    }

    public void SendTrainingStateAction(FaceIDTraining msg)
    {
        Thread thread = new Thread(() => {
            Dictionary<string, object> publish = new Dictionary<string, object>();
            publish.Add("topic", Constants.FACEID_TRAINING_STATE_TOPIC);
            publish.Add("op", "publish");

            Dictionary<string, object> data = new Dictionary<string, object>();
            data.Add("header", RosbridgeUtilities.GetROSHeader());
            data.Add("event_type", msg.event_type);
            data.Add("p_name", msg.p_name);
            data.Add("object_name", msg.object_name);

            publish.Add("msg", data);
            Logger.Log("Sending FaceIDTraining ROS message: " + Json.Serialize(publish));
            bool sent = false;
            while (!sent)
            {
                sent = this.rosClient.SendMessage(Json.Serialize(publish));
            }
            Logger.Log("Successfully sent FaceIDTraining ROS message.");
        });
        thread.Start();
    }

}
