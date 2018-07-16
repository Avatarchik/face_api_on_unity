using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityFaceIDHelper;

public class GameController : MonoBehaviour {
    
    // The singleton instance.
    public static GameController instance = null;

    private Queue<Func<Task>> taskQueue = new Queue<Func<Task>>();

    private RosManager rosManager;
    private UIAdjuster adjuster;
    private FaceAPIHelper apiHelper;

    public Canvas uiElementContainer;

    private GameState currentState;

    private string loggedInName, folderName;    //will always be the same, unless theres > 1 of the same loggedInName
    private Dictionary<string, string> profileInfo;
    private bool multipleNames = false;

    private Sprite savedFrame;
    private string savedFrameDir;
    private Dictionary<GameState, Func<Task>> commands;

    void Awake()
    {
        // Enforce singleton pattern.
        if (instance == null)
        {
            instance = this;
            //unityScheduler = TaskScheduler.FromCurrentSynchronizationContext();
        }
        else if (instance != this)
        {
            Logger.Log("duplicate GameController, destroying");
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
        InitCommandDict();

        //SetState(GameState.GAMECONTROLLER_STARTING);

        //AddTask(GameState.STARTED);
        AddTask(GameState.ROS_CONNECTION);

        if (!Directory.Exists(Constants.SAVE_PATH))
        {
            Directory.CreateDirectory(Constants.SAVE_PATH);
        }
	}
	
	// Update is called once per frame
    // needs to be async because API calls are currently all marked with the async keyword
	async void Update()
    {
        //Logger.Log("Current gameState: " + this.GetGameState());
        await HandleTaskQueue();
	}

    // Handle main task queue.
    // needs to be async because API calls are currently all marked with the async keyword
    private async Task HandleTaskQueue()
    {
        // Pop tasks from the task queue and perform them.
        // Tasks are added from other threads, usually in response to ROS msgs.
        if (this.taskQueue.Count > 0)
        {
            try
            {
                Logger.Log("Got a task from queue in GameController");
                await this.taskQueue.Dequeue().Invoke();
            }
            catch (Exception e)
            {
                Logger.LogError("Error invoking Task on main thread!\n" + e);
            }
        }
    }


    private void InitCommandDict()
    {
        commands = new Dictionary<GameState, Func<Task>>()
        {
            {GameState.ROS_CONNECTION, this.OpenROSConnectScreen()},

            {GameState.ROS_HELLO_WORLD_ACK, this.ROSHelloWorldAck()},

            {GameState.STARTED, this.StartGame()},
            {GameState.NEW_PROFILE_PROMPT, this.AskNewProfile()},
            {GameState.MUST_LOGIN_PROMPT, this.ShowMustLogin()},
            {GameState.ENTER_NAME_PROMPT, this.AskForNewProfileName()},
            {GameState.EVALUATING_TYPED_NAME, this.EvaluateTypedNameAsync()},
            {GameState.LISTING_IMAGES, this.ShowPicturesForProfile()},
            {GameState.TAKING_WEBCAM_PIC, this.OpenWebcamForPictureAsync()},
            {GameState.CHECKING_TAKEN_PIC, this.CheckPictureTakenAsync()},
            {GameState.PIC_APPROVAL, this.ShowImgApprovalPage()},
            {GameState.PIC_DISAPPROVAL, this.ShowImgDisapprovalPage()},
            {GameState.SAVING_PIC, this.AddImgToProfileAsync()},
            {GameState.LISTING_PROFILES, this.ListProfiles()},
            {GameState.LOGIN_DOUBLE_CHECK, this.ShowLoginDoubleCheck()},
            {GameState.LOGGING_IN, this.LogIn()},
            {GameState.CANCELLING_LOGIN, this.CancelLogin()},
            {GameState.WELCOME_SCREEN, this.ShowWelcomeScreen()},
            {GameState.SHOWING_SELECTED_PHOTO, this.ShowSelectedPhoto()},
            {GameState.DELETING_PHOTO, this.DeletePhotoAsync()},

            {GameState.API_ERROR_CREATE, this.APIError(GameState.API_ERROR_CREATE, "(during LargePersonGroup Person creation)")},
            {GameState.API_ERROR_COUNTING_FACES, this.APIError(GameState.API_ERROR_COUNTING_FACES, "(while counting faces)")},
            {GameState.API_ERROR_ADDING_FACE, this.APIError(GameState.API_ERROR_ADDING_FACE, "(while adding a face)")},
            {GameState.API_ERROR_IDENTIFYING, this.APIError(GameState.API_ERROR_IDENTIFYING, "(while identifying)")},
            {GameState.API_ERROR_GET_NAME, this.APIError(GameState.API_ERROR_GET_NAME, "(while trying to get name from ID after auth fail)")},
            {GameState.API_ERROR_TRAINING_STATUS, this.APIError(GameState.API_ERROR_TRAINING_STATUS, "(while checking training status)")},
            {GameState.API_ERROR_DELETING_FACE, this.APIError(GameState.API_ERROR_DELETING_FACE, "(while deleting a face)")}

        };
    }

    public void AddTask(GameState state, Dictionary<string, object> properties = null)
    {
        if (commands.ContainsKey(state))
        {
            Func<Task> toQueue = commands[state];
            this.taskQueue.Enqueue(toQueue);
        }
        else
        {
            Logger.LogError("Unknown GameState Task! state = " + state);
        }
    }

    // Clean up.
    void OnApplicationQuit()
    {
        if (this.rosManager != null && this.rosManager.IsConnected())
        {
            // Stop the thread that's sending StorybookState messages.
            this.rosManager.StopSendingFaceIDState();
            // Close the ROS connection cleanly.
            this.rosManager.CloseConnection();
        }
    }

    private Func<Task> OpenROSConnectScreen()
    {
        return async () =>
        {
            SetState(GameState.ROS_CONNECTION);
            ClearQueuedData(); // there shouldn't be any, but just in case...
            adjuster.HideAllElements();
            SceneManager.LoadScene("ROS Connection", LoadSceneMode.Single);
        };
    }

    private Func<Task> StartGame()
    {
        return async () =>
        {
            SetState(GameState.STARTED);
            ClearQueuedData();
            adjuster.AskQuestion("\r\nHi! Are you new here?");

        };
    }

    private Func<Task> AskNewProfile()
    {
        return async () =>
        {
            SetState(GameState.NEW_PROFILE_PROMPT);
            adjuster.AskQuestion("\r\nWould you like to make a profile?");

        };
    }

    private Func<Task> ShowMustLogin()
    {
        return async () =>
        {
            SetState(GameState.MUST_LOGIN_PROMPT);
            adjuster.PromptOKDialogue("\r\nIn order to use the app, you must be logged into a profile.");
        };
    }

    private Func<Task> AskForNewProfileName()
    {
        return async () =>
        {
            SetState(GameState.ENTER_NAME_PROMPT);
            adjuster.PromptInputText("What is your name?\r\n\r\nPlease ensure that the name you enter is valid.");
        };

    }

    private Func<Task> EvaluateTypedNameAsync()
    {
        return async () =>
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
                    Logger.LogError("API Error occurred while trying to create a LargePersonGroup Person");
                }
            }
        };
    }

    private Func<Task> ShowPicturesForProfile()
    {
        return async () =>
        {
            SetState(GameState.LISTING_IMAGES);
            Dictionary<Tuple<string, string>, string> imageList = LoadImages();
            adjuster.ListImages("Here is your photo listing:", imageList);
        };
    }

    private Func<Task> OpenWebcamForPictureAsync()
    {
        return async () =>
        {
            SetState(GameState.TAKING_WEBCAM_PIC);
            await AuthenticateIfNecessaryThenDo(() =>
            {
                adjuster.ShowWebcam("Take a picture!", "Snap!");
            }, true);
        };
    }

    private Func<Task> CheckPictureTakenAsync()
    {
        return async () =>
        {
            SetState(GameState.CHECKING_TAKEN_PIC);
            Sprite frame = adjuster.GrabCurrentWebcamFrame();
            adjuster.PromptNoButtonPopUp("Hold on, I'm thinking... (counting faces in image)");
            byte[] imgData = frame.texture.EncodeToPNG();

            int numFaces = await apiHelper.CountFacesAsync(imgData);
            if (numFaces == -1)
            {
                AddTask(GameState.API_ERROR_COUNTING_FACES);
                Logger.LogError("API Error occurred while trying to count the faces in a frame");
                return;
            }

            if (numFaces < 1)   //pic has no detectable faces in it... try again.
                AddTask(GameState.PIC_DISAPPROVAL);
            else
            {
                await AuthenticateIfNecessaryThenDo(() =>
                {
                    savedFrame = frame;
                    AddTask(GameState.PIC_APPROVAL);
                }, true, frame);
            }

        };
    }

    private Func<Task> ShowImgDisapprovalPage()
    {
        return async () =>
        {
            SetState(GameState.PIC_DISAPPROVAL);
            adjuster.PicWindow(SadFaceSprite(), "I didn't like this picture :( Can we try again?", "Try again...", "Cancel");
        };
    }

    private Func<Task> ShowImgApprovalPage()
    {
        return async () =>
        {
            SetState(GameState.PIC_APPROVAL);
            adjuster.PicWindow(savedFrame, "I like it! What do you think?", "Keep it!", "Try again...");
        };
    }

    private Func<Task> AddImgToProfileAsync()
    {
        return async () =>
        {
            SetState(GameState.SAVING_PIC);
            await AddImgToProfile();
            AddTask(GameState.LISTING_IMAGES);
        };
    }

    private Func<Task> ListProfiles()
    {
        return async () =>
        {
            SetState(GameState.LISTING_PROFILES);
            Dictionary<Tuple<string, string>, string> profiles = LoadProfiles();
            adjuster.ListProfiles("Here are the existing profiles:", profiles);
        };
    }

    private Func<Task> ShowLoginDoubleCheck()
    {
        return async () =>
        {
            SetState(GameState.LOGIN_DOUBLE_CHECK);
            Sprite pic = ImgDirToSprite(GetProfilePicDir(loggedInName));
            string displayName = FolderNameToLoginName(loggedInName);
            adjuster.PicWindow(pic, "Are you sure you want to log in as " + displayName + "?", "Login", "Back");
        };
    }

    private Func<Task> LogIn()
    {
        return async () =>
        {
            SetState(GameState.LOGGING_IN);
            LoadProfileData(loggedInName);
            await AuthenticateIfNecessaryThenDo(() =>
            {
                AddTask(GameState.WELCOME_SCREEN);
            }, true);
        };
    }

    private Func<Task> CancelLogin()
    {
        return async () =>
        {
            SetState(GameState.CANCELLING_LOGIN);
            ClearQueuedData();
            AddTask(GameState.LISTING_PROFILES);
        };
    }

    private Func<Task> ShowWelcomeScreen()
    {
        return async () =>
        {
            SetState(GameState.WELCOME_SCREEN);
            adjuster.PromptOKDialogue("\r\nWelcome, " + loggedInName + "!");
        };
    }

    private Func<Task> ShowSelectedPhoto()
    {
        return async () =>
        {
            SetState(GameState.SHOWING_SELECTED_PHOTO);
            adjuster.PicWindow(savedFrame, "Nice picture! What would you like to do with it?", "Delete it", "Keep it");
        };
    }

    private Func<Task> DeletePhotoAsync()
    {
        return async () =>
        {
            SetState(GameState.DELETING_PHOTO);
            await AuthenticateIfNecessaryThenDo(async () =>
            {
                await DeleteSelectedPhoto();
                AddTask(GameState.LISTING_IMAGES);
            }, true);
        };
    }

    private Func<Task> APIError(GameState newState, string err)
    {
        return async () =>
        {
            SetState(newState);
            adjuster.PromptOKDialogue("API Error\r\n" + err);
        };
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
            Logger.LogError("API Error while trying to add Face to LargePersonGroup Person");
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
            Logger.Log("Exception: " + e.ToString());
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
            Logger.LogError("API Error while trying to delete Face from LargePersonGroup Person");
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
            Logger.LogError("API Error occurred while trying to identify LargePersonGroup Person in a frame");
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
                            Logger.LogError("API Error occurred while trying to get name from ID (after auth fail)");
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
            Logger.Log("guesses does not contain the personId");
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
                Logger.LogError("API Error occurred when checking training status");
                return;
            }
            Logger.Log("Checking training status...");
            status = await apiHelper.GetTrainingStatusAsync();
            Logger.Log("status = " + status);
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

    public RosManager GetRosManager()
    {
        return this.rosManager;
    }

    public void SetRosManager(RosManager newRos)
    {
        this.rosManager = newRos;
    }

    // ====================================================================
    // All ROS message handlers.
    // They should add tasks to the task queue, because many of their
    // functionalities will throw errors if not run on the main thread.
    // ====================================================================

    public void RegisterRosMessageHandlers()
    {
        this.rosManager.RegisterHandler(FaceIDCommand.HELLO_WORLD_ACK, GameState.ROS_HELLO_WORLD_ACK);
    }

    // HELLO_WORLD_ACK
    private void OnHelloWorldAckReceived(Dictionary<string, object> args)
    {
        Logger.Log("OnHelloWorldAckReceived");
        AddTask(GameState.ROS_HELLO_WORLD_ACK);
    }

    private Func<Task> ROSHelloWorldAck()
    {
        return async () =>
        {
            ConnectionScreenController.instance.ShowContinueButton();
            //Logger.Log("Our \"Hello World\" ping has been received and acknowledged by the ROS Gods! :D");
            //SetState(GameState.ROS_HELLO_WORLD_ACK);
            //SceneManager.LoadScene("Game", LoadSceneMode.Single);
            //await Task.Delay(2000);
            //adjuster.HideAllElements();
            //adjuster.PromptOKDialogue("Our \"Hello World\" ping has been received and acknowledged by the ROS Gods! :D");
        };
    }
}
