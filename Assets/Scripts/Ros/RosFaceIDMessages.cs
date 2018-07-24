/* 
 * This file defines helper structs and enums for building and interpreting ROS messages.
 *
 * IMPORTANT:
 *
 * They need to be manually kept consistent with the unity_game_controllers/msgs/*.msg files,
 * otherwise messages will be malformatted and ROS will not send them (and since we are using
 * rosbridge, these failures will be silent, which, to quote our President, is BAD).
 *
 */

// Messages from the FaceID App to the controller.
public enum FaceIDEventType {
    HELLO_WORLD = 0
}

// Messages coming from the controller to the FaceID App.
// We will need to deal with each one by registering a handler.
public enum FaceIDCommand {
    HELLO_WORLD_ACK = 0 // No params.
}

// Message type representing the high level state of the FaceID App, to be published at 10Hz.
public struct FaceIDState
{
    public bool isLoggedIn;
    public bool isTrained;
    public int sessionNum;
}

public struct FaceAPIRequest
{
    public static readonly string app = Constants.FACE_MSGS_APP_NAME;
    public static readonly string location = Constants.FACE_MSGS_LOCATION;
    public static readonly string api_subscription_key = Util.ReadJsonParamFromStr(Constants.API_ACCESS_KEY, "subscriptionKey");

    public FaceAPIReqMethod request_method;
    public FaceAPIReqType request_type;
    public FaceAPIReqContentType content_type;
    public string request_parameters;
    public byte[] request_body;
}

public struct FaceAPIResponse
{
    public static readonly string app = Constants.FACE_MSGS_APP_NAME;
    public FaceAPIRespType response_type;
    public string response;
}

public enum FaceAPIReqMethod
{
    HTTP_POST = 0,
    HTTP_PUT = 1,
    HTTP_DELETE = 2,
    HTTP_GET = 3,
    HTTP_PATCH = 4
}

public enum FaceAPIReqContentType
{
    CONTENT_JSON,
    CONTENT_STREAM
}

public enum FaceAPIReqType
{
    FACE_DETECT = 0,
    FACE_FINDSIMILAR = 1,
    FACE_GROUP = 2,
    FACE_IDENTIFY = 3,
    FACE_VERIFY = 4,

    FACELIST_ADDFACE = 5,
    FACELIST_CREATE = 6,
    FACELIST_DELETE = 7,
    FACELIST_DELETEFACE = 8,
    FACELIST_GET = 9,
    FACELIST_LIST = 10,
    FACELIST_UPDATE = 11,

    LARGEFACELIST_ADDFACE = 12,
    LARGEFACELIST_CREATE = 13,
    LARGEFACELIST_DELETE = 14,
    LARGEFACELIST_DELETEFACE = 15,
    LARGEFACELIST_GET = 16,
    LARGEFACELIST_GETFACE = 17,
    LARGEFACELIST_GETTRAININGSTATUS = 18,
    LARGEFACELIST_LIST = 19,
    LARGEFACELIST_LISTFACE = 20,
    LARGEFACELIST_TRAIN = 21,
    LARGEFACELIST_UPDATE = 22,
    LARGEFACELIST_UPDATEFACE = 23,

    LARGEPERSONGROUP_CREATE = 24,
    LARGEPERSONGROUP_DELETE = 25,
    LARGEPERSONGROUP_GET = 26,
    LARGEPERSONGROUP_GETTRAININGSTATUS = 27,
    LARGEPERSONGROUP_LIST = 28,
    LARGEPERSONGROUP_TRAIN = 29,
    LARGEPERSONGROUP_UPDATE = 30,

    LARGEPERSONGROUPPERSON_ADDFACE = 31,
    LARGEPERSONGROUPPERSON_CREATE = 32,
    LARGEPERSONGROUPPERSON_DELETE = 33,
    LARGEPERSONGROUPPERSON_DELETEFACE = 34,
    LARGEPERSONGROUPPERSON_GET = 35,
    LARGEPERSONGROUPPERSON_GETFACE = 36,
    LARGEPERSONGROUPPERSON_LIST = 37,
    LARGEPERSONGROUPPERSON_UPDATE = 38,
    LARGEPERSONGROUPPERSON_UPDATEFACE = 39,

    PERSONGROUP_CREATE = 40,
    PERSONGROUP_DELETE = 41,
    PERSONGROUP_GET = 42,
    PERSONGROUP_GETTRAININGSTATUS = 43,
    PERSONGROUP_LIST = 44,
    PERSONGROUP_TRAIN = 45,
    PERSONGROUP_UPDATE = 46,

