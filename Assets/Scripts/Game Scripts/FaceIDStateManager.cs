using System;

// This singleton class manages the StorybookState that is published to the controller.
using System.Collections.Generic;
using Messages.face_id_app_msgs;

public static class FaceIDStateManager {

    private static FaceIDState currentState;
    private static Dictionary<string, object> rosMessageData;

    public static void Init() { 
        // Set default values for start of interaction.
        currentState = new FaceIDState {
            is_logged_in = false,
            is_trained = false,
            session_num = -1
        };

        rosMessageData = new Dictionary<string, object>();
        rosMessageData.Add("is_logged_in", currentState.is_logged_in);
        rosMessageData.Add("is_trained", currentState.is_trained);
        rosMessageData.Add("session_num", currentState.session_num);
    }

    public static FaceIDState GetState() {
        return currentState;
    }

    public static Dictionary<string, object> GetRosMessageData() {
        return new Dictionary<string, object>(rosMessageData);
    }

    public static void SetLoggedIn(bool loggedIn)
    {
        currentState.is_logged_in = loggedIn;
        rosMessageData["is_logged_in"] = loggedIn;
    }

    public static void SetIsTrained(bool trained)
    {
        currentState.is_trained = trained;
        rosMessageData["is_trained"] = trained;
    }

    public static void SetSessionNum(sbyte num)
    {
        currentState.session_num = num;
        rosMessageData["session_num"] = num;
    }

}


