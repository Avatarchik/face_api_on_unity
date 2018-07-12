using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityFaceIDHelper;

public class GameController : MonoBehaviour {
    
    // The singleton instance.
    public static GameController instance = null;

    private Queue<Task> taskQueue = new Queue<Task>();

    private RosManager rosManager;

    public Canvas uiElementContainer;

    private GameState currentState;

    private UIAdjuster adjuster;

    private string loggedInName, folderName;    //will always be the same, unless theres > 1 of the same loggedInName
    private Dictionary<string, string> profileInfo;
    private bool multipleNames = false;

    private Sprite savedFrame;
    private string savedFrameDir;
    private FaceAPIHelper apiHelper;

    void Awake()
    {
        // Enforce singleton pattern.
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Debug.Log("duplicate GameController, destroying");
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);
    }

    // Use this for initialization
    void Start()
    {
        adjuster = uiElementContainer.gameObject.GetComponent<UIAdjuster>();
        TextAsset api_access_key = Resources.Load("api_access_key") as TextAsset;
        apiHelper = new FaceAPIHelper(api_access_key.text, Constants.PERSON_GROUP_ID);

        SetState(GameState.GAMECONTROLLER_STARTING);

        AddTask(GameState.ROS_CONNECTION);

        if (!Directory.Exists(Constants.SAVE_PATH))
        {
            Directory.CreateDirectory(Constants.SAVE_PATH);
        }
	}
	
	// Update is called once per frame
	async void Update()
    {
        //Debug.Log("Current gameState: " + this.GetGameState());
        await HandleTaskQueue();
	}

    // Handle main task queue.
    private async Task HandleTaskQueue()
    {
        // Pop tasks from the task queue and perform them.
        // Tasks are added from other threads, usually in response to ROS msgs.
        if (this.taskQueue.Count > 0)
        {
            try
            {
                Debug.Log("Got a task from queue in GameController");
                await this.taskQueue.Dequeue();
            }
            catch (Exception e)
            {
                Debug.LogError("Error invoking Task on main thread!\n" + e);
            }
        }
    }

    public void AddTask(GameState state)
    {
        Task toQueue = null;
        switch (state)
        {
            case GameState.ROS_CONNECTION:              toQueue = this.OpenROSConnectScreen(); break;

            case GameState.STARTED:                     toQueue = this.StartGame(); break;
            case GameState.NEW_PROFILE_PROMPT:          toQueue = this.AskNewProfile(); break;
            case GameState.MUST_LOGIN_PROMPT:           toQueue = this.ShowMustLogin(); break;
            case GameState.ENTER_NAME_PROMPT:           toQueue = this.AskForNewProfileName(); break;
            case GameState.EVALUATING_TYPED_NAME:       toQueue = this.EvaluateTypedNameAsync(); break;
            case GameState.LISTING_IMAGES:              toQueue = this.ShowPicturesForProfile(); break;
            case GameState.TAKING_WEBCAM_PIC:           toQueue = this.OpenWebcamForPictureAsync(); break;
            case GameState.CHECKING_TAKEN_PIC:          toQueue = this.CheckPictureTakenAsync(); break;
            case GameState.PIC_APPROVAL:                toQueue = this.ShowImgApprovalPage(); break;
            case GameState.PIC_DISAPPROVAL:             toQueue = this.ShowImgDisapprovalPage(); break;
            case GameState.SAVING_PIC:                  toQueue = this.AddImgToProfileAsync(); break;
            case GameState.LISTING_PROFILES:            toQueue = this.ListProfiles(); break;
            case GameState.LOGIN_DOUBLE_CHECK:          toQueue = this.ShowLoginDoubleCheck(); break;
            case GameState.LOGGING_IN:                  toQueue = this.LogIn(); break;
            case GameState.CANCELLING_LOGIN:            toQueue = this.CancelLogin(); break;
            case GameState.WELCOME_SCREEN:              toQueue = this.ShowWelcomeScreen(); break;
            case GameState.SHOWING_SELECTED_PHOTO:      toQueue = this.ShowSelectedPhoto(); break;
            case GameState.DELETING_PHOTO:              toQueue = this.DeletePhotoAsync(); break;
            
            case GameState.API_ERROR_CREATE:            toQueue = this.APIError(state, "(during LargePersonGroup Person creation)"); break;
            case GameState.API_ERROR_COUNTING_FACES:    toQueue = this.APIError(state, "(while counting faces)"); break;
            case GameState.API_ERROR_ADDING_FACE:       toQueue = this.APIError(state, "(while adding a face)"); break;
            case GameState.API_ERROR_IDENTIFYING:       toQueue = this.APIError(state, "(while identifying)"); break;
            case GameState.API_ERROR_GET_NAME:          toQueue = this.APIError(state, "(while trying to get name from ID after auth fail)"); break;
            case GameState.API_ERROR_TRAINING_STATUS:   toQueue = this.APIError(state, "(while checking training status)"); break;
            case GameState.API_ERROR_DELETING_FACE:     toQueue = this.APIError(state, "(while deleting a face)"); break;
        }

        if (toQueue != null)
            this.taskQueue.Enqueue(toQueue);
    }
    /*
    // HELLO_WORLD_ACK
    private void OnHelloWorldAckReceived(Dictionary<string, object> args)
    {
        Logger.Log("OnHelloWorldAckReceived");
        Constants.PARTICIPANT_ID = (string)args["participant_id"];
        this.taskQueue.Enqueue(() => {
            // Remember what the previous story name was.
            this.prevSessionStoryName = (string)args["story_name"];
            // The app should begin in explore mode, and let the controller know.
            this.goToExploreMode();
        });
    }

    // Clean up.
    void OnApplicationQuit()
    {
        if (this.rosManager != null && this.rosManager.isConnected())
        {
            // Stop the thread that's sending StorybookState messages.
            this.rosManager.StopSendingStorybookState();
            // Close the ROS connection cleanly.
            this.rosManager.CloseConnection();
        }
    }*/

    private async Task OpenROSConnectScreen()
    {
        SetState(GameState.ROS_CONNECTION);
        ClearQueuedData(); // there shouldn't be any, but just in case...
        adjuster.HideAllElements();
        SceneManager.LoadScene("ROS Connection", LoadSceneMode.Single);
    }

    private async Task StartGame()
    {
        SetState(GameState.STARTED);
        ClearQueuedData();
        adjuster.AskQuestion("\r\nHi! Are you new here?");
    }

    private async Task AskNewProfile()
    {
        SetState(GameState.NEW_PROFILE_PROMPT);
        adjuster.AskQuestion("\r\nWould you like to make a profile?");
    }

    private async Task ShowMustLogin()
    {
        SetState(GameState.MUST_LOGIN_PROMPT);
        adjuster.PromptOKDialogue("\r\nIn order to use the app, you must be logged into a profile.");
    }

    private async Task AskForNewProfileName()
    {
        SetState(GameState.ENTER_NAME_PROMPT);
        adjuster.PromptInputText("What is your name?\r\n\r\nPlease ensure that the name you enter is valid.");
    }

    private async Task EvaluateTypedNameAsync()
    {
        SetState(GameState.EVALUATING_TYPED_NAME);
        string entered = adjuster.GetTypedInput().ToLower();
        if (IsInvalidName(entered))  // conditions for an invalid name
            AddTask(GameState.ENTER_NAME_PROMPT);
        else
        {
            adjuster.PromptNoButtonPopUp("Hold on, I'm thinking... (creating LargePersonGroup Person)");
            string personID = await apiHelper.CreatePersonAsync(entered);
            if (personID != "") //successful API call
            {
                loggedInName = entered;
                folderName = entered;
                CreateProfile();
                SetPersonID(personID);
                AddTask(GameState.WELCOME_SCREEN);
            }
            else
            { // maybe internet is down, maybe api access is revoked...
                AddTask(GameState.API_ERROR_CREATE);
                Debug.LogError("API Error occurred while trying to create a LargePersonGroup Person");
            }
        }
    }

    private async Task ShowPicturesForProfile()
    {
        SetState(GameState.LISTING_IMAGES);
        Dictionary<Tuple<string, string>, string> imageList = LoadImages();
        adjuster.ListImages("Here is your photo listing:", imageList);
    }

    private async Task OpenWebcamForPictureAsync()
    {
        SetState(GameState.TAKING_WEBCAM_PIC);
        await AuthenticateIfNecessaryThenDo(() => {
            adjuster.ShowWebcam("Take a picture!", "Snap!");
        }, true);
    }

    private async Task CheckPictureTakenAsync()
    {
        SetState(GameState.CHECKING_TAKEN_PIC);
        Sprite frame = adjuster.GrabCurrentWebcamFrame();
        adjuster.PromptNoButtonPopUp("Hold on, I'm thinking... (counting faces in image)");
        byte[] imgData = frame.texture.EncodeToPNG();

        int numFaces = await apiHelper.CountFacesAsync(imgData);
        if (numFaces == -1)
        {
            AddTask(GameState.API_ERROR_COUNTING_FACES);
            Debug.LogError("API Error occurred while trying to count the faces in a frame");
            return;
        }

        if (numFaces < 1)   //pic has no detectable faces in it... try again.
            AddTask(GameState.PIC_DISAPPROVAL);
        else
        {
            await AuthenticateIfNecessaryThenDo(() => {
                savedFrame = frame;
                AddTask(GameState.PIC_APPROVAL);
            }, true, frame);
        }
    }

    private async Task ShowImgDisapprovalPage()
    {
        SetState(GameState.PIC_DISAPPROVAL);
        adjuster.PicWindow(SadFaceSprite(), "I didn't like this picture :( Can we try again?", "Try again...", "Cancel");
    }

    private async Task ShowImgApprovalPage()
    {
        SetState(GameState.PIC_APPROVAL);
        adjuster.PicWindow(savedFrame, "I like it! What do you think?", "Keep it!", "Try again...");
    }

    private async Task AddImgToProfileAsync()
    {
        SetState(GameState.SAVING_PIC);
        await AddImgToProfile();
        AddTask(GameState.LISTING_IMAGES);
    }

    private async Task ListProfiles()
    {
        SetState(GameState.LISTING_PROFILES);
        Dictionary<Tuple<string, string>, string> profiles = LoadProfiles();
        adjuster.ListProfiles("Here are the existing profiles:", profiles);
    }

    private async Task ShowLoginDoubleCheck()
    {
        SetState(GameState.LOGIN_DOUBLE_CHECK);
        Sprite pic = ImgDirToSprite(GetProfilePicDir(loggedInName));
        string displayName = FolderNameToLoginName(loggedInName);
        adjuster.PicWindow(pic, "Are you sure you want to log in as " + displayName + "?", "Login", "Back");
    }

    private async Task LogIn()
    {
        SetState(GameState.LOGGING_IN);
        LoadProfileData(loggedInName);
        await AuthenticateIfNecessaryThenDo(() => {
            AddTask(GameState.WELCOME_SCREEN);
        }, true);
    }

    private async Task CancelLogin()
    {
        SetState(GameState.CANCELLING_LOGIN);
        ClearQueuedData();
        AddTask(GameState.LISTING_PROFILES);
    }

    private async Task ShowWelcomeScreen()
    {
        SetState(GameState.WELCOME_SCREEN);
        adjuster.PromptOKDialogue("\r\nWelcome, " + loggedInName + "!");
    }

    private async Task ShowSelectedPhoto()
    {
        SetState(GameState.SHOWING_SELECTED_PHOTO);
        adjuster.PicWindow(savedFrame, "Nice picture! What would you like to do with it?", "Delete it", "Keep it");
    }

    private async Task DeletePhotoAsync()
    {
        SetState(GameState.DELETING_PHOTO);
        await AuthenticateIfNecessaryThenDo(async () => {
            await DeleteSelectedPhoto();
            this.taskQueue.Enqueue(this.ShowPicturesForProfile());
            AddTask(GameState.LISTING_IMAGES);
        }, true);
    }

    private async Task APIError(GameState newState, string err)
    {
        SetState(newState);
        adjuster.PromptOKDialogue("API Error\r\n" + err);
    }

    private async Task AuthenticateIfNecessaryThenDo(Action f, bool showRejectionPrompt, Sprite imgToCheck = null)
    {
        if (!ShouldBeAuthenticated())
        {
            f();
        }
        else
        {
            if (await VerifyAsync(showRejectionPrompt))
            {
                f();
            }
        }
    }

    private void SetState(GameState newState)
    {
        currentState = newState;
    }

    public GameState GetGameState()
    {
        return currentState;
    }

    private void CreateProfile()
    {
        if (Directory.Exists(Path.Combine(Constants.SAVE_PATH, loggedInName)))
        {
            multipleNames = true;
            int count = Directory.GetDirectories(Constants.SAVE_PATH, loggedInName + "*").Length;
            folderName = loggedInName + " (" + count + ")";
        }
        Directory.CreateDirectory(Path.Combine(Constants.SAVE_PATH, folderName));
        profileInfo = new Dictionary<string, string>();
        profileInfo.Add("personID", "..."); //todo: change this to the actual personID
        profileInfo.Add("count", "0");
        ExportProfileInfo();
    }

    //TODO: maybe change this to use Object serialization... lol
    private void ExportProfileInfo()
    {
        string savePath = Path.Combine(Constants.SAVE_PATH, folderName, "info.txt");
        List<string> dataToSave = new List<string>();
        dataToSave.Add("{");
        for (int i = 0; i < profileInfo.Keys.Count; i++)
        {
            string key = profileInfo.Keys.ElementAt(i);

            if ((i+1) < profileInfo.Keys.Count)
                dataToSave.Add("\t\"" + key + "\": \"" + profileInfo[key] + "\",");
            else
                dataToSave.Add("\t\"" + key + "\": \"" + profileInfo[key] + "\""); // last one doesn't have a comma! :P
        }
        dataToSave.Add("}");

        System.IO.File.WriteAllLines(savePath, dataToSave);
    }

    private Dictionary<Tuple<string, string>, string> LoadImages()
    {
        // this method assumes that every image in the folder
        // has already been processed by face api and is already in info.txt

        Dictionary<Tuple<string, string>, string> images = new Dictionary<Tuple<string, string>, string>();

        string path = Path.Combine(Constants.SAVE_PATH, folderName);
        string[] dirs = Directory.GetFiles(path, "*.png");
        int count = 0;
        foreach (string img in dirs)
        {
            string fullImgName = Path.GetFileName(img);
            string displayName = "Photo " + (count + 1);

            string identifier = Path.Combine(folderName, fullImgName);

            Tuple<string, string> data = new Tuple<string, string>(displayName, identifier);
            images.Add(data, img);

            count++;
        }

        return images;
    }

    private async Task AddImgToProfile()
    {
        adjuster.PromptNoButtonPopUp("Hold on, I'm thinking... (adding Face to LargePersonGroup Person)");
        Texture2D tex = savedFrame.texture;
        byte[] imgData = tex.EncodeToPNG();

        string persistedId = await apiHelper.AddFaceAsync(profileInfo["personID"], imgData);

        if (persistedId == "")
        {
            AddTask(GameState.API_ERROR_ADDING_FACE);
            Debug.LogError("API Error while trying to add Face to LargePersonGroup Person");
            return;
        }

        int count = Int32.Parse(profileInfo["count"]);
        System.IO.File.WriteAllBytes(Path.Combine(Constants.SAVE_PATH, folderName, "Image " + count + ".png"), tex.EncodeToPNG());
        profileInfo["count"] = (count + 1).ToString();
        profileInfo["Image " + count] = persistedId;
        ExportProfileInfo();
        await RetrainProfilesAsync();
    }

    private Dictionary<Tuple<string, string>, string> LoadProfiles()
    {
        try
        {
            Dictionary<Tuple<string, string>, string> profiles = new Dictionary<Tuple<string, string>, string>();
            string[] profileDirs = Directory.GetDirectories(Constants.SAVE_PATH);
            int unknownCount = 0;
            foreach (string p in profileDirs)
            {
                string fName = Path.GetFileName(p);
                string pName = FolderNameToLoginName(fName);

                string pImg = GetProfilePicDir(fName, unknownCount);

                Tuple<string, string> data = new Tuple<string, string>(pName, fName);

                profiles.Add(data, pImg);
            }
            return profiles;
        }
        catch (Exception e)
        {
            Debug.Log("Exception: " + e.ToString());
            return new Dictionary<Tuple<string, string>, string>();
        }

    }

    private string GetProfilePicDir(string fName, int unknown = 0)
    {
        string dir;

        string[] profileImgDirs = Directory.GetFiles(Path.Combine(Constants.SAVE_PATH, fName), "*.png");
        if (profileImgDirs.Length < 1)
        {
            dir = "(" + unknown + ") " + Constants.UNKNOWN_IMG_RSRC_PATH;
            unknown++;
        }
        else
            dir = profileImgDirs[0];   //could also be a random photo, or maybe in the future they can pick a "profile pic"

        return dir;
    }

    private void ClearQueuedData()
    {
        if (profileInfo != null)
            ExportProfileInfo();
        profileInfo = null;
        loggedInName = null;
        folderName = null;
        savedFrame = null;
        savedFrameDir = null;
        multipleNames = false;
    }

    public void LoginAreYouSure(string profile)
    {
        loggedInName = profile;
        AddTask(GameState.LOGIN_DOUBLE_CHECK);
    }

    private void LoadProfileData(string profName)
    {
        folderName = loggedInName;
        loggedInName = FolderNameToLoginName(folderName);
        LoadDataFile();
    }

    private string FolderNameToLoginName(string fName)
    {
        string pName = fName;
        int index = fName.IndexOf('(');
        if (index > 0)
            pName = fName.Substring(0, index - 1);
        return pName;
    }

    private void LoadDataFile()
    {
        profileInfo = ReadJsonDictFromFile(Path.Combine(Constants.SAVE_PATH, folderName, "info.txt"));
    }

    private Dictionary<string, string> ReadJsonDictFromFile(string path)
    {
        string json = System.IO.File.ReadAllText(path);
        Dictionary<string, string> data = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        return data;
    }

    public void SelectPhoto(string identifier)
    {
        savedFrame = ImgDirToSprite(Path.Combine(Constants.SAVE_PATH, identifier));
        savedFrameDir = identifier;
        AddTask(GameState.SHOWING_SELECTED_PHOTO);
    }

    private async Task DeleteSelectedPhoto()
    {
        string fileName = Path.GetFileNameWithoutExtension(savedFrameDir);
        string persistedId = profileInfo[fileName];

        adjuster.PromptNoButtonPopUp("Hold on, I'm thinking... (deleting Face from LargePersonGroup Person)");

        bool deleted = await apiHelper.DeleteFaceAsync(profileInfo["personID"], persistedId);

        if (deleted)
        {
            profileInfo[fileName] = "deleted";
            ExportProfileInfo();
            File.Delete(Path.Combine(Constants.SAVE_PATH, savedFrameDir));
            await RetrainProfilesAsync();
        }
        else
        {
            AddTask(GameState.API_ERROR_DELETING_FACE);
            Debug.LogError("API Error while trying to delete Face from LargePersonGroup Person");
            return;
        }
    }

    private void SetPersonID(string id)
    {
        profileInfo["personID"] = id;
        ExportProfileInfo();
    }

    // currently, face API verification isn't needed unless a profile has at least 5 pics
    // (the less pictures that the API is trained with, the less accurate its verifications will be)
    private bool ShouldBeAuthenticated()
    {
        int count = 0;
        foreach (KeyValuePair<string, string> entry in profileInfo)
        {
            if (entry.Key.StartsWith("Image") && entry.Value != "deleted")
                count++;

            if (count >= 5)
                return true;
        }
        return false;
    }

    private async Task<bool> VerifyAsync(bool showRejectionPrompt = false, Sprite imgToCheck = null)
    {
        adjuster.HideAllElements();
        Sprite frame;

        if (imgToCheck == null)
        {
            adjuster.EnableCamera();
            await Task.Delay(Constants.CAM_DELAY_MS); //add delay so that the camera can turn on and focus
            frame = adjuster.GrabCurrentWebcamFrame();
        }
        else
            frame = imgToCheck;

        adjuster.PromptNoButtonPopUp("Hold on, I'm thinking... (identifying faces in current frame)");
        byte[] frameData = frame.texture.EncodeToPNG();

        Dictionary<string, decimal> guesses = await apiHelper.IdentifyBiggestInImageAsync(frameData);

        if (guesses == null)
        {
            AddTask(GameState.API_ERROR_IDENTIFYING);
            Debug.LogError("API Error occurred while trying to identify LargePersonGroup Person in a frame");
            return false;
        }

        bool verified = AuthenticateLogin(guesses);
        if (verified)  //identified and above confidence threshold
        {
            return true;
        }
        else
        {
            if (showRejectionPrompt)
            {
                string response = "Are you sure you're " + loggedInName + "? Because I'm";

                if (guesses.Count == 0)
                    response += " not sure who you are, to be honest.";
                else
                {
                    adjuster.PromptNoButtonPopUp("Hold on, I'm thinking... (retrieving name(s) from personId(s))");
                    for (int i = 0; i < guesses.Count; i++)
                    {
                        string key = guesses.Keys.ElementAt(i);
                        string nameFromID = await apiHelper.GetNameFromIDAsync(key);
                        if (nameFromID == "")
                        {
                            AddTask(GameState.API_ERROR_GET_NAME);
                            Debug.LogError("API Error occurred while trying to get name from ID (after auth fail)");
                            return false;
                        }

                        if ((i + 1) == guesses.Count && guesses.Count > 1)
                            response += " and";

                        response += " " + (guesses[key] * 100) + "% sure you are " + nameFromID;

                        if ((i + 1) < guesses.Count)
                            response += ",";
                    }
                }
                adjuster.PromptOKDialogue(response);
            }
            return false;
        }
    }

    private bool AuthenticateLogin(Dictionary<string, decimal> guesses)
    {
        string personId = profileInfo["personID"];
        if (!guesses.ContainsKey(personId))
        {
            Debug.Log("guesses does not contain the personId");
            return false;
        }
        else
        {
            decimal confidence = guesses[personId];
            return confidence >= Constants.CONFIDENCE_THRESHOLD;
        }
    }

    private async Task RetrainProfilesAsync()
    {
        adjuster.PromptNoButtonPopUp("Hold on, I'm thinking... (re-training profiles)");
        await apiHelper.StartTrainingAsync();
        string status = await apiHelper.GetTrainingStatusAsync();
        while (status != FaceAPIHelper.TRAINING_SUCCEEDED)
        {
            if (status == FaceAPIHelper.TRAINING_FAILED || status == FaceAPIHelper.TRAINING_API_ERROR) {
                AddTask(GameState.API_ERROR_TRAINING_STATUS);
                Debug.LogError("API Error occurred when checking training status");
                return;
            }
            Debug.Log("Checking training status...");
            status = await apiHelper.GetTrainingStatusAsync();
            Debug.Log("status = " + status);
        }
    }

    private bool IsInvalidName(string nameToTest) 
    {
        if (nameToTest.Length < 2 || nameToTest.Length > 50)  // limit is technically 256 characters anything over 31 seems unnecessarily long
            return true;

        char[] alphabet = {'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 
            'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 
            'x', 'y', 'z', ' '};

        bool onlySpaces = true;

        foreach (char c in nameToTest.ToLower())
        {
            if (!alphabet.Contains(c))
                return true;

            if (c != ' ')
                onlySpaces = false;
        }

        return onlySpaces || (nameToTest[nameToTest.Length - 1] == ' ');
    }

    private byte[] GetImageAsByteArray(string imageFilePath)
    {
        using (FileStream fileStream =
            new FileStream(imageFilePath, FileMode.Open, FileAccess.Read))
        {
            BinaryReader binaryReader = new BinaryReader(fileStream);
            return binaryReader.ReadBytes((int)fileStream.Length);
        }
    }

    private Sprite ImgDirToSprite(string dir)
    {
        // Create a texture. Texture size does not matter, since
        // LoadImage will replace with with incoming image size.
        Texture2D tex;

        if (dir.Contains("unknown"))
        {
            tex = Resources.Load(Constants.UNKNOWN_IMG_RSRC_PATH) as Texture2D;
        }
        else
        {
            tex = new Texture2D(2, 2);
            byte[] pngBytes = GetImageAsByteArray(dir);
            // Load data into the texture.
            tex.LoadImage(pngBytes);
        }

        Sprite newImage = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0, 0));
        return newImage;
    }

    private Sprite SadFaceSprite()
    {
        Texture2D tex = Resources.Load(Constants.SADFACE_IMG_RSRC_PATH) as Texture2D;
        Sprite newImage = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0, 0));
        return newImage;
    }

    public static string DetermineSavePath()
    {
        string ret = Constants.EDITOR_SAVE_PATH;

        #if UNITY_ANDROID
        Debug.Log("Unity Android Detected");
        ret = Constants.ANDROID_SAVE_PATH;
        #endif

        return ret;
    }
}
