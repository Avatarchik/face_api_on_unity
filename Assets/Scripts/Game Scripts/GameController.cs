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
using MiniJSON;
using Newtonsoft.Json.Linq;
using Messages;
using Messages.face_msgs;
using Messages.face_id_app_msgs;

public class GameController : MonoBehaviour
{

    // The singleton instance.
    public static GameController instance = null;

    // the task queue. Unfortunately, declaration + initialization is a bit verbose... :P
    private Queue<Tuple<Dictionary<string, object>, Func<Dictionary<string, object>, Task>>> taskQueue = new Queue<Tuple<Dictionary<string, object>, Func<Dictionary<string, object>, Task>>>();

    private RosManager rosManager;
    private UIAdjuster adjuster;
    private FaceAPIHelper apiHelper;

    private GameState currentState;
    private Dictionary<GameState, Func<Dictionary<string, object>, Task>> commands;


    private int groupIdNum;
    private string personGroupId;
    private Profile loggedInProfile;   // the current logged in profile (nullable)

    private Profile selectedProfile;       // store profile that user has selected (nullable)
    private ProfileImage? selectedProfileImg;   // store image that user has selected (nullable)

    private string[] current_training_obj;

    public string api_acc_key;

    void Awake()
    {
        // Enforce singleton pattern.
        if (instance == null)
        {
            instance = this;
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
        api_acc_key = GameController.DetermineAPIAccessKey();
        adjuster = UIAdjuster.instance;
        //apiHelper = new FaceAPIHelper(Constants.API_ACCESS_KEY, Constants.PERSON_GROUP_ID);
        InitCommandDict();

        //SetState(GameState.GAMECONTROLLER_STARTING);

        if (!Directory.Exists(Constants.SAVE_PATH))
        {
            //Directory.CreateDirectory(Constants.SAVE_PATH);
            AddTask(GameState.INTERNAL_ERROR_MISSING_PROFILEDATA);
        }

        //AddTask(GameState.GAMECONTROLLER_STARTING);

        //AddTask(GameState.STARTED);
        AddTask(Constants.USE_ROS ? GameState.ROS_CONNECTION : GameState.STARTED);
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
                Tuple<Dictionary<string, object>, Func<Dictionary<string, object>, Task>> task = this.taskQueue.Dequeue();
                Dictionary<string, object> parameters = task.Item1;
                Logger.Log("Got a task from queue in GameController: " + task.Item2.Method.Name);
                await task.Item2.Invoke(parameters);
            }
            catch (Exception e)
            {
                Logger.LogError("Error invoking Task on main thread!\n" + e);
            }
        }
    }


    private void InitCommandDict()
    {
        commands = new Dictionary<GameState, Func<Dictionary<string, object>, Task>>()
        {
            //{GameState.GAMECONTROLLER_STARTING, this.Test()},
            
            {GameState.ROS_CONNECTION, this.OpenROSConnectScreen()},

            {GameState.ROS_HELLO_WORLD_ACK, this.ROSHelloWorldAck()},
            {GameState.ROS_ASK_GROUP_ID, this.ROSAskingForID()},
            {GameState.ROS_ASK_TO_RETRAIN, this.ROSAskingToRetrain()},
            {GameState.ROS_RECEIVED_GROUP_ID, this.ROSGroupIDReceived()},
            {GameState.ROS_TRAINING_RECEIVED_OBJECT_REQ, this.ROSTrainingObjReq()},
            {GameState.ROS_TRAINING_RECEIVED_START_TAKING_PICS, this.ROSTrainingTakePics()},
            {GameState.ROS_TRAINING_RECEIVED_FINISHED, this.ROSTrainingFinished()},
            {GameState.ROS_SEND_ACCEPTED_LOGIN, this.ROSSendAcceptLogin()},
            {GameState.ROS_SEND_REJECTED_LOGIN, this.ROSSendRejectLogin()},

            {GameState.STARTED, this.WhoAreYouScreen()},
            {GameState.LISTING_PROFILES, this.ListProfiles()},
            {GameState.LOGIN_DOUBLE_CHECK, this.ShowLoginDoubleCheck()},
            {GameState.LOGGING_IN, this.LogIn()},
            {GameState.CANCELLING_LOGIN, this.CancelLogin()},

            {GameState.API_ERROR_CREATE, this.APIError(GameState.API_ERROR_CREATE, "(during LargePersonGroup Person creation)")},
            {GameState.API_ERROR_COUNTING_FACES, this.APIError(GameState.API_ERROR_COUNTING_FACES, "(while counting faces)")},
            {GameState.API_ERROR_ADDING_FACE, this.APIError(GameState.API_ERROR_ADDING_FACE, "(while adding a face)")},
            {GameState.API_ERROR_IDENTIFYING, this.APIError(GameState.API_ERROR_IDENTIFYING, "(while identifying)")},
            {GameState.API_ERROR_GET_NAME, this.APIError(GameState.API_ERROR_GET_NAME, "(while trying to get name from ID after auth fail)")},
            {GameState.API_ERROR_TRAINING_STATUS, this.APIError(GameState.API_ERROR_TRAINING_STATUS, "(while checking training status)")},
            {GameState.API_ERROR_DELETING_FACE, this.APIError(GameState.API_ERROR_DELETING_FACE, "(while deleting a face)")},

            {GameState.INTERNAL_ERROR_PARSING, this.InternalError(GameState.INTERNAL_ERROR_PARSING, "(while parsing Task parameters)")},
            {GameState.INTERNAL_ERROR_NAME_FROM_ID, this.InternalError(GameState.INTERNAL_ERROR_NAME_FROM_ID, "(while retrieving name for personId locally)")},
            {GameState.INTERNAL_ERROR_MISSING_PROFILEDATA, this.InternalError(GameState.INTERNAL_ERROR_NAME_FROM_ID, "(Missing ProfileData folder)")}

        };
    }

    public void AddTask(GameState state, Dictionary<string, object> properties = null)
    {
        if (commands.ContainsKey(state))
        {
            Func<Dictionary<string, object>, Task> task = commands[state];
            Tuple<Dictionary<string, object>, Func<Dictionary<string, object>, Task>> toQueue = new Tuple<Dictionary<string, object>, Func<Dictionary<string, object>, Task>>(properties, task);
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

    // don't be intimidated by the method type!
    // each of these Funcs take in a Dictionary of parameters (nullable)
    // and returns a Task that (may or may not) use the parameters

    private Func<Dictionary<string, object>, Task> OpenROSConnectScreen()
    {
        return async (Dictionary<string, object> parameters) =>
        {
            SetState(GameState.ROS_CONNECTION);
            ClearQueuedData(); // there shouldn't be any, but just in case...
            adjuster.HideAllElementsAction();
            SceneManager.LoadScene(Constants.UNITY_ROSCONNECTION_SCENE, LoadSceneMode.Single);
        };
    }

    private Func<Dictionary<string, object>, Task> WhoAreYouScreen()
    {
        return async (Dictionary<string, object> parameters) =>
        {
            SetState(GameState.STARTED);
            ClearQueuedData();

            if (Constants.USE_ROS && !File.Exists(Path.Combine(Constants.SAVE_PATH, Constants.GRP_INFO_FILE)))
            {
                AddTask(GameState.ROS_ASK_GROUP_ID);
                return;
            }

            LoadGroupData();
            apiHelper = new FaceAPIHelper(this.api_acc_key, this.personGroupId);

            List<Profile> profiles = LoadProfiles();
            adjuster.ListProfilesAction("Who are you? Select one of the options", profiles, false);
        };
    }

    private Func<Dictionary<string, object>, Task> ListProfiles()
    {

        return async (Dictionary<string, object> parameters) =>
        {
            SetState(GameState.LISTING_PROFILES);
            List<Profile> profiles = LoadProfiles();
            adjuster.ListProfilesAction("Here are the existing profiles:", profiles);
        };
    }

    private Func<Dictionary<string, object>, Task> ShowLoginDoubleCheck()
    {

        return async (Dictionary<string, object> parameters) =>
        {
            SetState(GameState.LOGIN_DOUBLE_CHECK);

            Profile attempt = (Profile) parameters["attemptedLogin"];

            Sprite pic = ImgDirToSprite(attempt.profilePicture);
            this.selectedProfile = attempt;
            adjuster.PicWindowAction(pic, "Are you sure you want to log in as " + attempt.displayName + "?", "Login", "Back");

        };
    }

    private Func<Dictionary<string, object>, Task> CancelLogin()
    {

        return async (Dictionary<string, object> parameters) =>
        {
            SetState(GameState.CANCELLING_LOGIN);
            ClearQueuedData();
            AddTask(GameState.LISTING_PROFILES);
        };
    }

    private Func<Dictionary<string, object>, Task> LogIn()
    {
        return async (Dictionary<string, object> parameters) =>
        {
            SetState(GameState.LOGGING_IN);

            Profile profile = (Profile) parameters["profile"];

            Tuple<bool, Dictionary<string, decimal>> verified = profile.needsRetraining ? null : await VerifiedLogin(profile);

            if (profile.needsRetraining || verified.Item1)
            {
                this.loggedInProfile = profile;
                IncrementSessionNum(this.loggedInProfile);
                if (profile.needsRetraining)
                    AddTask(GameState.ROS_ASK_TO_RETRAIN);
                else
                    AddTask(GameState.ROS_SEND_ACCEPTED_LOGIN); //TODO: figure out what to do in this case
            }
            else
            {
                Dictionary<string, object> paramz = new Dictionary<string, object>();
                paramz.Add("attemptedLogin", profile.displayName);

                bool knownPerp = false;
                string perpName = "";

                if (verified.Item2.Count != 0)
                {
                    string max_pID = (from x in verified.Item2 where x.Value == verified.Item2.Max(v => v.Value) select x.Key).ElementAt(0);
                    if (max_pID != profile.personId && verified.Item2[max_pID] > Constants.CONFIDENCE_THRESHOLD)
                    {
                        knownPerp = true;

                        while (perpName == "")
                        {
                            FaceAPICall<string> nameCall = apiHelper.GetNameFromLargePersonGroupPersonPersonIdCall(max_pID);
                            await MakeRequestAndSendInfoToROS(nameCall);
                            perpName = nameCall.GetResult();
                        }
                    }
                }

                paramz.Add("knownPerp", knownPerp);
                paramz.Add("perpName", perpName.Split(' ')[0]);

                AddTask(GameState.ROS_SEND_REJECTED_LOGIN, paramz);
            }

        };
    }

    // returns a tuple:
    // Item2 is a dictionary of guesses/confidences for people in the first frame with detectable faces
    // Item1 is a bool that returns true if p's personId is in this dictionary, and if the confidence value is above a certain threshold
    private async Task<Tuple<bool, Dictionary<string, decimal>>> VerifiedLogin(Profile p, Sprite imgToCheck=null)
    {
        // 2 calls: Face - Detect and then Face - Identify

        // first call: Face - Detect
        List<string> detectedFaces = new List<string>();

        if (imgToCheck == null) adjuster.EnableCamera();

        while (detectedFaces == null || detectedFaces.Count < 1)    //could fail if the API call fails, or if the picture has no detectable faces
        {
            Sprite frameToCheck;

            if (imgToCheck == null)
            {
                await Task.Delay(Constants.TRAINING_CAM_DELAY_MS); //add delay so that the camera can turn on and focus
                adjuster.GrabCurrentWebcamFrame();
                frameToCheck = adjuster.GetCurrentSavedFrame();
            }
            else
                frameToCheck = imgToCheck;

            byte[] frameData = frameToCheck.texture.EncodeToPNG();

            FaceAPICall<List<string>> detectAPICall = apiHelper.DetectForIdentifyingCall(frameData);
            await MakeRequestAndSendInfoToROS(detectAPICall);

            detectedFaces = detectAPICall.GetResult();
        }

        if (imgToCheck == null) adjuster.DisableCamera();

        // second call: Face - Identify
        Dictionary<string, decimal> idGuesses = null;

        while (idGuesses == null)
        {
            string biggestFaceId = detectedFaces[0];

            FaceAPICall<Dictionary<string, decimal>> identifyAPICall = apiHelper.IdentifyFromFaceIdCall(biggestFaceId);
            await MakeRequestAndSendInfoToROS(identifyAPICall);

            idGuesses = identifyAPICall.GetResult();
        }

        return new Tuple<bool, Dictionary<string, decimal>>(idGuesses.ContainsKey(p.personId) && idGuesses[p.personId] > Constants.CONFIDENCE_THRESHOLD,
                                                            idGuesses);
    }

    private Func<Dictionary<string, object>, Task> APIError(GameState newState, string err)
    {

        return async (Dictionary<string, object> parameters) =>
        {
            SetState(newState);
            adjuster.PromptOKDialogueAction("API Error\r\n" + err);
        };
    }

    // might combine with above function in the future
    private Func<Dictionary<string, object>, Task> InternalError(GameState newState, string err, bool okBtn = true)
    {

        return async (Dictionary<string, object> parameters) =>
        {
            SetState(newState);
            if (okBtn)
                adjuster.PromptOKDialogueAction("Internal Error\r\n" + err);
            else
                adjuster.PromptNoButtonPopUpAction("Internal Error\r\n" + err);
        };
    }

    private void SetState(GameState newState)
    {
        currentState = newState;
    }

    public GameState GetGameState()
    {
        return currentState;
    }

    private void CreateProfile(string displayName, string personId, bool loginAfterward = true)
    {
        string folderName = displayName;
        if (Directory.Exists(Path.Combine(Constants.SAVE_PATH, displayName)))
        {
            int count = Directory.GetDirectories(Constants.SAVE_PATH, displayName + "*").Length;
            folderName = displayName + " (" + count + ")";
        }
        Directory.CreateDirectory(Path.Combine(Constants.SAVE_PATH, folderName));

        Profile newPerson = new Profile
        {
            displayName = displayName,
            folderName = folderName,
            imageCount = 0,
            images = new List<ProfileImage>(),
            personId = personId,
            profilePicture = "none",
            sessionCount = 0,
            needsRetraining = true
        };

        ExportProfileInfo(newPerson);

        if (loginAfterward)
        {
            Dictionary<string, object> param = new Dictionary<string, object>
            {
                { "profile", newPerson }
            };
            AddTask(GameState.LOGGING_IN, param);
        }

    }

    private void ExportProfileInfo(Profile p)
    {
        Logger.Log("Exporting the following profile:\r\n" + p.ToString());
        Dictionary<string, object> json = new Dictionary<string, object>();

        json.Add("personId", p.personId);
        json.Add("displayName", p.displayName);
        json.Add("sessionCount", p.sessionCount);
        json.Add("needsRetraining", p.needsRetraining);
        json.Add("count", p.imageCount);
        json.Add("profilePic", p.profilePicture ?? "none");

        ProfileImage?[] imgList = new ProfileImage?[p.imageCount];

        Logger.Log("p.images.Count: " + p.images.Count);

        foreach (ProfileImage prof in p.images)
        {
            int index = prof.indexNumber;
            imgList[index] = prof;
        }

        Dictionary<string, object> images = new Dictionary<string, object>();

        for (int i = 0; i < imgList.Length; i++)
        {
            ProfileImage? data = imgList[i];

            if (data == null)
            {
                images.Add(Constants.IMAGE_LABEL + " " + i, Constants.DELETED_IMG_LABEL);
                continue;
            }
            else
            {
                Dictionary<string, object> imgData = new Dictionary<string, object>();
                imgData.Add("path", data.Value.path);
                imgData.Add("persistedFaceId", data.Value.persistedFaceId);

                images.Add(Constants.IMAGE_LABEL + " " + i, imgData);
            }
        }

        json.Add("images", images);

        string savePath = Path.Combine(Constants.SAVE_PATH, p.folderName, Constants.INFO_FILE);
        string unformatted = Json.Serialize(json);
        string dataToSave = JToken.Parse(unformatted).ToString(Formatting.Indented);
        System.IO.File.WriteAllText(savePath, dataToSave);
    }

    private async Task<bool> AddImgToProfile(Profile profile, Sprite img)
    {
        //adjuster.PromptNoButtonPopUpAction("Hold on, I'm thinking... (adding Face to LargePersonGroup Person)");
        Texture2D tex = img.texture;
        byte[] imgData = tex.EncodeToPNG();

        FaceAPICall<string> call = apiHelper.AddFaceToLargePersonGroupPersonCall(profile.personId, imgData);

        await MakeRequestAndSendInfoToROS(call);

        string persistedId = call.GetResult();

        //if (!call.SuccessfulCall() || persistedId == "")
        //{
        //    AddTask(GameState.API_ERROR_ADDING_FACE);
        //    Logger.LogError("API Error while trying to add Face to LargePersonGroup Person");
        //    return false;
        //}

        int count = profile.imageCount;

        ProfileImage newImg = new ProfileImage
        {
            imageOwner = profile,
            indexNumber = count,
            number = profile.images.Count,
            path = Path.Combine(Constants.SAVE_PATH, profile.folderName, Constants.IMAGE_LABEL + " " + count + ".png"),
            persistedFaceId = persistedId
        };

        System.IO.File.WriteAllBytes(newImg.path, tex.EncodeToPNG());
        Logger.Log("count = " + count);
        profile.imageCount = count + 1;
        Logger.Log("count now = " + profile.imageCount);

        List<ProfileImage> newImgList = profile.images;
        newImgList.Add(newImg);

        profile.images = newImgList;

        ExportProfileInfo(profile);
        return true;
        //return await RetrainProfilesAsync();
    }

    private List<Profile> LoadProfiles()
    {
        try
        {
            List<Profile> profiles = new List<Profile>();

            string[] profileDirs = Directory.GetDirectories(Constants.SAVE_PATH);
            foreach (string dir in profileDirs)
            {
                if (File.Exists(Path.Combine(dir, Constants.INFO_FILE)))
                {
                    string folderName = new DirectoryInfo(dir).Name;

                    Profile attemptToLoad = LoadProfileData(folderName);
                    if (attemptToLoad != null)
                        profiles.Add(attemptToLoad);
                }
            }

            return profiles;
        }
        catch (Exception e)
        {
            Logger.LogError("[profile list loading]: " + e.ToString());
            return new List<Profile>();
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
        if (loggedInProfile != null)
            ExportProfileInfo(loggedInProfile);

        loggedInProfile = null;
        selectedProfile = null;
        selectedProfileImg = null;
    }

    public void SelectProfile(Profile profile)
    {
        Dictionary<string, object> parameter = new Dictionary<string, object>
        {
            { "attemptedLogin", profile }
        };

        AddTask(GameState.LOGIN_DOUBLE_CHECK, parameter);
    }

    private void LoadGroupData()
    {
        try
        {
            Dictionary<string, object> grpData = LoadDataFile(Path.Combine(Constants.SAVE_PATH, Constants.GRP_INFO_FILE));
            this.groupIdNum = Int32.Parse(grpData["id"].ToString());
            this.personGroupId = grpData["personGroupId"].ToString();
        }
        catch (Exception e)
        {
            Logger.LogError("[data loading] " + e.ToString());
        }
    }

    private Profile LoadProfileData(string folderName)
    {
        try
        {
            Profile newProf = new Profile();

            Dictionary<string, object> data = LoadDataFile(Path.Combine(Constants.SAVE_PATH, folderName, Constants.INFO_FILE));
            newProf.displayName = data["displayName"].ToString();
            newProf.folderName = folderName;
            newProf.imageCount = Int32.Parse(data["count"].ToString());
            newProf.personId = data["personId"].ToString();
            newProf.profilePicture = data["profilePic"].ToString();
            newProf.sessionCount = Int32.Parse(data["sessionCount"].ToString());
            newProf.needsRetraining = Boolean.Parse(data["needsRetraining"].ToString());

            Dictionary<string, object> images = ((JObject)data["images"]).ToObject<Dictionary<string, object>>();

            Logger.Log("images null: " + (images == null));

            List<ProfileImage> profileImgs = LoadProfileImageData(newProf, images);

            if (profileImgs == null)
                throw new JsonException("profileImgs is null -- error processing image JSON data?");

            newProf.images = profileImgs;

#if UNITY_ANDROID
            if (!newProf.profilePicture.StartsWith("sdcard"))   // in case of relative pathing
                newProf.profilePicture = Path.Combine("sdcard", newProf.profilePicture);
#endif

            return newProf;
        }
        catch (Exception e)
        {
            Logger.LogError("[data loading] " + e.ToString());
            return null;
        }
    }

    private List<ProfileImage> LoadProfileImageData(Profile p, Dictionary<string, object> data)
    {
        List<ProfileImage> profileImgs;
        try
        {
            profileImgs = new List<ProfileImage>();
            foreach (KeyValuePair<string, object> entry in data)
            {
                if (entry.Value.ToString() == "deleted")
                    continue;

                Dictionary<string, object> info = ((JObject)entry.Value).ToObject<Dictionary<string, object>>();

                string prefix = Constants.IMAGE_LABEL + " ";
                int num = Int32.Parse(entry.Key.Substring(prefix.Length));

                string path = (string)info["path"];
                string persistedFaceId = (string)info["persistedFaceId"];

                ProfileImage pImg = new ProfileImage
                {
                    imageOwner = p,
                    indexNumber = num,
                    number = profileImgs.Count,
#if UNITY_ANDROID
                    path = !path.StartsWith("sdcard") ? Path.Combine("sdcard", path) : path,    // in case of relative pathing
#else
                    path = path,
#endif
                    persistedFaceId = persistedFaceId
                };

                profileImgs.Add(pImg);

            }
        }
        catch (Exception e)
        {
            profileImgs = null;
            Logger.LogError("[data loading] " + e.ToString());
        }

        return profileImgs;
    }

    private string FolderNameToLoginName(string fName)
    {
        string pName = fName;
        int index = fName.IndexOf('(');
        if (index > 0)
            pName = fName.Substring(0, index - 1);
        return pName;
    }

    private Dictionary<string, object> LoadDataFile(string filePath)
    {
        string json = System.IO.File.ReadAllText(filePath);
        return JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
    }

    private Dictionary<string, string> ReadJsonDictFromFile(string path)
    {
        string json = System.IO.File.ReadAllText(path);
        Dictionary<string, string> data = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        return data;
    }

    private async Task<bool> DeleteSelectedPhoto(Profile person, ProfileImage photo)
    {
        string fileName = Path.GetFileNameWithoutExtension(photo.path);
        string persistedId = photo.persistedFaceId;

        adjuster.PromptNoButtonPopUpAction("Hold on, I'm thinking... (deleting Face from LargePersonGroup Person)");

        FaceAPICall<bool> apiCall = apiHelper.DeleteFaceFromLargePersonGroupPersonCall(person.personId, persistedId);
        await MakeRequestAndSendInfoToROS(apiCall);

        bool deleted = apiCall.GetResult();

        if (apiCall.SuccessfulCall() && deleted)
        {
            person.images.Remove(photo);
            ExportProfileInfo(person);
            File.Delete(photo.path);
            return await RetrainProfilesAsync();
        }
        else
        {
            AddTask(GameState.API_ERROR_DELETING_FACE);
            Logger.LogError("API Error while trying to delete Face from LargePersonGroup Person");
            return false;
        }
    }

    // currently, face API verification isn't needed unless a profile has at least 5 pics
    // (the less pictures that the API is trained with, the less accurate its verifications will be)
    private bool ShouldBeAuthenticated(Profile profile)
    {
        return profile.images.Count >= 5;
    }

    private bool AuthenticateLogin(string personId, Dictionary<string, decimal> guesses)
    {
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

    private async Task<bool> RetrainProfilesAsync()
    {
        // adjuster.PromptNoButtonPopUpAction("Hold on, I'm thinking... (re-training profiles)");
        FaceAPICall<bool> startTrainingAPICall = apiHelper.StartTrainingLargePersonGroupCall();
        await MakeRequestAndSendInfoToROS(startTrainingAPICall);

        FaceAPICall<string> trainingStatusAPICall = apiHelper.GetLargePersonGroupTrainingStatusCall();
        await MakeRequestAndSendInfoToROS(trainingStatusAPICall);

        string status = trainingStatusAPICall.GetResult();
        while (status != FaceAPIHelper.TRAINING_SUCCEEDED)
        {
            //if (status == FaceAPIHelper.TRAINING_FAILED || status == FaceAPIHelper.TRAINING_API_ERROR || !trainingStatusAPICall.SuccessfulCall()) {
            //    AddTask(GameState.API_ERROR_TRAINING_STATUS);
            //    Logger.LogError("API Error occurred when checking training status");
            //    return false;
            //}
            Logger.Log("Checking training status...");
            trainingStatusAPICall = apiHelper.GetLargePersonGroupTrainingStatusCall();
            await MakeRequestAndSendInfoToROS(trainingStatusAPICall);
            status = trainingStatusAPICall.GetResult();
            Logger.Log("status = " + status);
        }
        return true;
    }

    private void IncrementSessionNum(Profile p)
    {
        p.sessionCount++;
        ExportProfileInfo(p);
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

        if (dir.Contains("none"))
        {
            tex = Resources.Load(Constants.UNKNOWN_IMG_RSRC_PATH) as Texture2D;
        }
        else if (dir.Contains("Stock Images") || dir.Contains("Training Images"))
        {
            tex = Resources.Load(dir) as Texture2D;
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

    public RosManager GetRosManager()
    {
        return this.rosManager;
    }

    public void SetRosManager(RosManager newRos)
    {
        this.rosManager = newRos;
    }

    public Profile GetSelectedProfile()
    {
        return this.selectedProfile;
    }

    public ProfileImage GetSelectedProfileImage()
    {
        return this.selectedProfileImg.Value;
    }

    private string GetNameFromIDLocal(string personId)
    {
        List<Profile> profiles = LoadProfiles();
        foreach (Profile person in profiles)
        {
            if (person.personId.ToLower() == personId.ToLower())
                return person.displayName;
        }
        return "";  //  ??? unknown id???
    }

    private void ReloadProfile()
    {
        this.loggedInProfile = LoadProfileData(loggedInProfile.folderName);
    }


    // ====================================================================
    // All ROS message handlers.
    // They should add tasks to the task queue, because many of their
    // functionalities will throw errors if not run on the main thread.
    // ====================================================================

    public void RegisterRosMessageHandlers()
    {
        this.rosManager.RegisterHandler(FaceIDCommand.HELLO_WORLD_ACK, GameState.ROS_HELLO_WORLD_ACK);
        this.rosManager.RegisterHandler(FaceIDCommand.SEND_GROUP_ID, GameState.ROS_RECEIVED_GROUP_ID);
        this.rosManager.RegisterHandler(FaceIDCommand.LIST_PROFILES, GameState.STARTED);

        this.rosManager.RegisterHandler(FaceIDCommand.TRAINING_SHOW_OBJECT, GameState.ROS_TRAINING_RECEIVED_OBJECT_REQ);
        this.rosManager.RegisterHandler(FaceIDCommand.TRAINING_TAKE_PICS, GameState.ROS_TRAINING_RECEIVED_START_TAKING_PICS);
        this.rosManager.RegisterHandler(FaceIDCommand.TRAINING_IS_FINISHED, GameState.ROS_TRAINING_RECEIVED_FINISHED);
    }

    // HELLO_WORLD_ACK
    private void OnHelloWorldAckReceived(Dictionary<string, object> args)
    {
        Logger.Log("OnHelloWorldAckReceived");
        AddTask(GameState.ROS_HELLO_WORLD_ACK);
    }

    private Func<Dictionary<string, object>, Task> ROSHelloWorldAck()
    {

        return async (Dictionary<string, object> parameters) =>
        {
            SetState(GameState.ROS_HELLO_WORLD_ACK);
            ConnectionScreenController.instance.ShowContinueButton();
        };
    }

    private Func<Dictionary<string, object>, Task> ROSAskingForID()
    {
        return async (Dictionary<string, object> parameters) =>
        {
            SetState(GameState.ROS_ASK_GROUP_ID);
            adjuster.PromptNoButtonPopUpAction("Hold on, I'm thinking... (requesting group ID from controller)");
            this.rosManager.SendGroupIDRequestAction().Invoke();
        };
    }

    private Func<Dictionary<string, object>, Task> ROSGroupIDReceived()
    {
        return async (Dictionary<string, object> parameters) =>
        {
            SetState(GameState.ROS_RECEIVED_GROUP_ID);

            bool parsed = parameters.ContainsKey("json");
            string json = parameters["json"].ToString();

            if (parsed)
            {
                System.IO.File.WriteAllText(Path.Combine(Constants.SAVE_PATH, Constants.GRP_INFO_FILE), json);
                AddTask(GameState.STARTED);
            }
            else
                AddTask(GameState.INTERNAL_ERROR_PARSING);
        };
    }

    private Func<Dictionary<string, object>, Task> ROSAskingToRetrain()
    {
        return async (Dictionary<string, object> parameters) =>
        {
            SetState(GameState.ROS_ASK_TO_RETRAIN);

            FaceIDTraining trainingMsg = new FaceIDTraining();
            trainingMsg.event_type = FaceIDTraining.NEEDS_RETRAINING;
            trainingMsg.p_name = loggedInProfile.displayName.Split(' ')[0];

            rosManager.SendTrainingStateAction(trainingMsg);
        };
    }

    private Func<Dictionary<string, object>, Task> ROSTrainingObjReq()
    {
        return async (Dictionary<string, object> parameters) =>
        {
            SetState(GameState.ROS_TRAINING_RECEIVED_OBJECT_REQ);

            sbyte objToShow;
            bool parsed = SByte.TryParse(parameters["location"].ToString(), out objToShow);//TryParseParam("location", parameters, out objToShow);

            if (parsed)
            {
                this.current_training_obj = Constants.TRAINING_OBJ_NAME_DICT[objToShow];
                Sprite img = ImgDirToSprite(this.current_training_obj[0]);
                Vector3 objPlacement = Constants.TRAINING_OBJ_LOC_DICT[objToShow];
                FaceIDTraining msg = new FaceIDTraining();
                msg.event_type = FaceIDTraining.SHOWING_OBJECT;
                msg.object_name = this.current_training_obj[1];
                rosManager.SendTrainingStateAction(msg);

                SetState(GameState.ROS_TRAINING_SEND_OBJECT_READY);

                adjuster.ShowObjectOnScreenAction(img, objPlacement, new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, 1.0f));
            }
            else
                AddTask(GameState.INTERNAL_ERROR_PARSING);
        };
    }

    private Func<Dictionary<string, object>, Task> ROSTrainingTakePics()
    {
        return async (Dictionary<string, object> parameters) =>
        {
            SetState(GameState.ROS_TRAINING_RECEIVED_START_TAKING_PICS);

            int imageCount = 0; //count for this specific position/object

            adjuster.EnableCamera();

            while (imageCount < Constants.TRAINING_NUM_PER_POSITION)
            {
                imageCount += await TakePicAndCountFaceAsync();
                await Task.Delay(Constants.TRAINING_DELAY_BETWEEN_PICS_MS);
            }

            adjuster.DisableCamera();

            FaceIDTraining msg = new FaceIDTraining();
            msg.event_type = FaceIDTraining.DONE_WITH_LOCATION;
            rosManager.SendTrainingStateAction(msg);

            SetState(GameState.ROS_TRAINING_SEND_LOCATION_DONE);
        };
    }

    private Func<Dictionary<string, object>, Task> ROSTrainingFinished()
    {
        return async (Dictionary<string, object> parameters) =>
        {
            SetState(GameState.ROS_TRAINING_RECEIVED_FINISHED);

            bool trained = await RetrainProfilesAsync();

            while (!trained)
                trained = await RetrainProfilesAsync(); // in case the free plan limit hits

            this.loggedInProfile.needsRetraining = false;
            ExportProfileInfo(this.loggedInProfile);

            adjuster.HideAllElementsAction();

            AddTask(GameState.LOGGING_IN, new Dictionary<string, object> { { "profile", this.loggedInProfile } });

            //if (trained)
            //    Logger.LogError("Unable to re-train ")
            //else
            //    adjuster.PromptNoButtonPopUpAction("Done with training, but API still needs to be re-trained.");
            //AddTask(GameState.LOGGING_IN);
        };
    }

    private async Task<int> TakePicAndCountFaceAsync()
    {
        await Task.Delay(Constants.TRAINING_CAM_DELAY_MS); //add delay so that the camera can turn on and focus
        adjuster.GrabCurrentWebcamFrame();
        Sprite frame = adjuster.GetCurrentSavedFrame();
        byte[] picTaken = frame.texture.EncodeToPNG();

        FaceAPICall<int> apiCall = apiHelper.CountFacesCall(picTaken);
        await MakeRequestAndSendInfoToROS(apiCall);

        int numFaces = apiCall.GetResult();

        return (numFaces > 0 && await AddImgToProfile(this.loggedInProfile, frame)) ? 1 : 0;
    }

    private Func<Dictionary<string, object>, Task> ROSSendAcceptLogin()
    {
        return async (Dictionary<string, object> parameters) =>
        {
            SetState(GameState.ROS_SEND_ACCEPTED_LOGIN);

            rosManager.SendAcceptLoginAction(this.loggedInProfile.displayName.Split(' ')[0], this.loggedInProfile.sessionCount).Invoke();

            adjuster.PromptNoButtonPopUpAction("Done! Yay!");
        };
    }

    private Func<Dictionary<string, object>, Task> ROSSendRejectLogin()
    {
        return async (Dictionary<string, object> parameters) =>
        {
            SetState(GameState.ROS_SEND_REJECTED_LOGIN);

            string attemptedName = parameters["attemptedLogin"].ToString();
            bool knownPerp = bool.Parse(parameters["knownPerp"].ToString());
            string perpName = parameters["perpName"].ToString();

            rosManager.SendRejectLoginAction(attemptedName, knownPerp, perpName).Invoke();

            AddTask(GameState.STARTED);
        };
    }

    // class so that it gets passed by reference
    public class Profile : ProfileHandler.IScrollable
    {
        public string displayName, folderName;

        // index: image number
        // string 1: image path
        // string 2: Face API persistedFaceId
        public List<ProfileImage> images;

        public string personId;

        public int sessionCount;

        public bool needsRetraining;

        public int imageCount;

        public string profilePicture;   // stored as a path

        public string ImgPath { get { return profilePicture; } }

        public string DisplayName { get { return displayName; } }

        public string IdentifyingName { get { return folderName; } }

        public override string ToString()
        {
            string ret = "";
            ret += "Display Name: " + displayName;
            ret += "\r\nFolder Name: " + folderName;
            ret += "\r\npersonId: " + personId;
            ret += "\r\nSession Count: " + sessionCount;
            ret += "\r\nNeeds Retraining: " + needsRetraining;
            ret += "\r\nImage Count: " + imageCount;
            ret += "\r\nProfile Picture: " + profilePicture;

            ret += "\r\nImages:\r\n";
            foreach (ProfileImage img in images)
            {
                ret += "---\r\n";
                ret += img.ToString();
                ret += "\r\n---";
            }

            return ret;
        }
    }

    //struct for simplicity
    public struct ProfileImage : ProfileHandler.IScrollable
    {
        public Profile imageOwner;

        public int indexNumber; //includes deleted images
        public int number;      //excludes deleted images
        public string path;
        public string persistedFaceId;

        public string ImgPath { get { return path; } }

        public string DisplayName
        {
            get { return "you shouldn't be seeing this";/*Constants.IMAGE_DISPLAY_LABEL + " " + (number + 1);*/ }
        }

        public string IdentifyingName
        {
            get { return Path.Combine(imageOwner.folderName, Constants.IMAGE_LABEL + " " + indexNumber); }
        }

        public override string ToString()
        {
            string ret = "";
            ret += "Image Owner: " + imageOwner.displayName;
            ret += "\r\nIdentifyingName: " + IdentifyingName;
            ret += "\r\nDisplayName: " + DisplayName;
            ret += "\r\npersistedFaceId: " + persistedFaceId;
            ret += "\r\npath: " + path;
            ret += "\r\nindexNumber: " + indexNumber;
            ret += "\r\nnumber: " + number;
            return ret;
        }
    }

    public async Task MakeRequestAndSendInfoToROS<T>(FaceAPICall<T> call)
    {
        FaceAPIRequest request = call.request;
        //rosManager.SendFaceAPIRequestAction(request);

        await call.MakeCallAsync();

        FaceAPIResponse response = call.response;
        //rosManager.SendFaceAPIResponseAction(response);
    }

    public static string DetermineAPIAccessKey()
    {
        TextAsset api_access_key = Resources.Load(Constants.API_ACCESS_KEY_PATH) as TextAsset;
        return api_access_key.text;
    }
}
