using System.IO;

public static class Constants
{
    public static readonly string EDITOR_SAVE_PATH = Path.Combine(Directory.GetCurrentDirectory(), "ProfileData");
    public static readonly string ANDROID_SAVE_PATH = Path.Combine("sdcard", "PersonalRobotsGroup.FaceIDApp", "ProfileData");

    public static readonly string SAVE_PATH = GameController.DetermineSavePath();

    public static readonly string UNKNOWN_IMG_RSRC_PATH = Path.Combine("Stock Images", "unknown");
    public static readonly string SADFACE_IMG_RSRC_PATH = Path.Combine("Stock Images", "sad");

    public static readonly string PERSON_GROUP_ID = "unity";
    public static readonly decimal CONFIDENCE_THRESHOLD = 0.70m;    // decimal between 0 and 1
    public static readonly int CAM_DELAY_MS = 2000;

    public static float STORYBOOK_STATE_PUBLISH_HZ = 3.0f;
    public static float STORYBOOK_STATE_PUBLISH_DELAY_MS = 1000.0f / STORYBOOK_STATE_PUBLISH_HZ;
}