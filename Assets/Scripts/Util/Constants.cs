using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Messages.face_id_app_msgs;
using System;

public static class Constants
{
    public static readonly string UNITY_ROSCONNECTION_SCENE = "ROS Connection";
    public static readonly string UNITY_GAME_SCENE = "Game";

    // Path.Combine is better because it supports cross-compatibility

    // "./PersonalRobotsGroup.FaceIDApp/ProfileData"
    public static readonly string EDITOR_SAVE_PATH = Path.Combine(Directory.GetCurrentDirectory(), "PersonalRobotsGroup.FaceIDApp", "ProfileData");
    // "sdcard/PersonalRobotsGroup.FaceIDApp/ProfileData"
    public static readonly string ANDROID_SAVE_PATH = Path.Combine("sdcard", "PersonalRobotsGroup.FaceIDApp", "ProfileData");

    public static readonly string SAVE_PATH = DetermineSavePath();

    public static readonly string UNKNOWN_IMG_RSRC_PATH = Path.Combine("Stock Images", "unknown");

    public static readonly string INFO_FILE = "info.txt";
    public static readonly string GRP_INFO_FILE = "group_info.txt";

    public static readonly string API_ACCESS_KEY_PATH = "api_access_key";
    public static readonly decimal CONFIDENCE_THRESHOLD = 0.70m;    // decimal between 0 and 1
    public static readonly string IMAGE_LABEL = "Image";
    public static readonly string DELETED_IMG_LABEL = "deleted";

    // Training constants

    public static readonly int TRAINING_NUM_PER_POSITION = 1;
    public static readonly int TRAINING_CAM_DELAY_MS = 250;
    public static readonly int TRAINING_DELAY_BETWEEN_PICS_MS = 75;
    public static readonly int TRAINING_IMG_ROT_DELAY_MS = 250;

    // key: location (from FaceIDTraining msg); value: 2-val str array. val 0: path to image; val 1: object name
    public static readonly Dictionary<sbyte, string[]> TRAINING_OBJ_NAME_DICT = new Dictionary<sbyte, string[]>
    {
        {FaceIDTraining.TRAINING_LOC_TOP_LEFT, new string[] {Path.Combine("Training Images", "bulbasaur"), "Bulbasaur"}},
        {FaceIDTraining.TRAINING_LOC_TOP_MID, new string[] {Path.Combine("Training Images", "charmander"), "Charmander"}},
        {FaceIDTraining.TRAINING_LOC_TOP_RIGHT, new string[] {Path.Combine("Training Images", "squirtle"), "Squirtle"}},
        {FaceIDTraining.TRAINING_LOC_MID_LEFT, new string[] {Path.Combine("Training Images", "eevee"), "Eevee"}},
        {FaceIDTraining.TRAINING_LOC_MID_MID, new string[] {Path.Combine("Training Images", "pikachu"), "Pikachu"}},
        {FaceIDTraining.TRAINING_LOC_MID_RIGHT, new string[] {Path.Combine("Training Images", "snorlax"), "Snorlax"}},
        {FaceIDTraining.TRAINING_LOC_BOT_LEFT, new string[] {Path.Combine("Training Images", "treecko"), "Treecko"}},
        {FaceIDTraining.TRAINING_LOC_BOT_MID, new string[] {Path.Combine("Training Images", "torchic"), "Torchic"}},
        {FaceIDTraining.TRAINING_LOC_BOT_RIGHT, new string[] {Path.Combine("Training Images", "mudkip"), "Mudkip"}}
    };

    public static readonly float W_MULT = 1/3.0f, H_MULT = 1 / 3.0f;

