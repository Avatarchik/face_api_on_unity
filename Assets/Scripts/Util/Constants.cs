using System.IO;

public static class Constants
{
    // Path.Combine is better because it supports cross-compatibility

    // "/ProfileData"
    public static readonly string EDITOR_SAVE_PATH = Path.Combine(Directory.GetCurrentDirectory(), "ProfileData");
    // "sdcard/PersonalRobotsGroup.FaceIDApp/ProfileData"
    public static readonly string ANDROID_SAVE_PATH = Path.Combine("sdcard", "PersonalRobotsGroup.FaceIDApp", "ProfileData");

    public static readonly string SAVE_PATH = GameController.DetermineSavePath();

    public static readonly string UNKNOWN_IMG_RSRC_PATH = Path.Combine("Stock Images", "unknown");
    public static readonly string SADFACE_IMG_RSRC_PATH = Path.Combine("Stock Images", "sad");

    public static readonly string PERSON_GROUP_ID = "unity";
    public static readonly decimal CONFIDENCE_THRESHOLD = 0.70m;    // decimal between 0 and 1
    public static readonly int CAM_DELAY_MS = 2000;
    public static readonly int ROS_CONNECT_DELAY_MS = 1000;

    public static float FACEID_STATE_PUBLISH_HZ = 3.0f;
    public static float FACEID_STATE_PUBLISH_DELAY_MS = 1000.0f / FACEID_STATE_PUBLISH_HZ;

    // ROS connection information.
    public static string DEFAULT_ROSBRIDGE_IP = "192.168.1.166";
    public static string DEFAULT_ROSBRIDGE_PORT = "9090";

    // ROS topics.
    // FaceID to Roscore
    public static string FACEID_EVENT_TOPIC = "/faceid_event";
    public static string FACEID_EVENT_MESSAGE_TYPE = "/unity_game_msgs/FaceIDEvent";
    public static string FACEID_STATE_TOPIC = "/faceid_state";
    public static string FACEID_STATE_MESSAGE_TYPE = "/unity_game_msgs/FaceIDState";

    // Roscore to FaceID
    public static string FACEID_COMMAND_TOPIC = "/faceid_command";
    public static string FACEID_COMMAND_MESSAGE_TYPE = "/unity_game_msgs/FaceIDCommand";

}