    PERSONGROUPPERSON_ADDFACE = 47,
    PERSONGROUPPERSON_CREATE = 48,
    PERSONGROUPPERSON_DELETE = 49,
    PERSONGROUPPERSON_DELETEFACE = 50,
    PERSONGROUPPERSON_GET = 51,
    PERSONGROUPPERSON_GETFACE = 52,
    PERSONGROUPPERSON_LIST = 53,
    PERSONGROUPPERSON_UPDATE = 54,
    PERSONGROUPPERSON_UPDATEFACE = 55
}

public enum FaceAPIRespType
{
    RSP_200_SUCCESS_GENERAL = 200,
    RSP_202_SUCCESS_TRAINING = 202,

    RSP_400_FAIL_ARGUMENT = 400,
    RSP_401_FAIL_SUBKEY = 401,
    RSP_403_FAIL_QUOTA = 403,
    RSP_404_FAIL_NOTFOUND = 404,
    RSP_408_FAIL_TIMEOUT = 408,
    RSP_409_FAIL_RESOURCECONFLICT = 409,
    RSP_415_FAIL_UNSUPPORTEDMEDIA = 415,
    RSP_429_FAIL_RATELIMIT = 429
}

// Messages from the storybook to the controller.
/*public enum StorybookEventType {
    HELLO_WORLD = 0,
    SPEECH_ACE_RESULT = 1, // Message is {page_num: int, index: int, text: string, duration: float,
                           // speechace: json string}
    WORD_TAPPED = 2, // Message is {index: int, word: string, phrase: string} of the tinkertext.
    SCENE_OBJECT_TAPPED = 3, // Message is {id: int, label: string} of the scene object.
    SENTENCE_SWIPED = 4, // Message is {index: int, text: string} of the stanza.
    RECORD_AUDIO_COMPLETE = 5, // Message is index of sentence. 
    STORY_SELECTED = 6, // Message is {needs_download: bool, target_words: [string]}.
    STORY_LOADED = 7, // Message is {continue_midway: bool}.
    CHANGE_MODE = 8, // Message is {mode: int}
    REPEAT_END_PAGE_QUESTION = 9, // Message is empty.
    END_STORY = 10, // Message is empty. Happens in explore mode when we reach "The End" page.
    RETURN_TO_LIBRARY_EARLY= 11, // Message is empty.
}

// Messages coming from the controller to the storybook.
// We will need to deal with each one by registering a handler.
public enum StorybookCommand {
    HELLO_WORLD_ACK = 0, // No params.
    HIGHLIGHT_WORD = 1, // Params is {indexes: [int], unhighlight: bool, stay_on: bool},
                        // words to highlight, whether this is an "off" command,
                        // whether should stay on forever
    HIGHLIGHT_SCENE_OBJECT = 2, // Params is {ids: [int]}, scene object to highlight.
    SHOW_NEXT_SENTENCE = 3, // Params is {index: int, child_turn: bool, record: bool}.
    START_RECORD = 4, // Params is {index: int, oneshot: bool}. Which index, and whether
                      // this is during the normal course of reading the page or just for a specific
                      // sentence. (Controls whether the next sentence autoshows).
    CANCEL_RECORD = 5, // Stop and discard the recording. Params is empty.
    GO_TO_PAGE = 6, // Params is {page_number: int}. Used for starting story midway through.
    NEXT_PAGE = 7, // Params is empty.
    GO_TO_END_PAGE = 8, // Params is empty.
    SHOW_LIBRARY_PANEL = 9, // Params is empty.
    HIGHLIGHT_ALL_SENTENCES = 10, // Params is empty.
}

// Message type representing the high level state of the storybook, to be published at 10Hz.
public struct StorybookState {
    public bool audioPlaying; // Is an audio file playing?
    public string audioFile; // Name of the audio file that's playing, if there is one.

    public StorybookMode storybookMode; // See Constants.StorybookMode.

    public string currentStory;
    public int numPages;
     
    public int evaluatingSentenceIndex; // If in Evaluate mode, this will be which sentence we're on.
}

// Message type representing which page of the storybook is currently active.
public struct StorybookPageInfo {
    public string storyName;
    public int pageNumber; // 0-indexed, where 0 is the title page.
    public string[] sentences;
    public StorybookSceneObject[] sceneObjects;
    public StorybookTinkerText[] tinkerTexts;
    public JiboPrompt[] prompts;
}

// To be nested inside of StorybookPageInfo.
// Represents info about a scene object on the page.
public struct StorybookSceneObject {
    public int id;
    public string label;
    public bool inText;
}

// To be nested inside of StorybookPageInfo.
// Represents info about a tinkertext on the page.
public struct StorybookTinkerText {
    public bool hasSceneObject;
    public int sceneObjectId;
    public string word;
}*/
    