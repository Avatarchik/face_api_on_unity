using System;

// This singleton class manages the StorybookState that is published to the controller.
using System.Collections.Generic;


public static class FaceIDStateManager {


    private static FaceIDState currentState;
    private static Dictionary<string, object> rosMessageData;

    public static void Init() {
            
        // Set default values for start of interaction.
        currentState = new FaceIDState {
            isLoggedIn = false,
            isTrained = false,
            sessionNum = -1
        };

        rosMessageData = new Dictionary<string, object>();
        rosMessageData.Add("is_logged_in", currentState.isTrained);
        rosMessageData.Add("is_trained", currentState.isTrained);
        rosMessageData.Add("session_num", currentState.sessionNum);
    }

    public static FaceIDState GetState() {
        // It's a struct, so it should return by value.
        return currentState;
    }

    public static Dictionary<string, object> GetRosMessageData() {
        return new Dictionary<string, object>(rosMessageData);
    }

    public static void SetLoggedIn(bool loggedIn)
    {
        currentState.isLoggedIn = loggedIn;
        rosMessageData["is_logged_in"] = loggedIn;
    }

    public static void SetIsTrained(bool trained)
    {
        currentState.isTrained = trained;
        rosMessageData["is_trained"] = trained;
    }

    public static void SetSessionNum(int num)
    {
        currentState.sessionNum = num;
        rosMessageData["session_num"] = num;
    }

}