    // key: location (from FaceIDTraining msg); value: Vector2 to calculate positioning
    // Dict's value Vector is scaled (component-multiplied) with Vector2(panel_width, panel_height) to get scaled shift value
    // ((TRAINING_OBJ_LOC_DICT[key] scale Vector2(panel_width, panel_height)) + center_of_parent_panel) = position of object
    public static readonly Dictionary<sbyte, Vector2> TRAINING_OBJ_LOC_DICT = new Dictionary<sbyte, Vector2>
    {
        {FaceIDTraining.TRAINING_LOC_TOP_LEFT,  new Vector2(-W_MULT, +H_MULT)}, // (-,+)
        {FaceIDTraining.TRAINING_LOC_TOP_MID,   new Vector2(+0.000f, +H_MULT)}, // (0,+)
        {FaceIDTraining.TRAINING_LOC_TOP_RIGHT, new Vector2(+W_MULT, +H_MULT)}, // (+,+)
        {FaceIDTraining.TRAINING_LOC_MID_LEFT,  new Vector2(-W_MULT, +0.000f)}, // (-,0)
        {FaceIDTraining.TRAINING_LOC_MID_MID,   new Vector2(+0.000f, +0.000f)}, // (0,0)
        {FaceIDTraining.TRAINING_LOC_MID_RIGHT, new Vector2(+W_MULT, +0.000f)}, // (+,0)
        {FaceIDTraining.TRAINING_LOC_BOT_LEFT,  new Vector2(-W_MULT, -H_MULT)}, // (-,-)
        {FaceIDTraining.TRAINING_LOC_BOT_MID,   new Vector2(+0.000f, -H_MULT)}, // (0,-)
        {FaceIDTraining.TRAINING_LOC_BOT_RIGHT, new Vector2(+W_MULT, -H_MULT)}, // (+,-)
    };

    // ROS connection information.
    public static readonly bool USE_ROS = true;
    public static readonly string DEFAULT_ROSBRIDGE_IP = "192.168.1.236";
    public static readonly string DEFAULT_ROSBRIDGE_PORT = "9090";
    public static readonly int ROS_CONNECT_DELAY_MS = 1000;

    // face_msgs info
    public static readonly string FACE_MSGS_APP_NAME = "Face ID - Unity";
    public static readonly string FACE_MSGS_LOCATION = "eastus";

    public static readonly float FACEID_STATE_PUBLISH_HZ = 3.0f;
    public static readonly float FACEID_STATE_PUBLISH_DELAY_MS = 1000.0f / FACEID_STATE_PUBLISH_HZ;

    // ROS topics.
    // FaceID to Roscore
    public static readonly string FACEID_EVENT_TOPIC = "/faceid_event";
    public static readonly string FACEID_EVENT_MESSAGE_TYPE = "/unity_game_msgs/FaceIDEvent";
    public static readonly string FACEID_STATE_TOPIC = "/faceid_state";
    public static readonly string FACEID_STATE_MESSAGE_TYPE = "/unity_game_msgs/FaceIDState";
    public static readonly string FACEID_TRAINING_STATE_TOPIC = "/faceid_training";
    public static readonly string FACEID_TRAINING_STATE_MESSAGE_TYPE = "/unity_game_msgs/FaceIDTraining";

    public static readonly string FACEAPIREQUEST_TOPIC = "/faceapi_requests";
    public static readonly string FACEAPIREQUEST_MESSAGE_TYPE = "/face_msgs/FaceAPIRequest";
    public static readonly string FACEAPIRESPONSE_TOPIC = "/faceapi_responses";
    public static readonly string FACEAPIRESPONSE_MESSAGE_TYPE = "/face_msgs/FaceAPIResponse";


    // Roscore to FaceID
    public static readonly string FACEID_COMMAND_TOPIC = "/faceid_command";
    public static readonly string FACEID_COMMAND_MESSAGE_TYPE = "/unity_game_msgs/FaceIDCommand";

    // old constants (commented):
  /*public static readonly string PERSON_GROUP_ID = "unity";
    public static readonly int CAM_DELAY_MS = 2000;
    public static readonly string IMAGE_DISPLAY_LABEL = "Photo";
    public static readonly string SADFACE_IMG_RSRC_PATH = Path.Combine("Stock Images", "sad");*/

    private static string DetermineSavePath()
    {
        string ret = Constants.EDITOR_SAVE_PATH;

#if UNITY_ANDROID
        Debug.Log("Unity Android Detected");
        ret = Constants.ANDROID_SAVE_PATH;
#endif

        return ret;
    }
}