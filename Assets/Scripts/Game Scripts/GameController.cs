using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityFaceIDHelper;

public class GameController : MonoBehaviour {

    // The singleton instance.
    public static GameController instance = null;

    private string currentState;
    private bool shouldUpdate;

    private UIAdjuster adjuster;

    private string loggedInName, folderName;    //will always be the same, unless theres > 1 of the same loggedInName
    private Dictionary<string, string> profileInfo;
    private bool multipleNames = false;

    private Sprite savedFrame;
    private string savedFrameDir;
    private FaceAPIHelper apiHelper;

    public static readonly string SAVE_PATH = DetermineSavePath();
    public static readonly string UNKNOWN_IMG = Path.Combine("Stock Images", "unknown");
    public static readonly string SADFACE_IMG = Path.Combine("Stock Images", "sad");
    const string PERSON_GROUP_ID = "unity";
    const decimal CONFIDENCE_THRESHOLD = 0.70m;    // decimal between 0 and 1
    const int CAM_DELAY_MS = 2000;

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
        adjuster = GetComponent<UIAdjuster>();
        TextAsset api_access_key = Resources.Load("api_access_key") as TextAsset;
        apiHelper = new FaceAPIHelper(api_access_key.text, PERSON_GROUP_ID);

        currentState = "started";
        shouldUpdate = true;

        if (!Directory.Exists(SAVE_PATH))
        {
            Directory.CreateDirectory(SAVE_PATH);
        }
	}
	
	// Update is called once per frame
	async void Update()
    {
        if (shouldUpdate)
        {
            shouldUpdate = false;
            switch (currentState)
            {
                case "started":
                    //TODO: clear any queue'd data (like user id) before prompting question
                    ClearQueuedData();
                    UIAskQuestion("\r\nHi! Are you new here?");
                    break;
                case "newPrompt":
                    UIAskQuestion("\r\nWould you like to make a profile?");
                    break;
                case "mustLogin":
                    UIPromptOKDialogue("\r\nIn order to use the app, you must be logged into a profile.");
                    break;
                case "profileName":
                    UIPromptInputText("What is your name?\r\n\r\nPlease ensure that the name you enter is valid.");
                    break;
                case "afterNameTyped":
                    string entered = adjuster.GetTypedInput().ToLower();
                    if (IsInvalidName(entered))  // conditions for an invalid name
                        ChangeState("profileName");
                    else
                    {
                        UIPromptNoButtonPopUp("Hold on, I'm thinking... (creating LargePersonGroup Person)");
                        string personID = await apiHelper.CreatePersonAsync(entered);
                        if (personID != "") //successful API call
                        {
                            loggedInName = entered;
                            folderName = entered;
                            CreateProfile();
                            SetPersonID(personID);
                            ChangeState("loggedIn");
                        }
                        else
                        { // maybe internet is down, maybe api access is revoked...
                            ChangeState("apiErrorCreate");
                            Debug.LogError("API Error occurred while trying to create a LargePersonGroup Person");
                        }
                    }
                    break;
                case "listFaceImages":
                    Dictionary<Tuple<string, string>, string> imageList = LoadImages();
                    UIListImages("Here is your photo listing:", imageList);
                    break;
                case "addingImgWebcam":
                    if (!ShouldBeAuthenticated())   //if they don't need to be authenticated
                        UIShowWebcam("Take a picture!", "Snap!");
                    else
                    {
                        if (await VerifyAsync(true))    //if they pass authentification
                            UIShowWebcam("Take a picture!", "Snap!");
                    }
                    break;
                case "addingImgCheckPic":
                    Sprite frame = adjuster.GrabCurrentWebcamFrame();
                    UIPromptNoButtonPopUp("Hold on, I'm thinking... (counting faces in image)");
                    byte[] imgData = frame.texture.EncodeToPNG();

                    int numFaces = await apiHelper.CountFacesAsync(imgData);
                    if (numFaces == -1)
                    {
                        ChangeState("apiErrorCountingFaces");
                        Debug.LogError("API Error occurred while trying to count the faces in a frame");
                        return;
                    }

                    if (numFaces < 1)   //pic has no detectable faces in it... try again.
                        ChangeState("addingImgTryAgain");
                    else
                    {
                        if (!ShouldBeAuthenticated())
                        {
                            savedFrame = frame;
                            ChangeState("addingImgShowPic");
                        }
                        else
                        {
                            if (await VerifyAsync(true, frame))    //if they pass authentification
                            {
                                savedFrame = frame;
                                ChangeState("addingImgShowPic");
                            }
                        }
                    }
                    break;
                case "addingImgTryAgain":
                    UIPicWindow(savedFrame, "I didn't like this picture :( Can we try again?", "Try again...", "Cancel");
                    break;
                case "addingImgShowPic":
                    UIPicWindow(savedFrame, "I like it! What do you think?", "Keep it!", "Try again...");
                    break;
                case "addingImgSaving":
                    await AddImgToProfile();
                    ChangeState("listFaceImages");
                    break;
                case "listProfiles":
                    Dictionary<Tuple<string, string>, string> profiles = LoadProfiles();
                    UIListProfiles("Here are the existing profiles:", profiles);
                    break;
                case "loginAreYouSure":
                    Sprite pic = ImgDirToSprite(GetProfilePicDir(loggedInName));
                    string displayName = FolderNameToLoginName(loggedInName);
                    UIPicWindow(pic, "Are you sure you want to log in as " + displayName + "?", "Login", "Back"); 
                    break;
                case "loggingIn":
                    LoadProfileData(loggedInName);
                    if (!ShouldBeAuthenticated())
                        ChangeState("loggedIn");
                    else
                    {
                        if (await VerifyAsync(true))
                            ChangeState("loggedIn");
                    }
                    break;
                case "cancelLogin":
                    ClearQueuedData();
                    ChangeState("listProfiles");
                    break;
                case "loggedIn": 
                    UIPromptOKDialogue("\r\nWelcome, " + loggedInName + "!");
                    break;
                case "photoSelected":
                    UIPicWindow(savedFrame, "Nice picture! What would you like to do with it?", "Delete it", "Keep it");
                    break;
                case "deletePhoto":
                    if (!ShouldBeAuthenticated())
                    {
                        await DeleteSelectedPhoto();
                        ChangeState("listFaceImages");
                    }
                    else
                    {
                        if (await VerifyAsync(true))
                        {
                            await DeleteSelectedPhoto();
                            ChangeState("listFaceImages");
                        }
                    }
                    break;
                case "apiErrorCreate": UIPromptOKDialogue("API Error\r\n(during LargePersonGroup Person creation)"); break;
                case "apiErrorCountingFaces": UIPromptOKDialogue("API Error\r\n(while counting faces)"); break;
                case "apiErrorAddingFace": UIPromptOKDialogue("API Error\r\n(while adding a face)"); break;
                case "apiErrorIdentifying": UIPromptOKDialogue("API Error\r\n(while identifying)"); break;
                case "apiErrorGetNameAfterRejection": UIPromptOKDialogue("API Error\r\n(while trying to get name from ID after auth fail)"); break;
                case "apiErrorTrainingStatus": UIPromptOKDialogue("API Error\r\n(while checking training status)"); break;
                case "apiErrorDeletingFace": UIPromptOKDialogue("API Error\r\n(while deleting a face)"); break;
                default: Debug.LogError("Unknown state entered! Note that states are case-sensitive. state = " + currentState); break;
            }
        }
        else
        {
            return;
        }

	}

    public void ChangeState(string newState)
    {
        currentState = newState;
        shouldUpdate = true;
    }

    public string GetGameState()
    {
        return currentState;
    }

    private void CreateProfile()
    {
        if (Directory.Exists(Path.Combine(SAVE_PATH, loggedInName)))
        {
            multipleNames = true;
            int count = Directory.GetDirectories(SAVE_PATH, loggedInName + "*").Length;
            folderName = loggedInName + " (" + count + ")";
        }
        Directory.CreateDirectory(Path.Combine(SAVE_PATH, folderName));
        profileInfo = new Dictionary<string, string>();
        profileInfo.Add("personID", "..."); //todo: change this to the actual personID
        profileInfo.Add("count", "0");
        ExportProfileInfo();
    }

    //TODO: maybe change this to use Object serialization... lol
    private void ExportProfileInfo()
    {
        string savePath = Path.Combine(SAVE_PATH, folderName, "info.txt");
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

        string path = Path.Combine(SAVE_PATH, folderName);
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
        UIPromptNoButtonPopUp("Hold on, I'm thinking... (adding Face to LargePersonGroup Person)");
        Texture2D tex = savedFrame.texture;
        byte[] imgData = tex.EncodeToPNG();

        string persistedId = await apiHelper.AddFaceAsync(profileInfo["personID"], imgData);

        if (persistedId == "")
        {
            ChangeState("apiErrorAddingFace");
            Debug.LogError("API Error while trying to add Face to LargePersonGroup Person");
            return;
        }

        int count = Int32.Parse(profileInfo["count"]);
        System.IO.File.WriteAllBytes(Path.Combine(SAVE_PATH, folderName, "Image " + count + ".png"), tex.EncodeToPNG());
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
            string[] profileDirs = Directory.GetDirectories(SAVE_PATH);
            Debug.Log("number of profile directories: " + profileDirs.Length);
            int unknownCount = 0;
            foreach (string p in profileDirs)
            {
                Debug.Log("Here's a profile dir: " + p);
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

        string[] profileImgDirs = Directory.GetFiles(Path.Combine(SAVE_PATH, fName), "*.png");
        if (profileImgDirs.Length < 1)
        {
            dir = "(" + unknown + ") " + GameController.UNKNOWN_IMG;
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
        ChangeState("loginAreYouSure");
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
        profileInfo = ReadJsonDictFromFile(Path.Combine(SAVE_PATH, folderName, "info.txt"));
    }

    private Dictionary<string, string> ReadJsonDictFromFile(string path)
    {
        string json = System.IO.File.ReadAllText(path);
        Dictionary<string, string> data = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        return data;
    }

    public void SelectPhoto(string identifier)
    {
        savedFrame = ImgDirToSprite(Path.Combine(SAVE_PATH, identifier));
        savedFrameDir = identifier;
        ChangeState("photoSelected");
    }

    private async Task DeleteSelectedPhoto()
    {
        string fileName = Path.GetFileNameWithoutExtension(savedFrameDir);
        string persistedId = profileInfo[fileName];

        UIPromptNoButtonPopUp("Hold on, I'm thinking... (deleting Face from LargePersonGroup Person)");

        bool deleted = await apiHelper.DeleteFaceAsync(profileInfo["personID"], persistedId);

        if (deleted)
        {
            profileInfo[fileName] = "deleted";
            ExportProfileInfo();
            File.Delete(Path.Combine(SAVE_PATH, savedFrameDir));
            await RetrainProfilesAsync();
        }
        else
        {
            ChangeState("apiErrorDeletingFace");
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
        Sprite frame;

        if (imgToCheck == null)
        {
            adjuster.EnableCamera();
            await Task.Delay(CAM_DELAY_MS); //add delay so that the camera can turn on and focus
            frame = adjuster.GrabCurrentWebcamFrame();
        }
        else
            frame = imgToCheck;

        UIPromptNoButtonPopUp("Hold on, I'm thinking... (identifying faces in current frame)");
        byte[] frameData = frame.texture.EncodeToPNG();

        Dictionary<string, decimal> guesses = await apiHelper.IdentifyBiggestInImageAsync(frameData);

        if (guesses == null)
        {
            ChangeState("apiErrorIdentifying");
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
                    UIPromptNoButtonPopUp("Hold on, I'm thinking... (retrieving name(s) from personId(s)");
                    for (int i = 0; i < guesses.Count; i++)
                    {
                        string key = guesses.Keys.ElementAt(i);
                        string nameFromID = await apiHelper.GetNameFromIDAsync(key);
                        if (nameFromID == "")
                        {
                            ChangeState("apiErrorGetNameAfterRejection");
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
                UIPromptOKDialogue(response);
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
            return confidence >= CONFIDENCE_THRESHOLD;
        }
    }

    private async Task RetrainProfilesAsync()
    {
        UIPromptNoButtonPopUp("Hold on, I'm thinking... (re-training profiles)");
        await apiHelper.StartTrainingAsync();
        string status = await apiHelper.GetTrainingStatusAsync();
        while (status != FaceAPIHelper.TRAINING_SUCCEEDED)
        {
            if (status == FaceAPIHelper.TRAINING_FAILED || status == FaceAPIHelper.TRAINING_API_ERROR) {
                ChangeState("apiErrorTrainingStatus");
                Debug.LogError("API Error occurred when checking training status");
                return;
            }
            Debug.Log("Checking training status...");
            status = await apiHelper.GetTrainingStatusAsync();
            Debug.Log("status = " + status);
        }
    }

    private void UIAskQuestion(string q)
    {
        //hide everything not in use
        adjuster.HideCameraFeed();
        adjuster.HideUpdateCancelPopUp();
        adjuster.HideTextInput();
        adjuster.HideOKPopUp();
        adjuster.HideNoButtonPopUp();
        adjuster.HideProfileList();
        adjuster.HideImageList();
        adjuster.HideUpdateImage();

        //hide window while values are being changed
        adjuster.HideQuestionPopUp();
        adjuster.SetQuestionPopUpText(q);

        //show the window after changes are made
        adjuster.ShowQuestionPopUp();
    }

    private void UIPromptInputText(string prompt)
    {
        //hide everything not in use
        adjuster.HideCameraFeed();
        adjuster.HideUpdateCancelPopUp();
        adjuster.HideNoButtonPopUp();
        adjuster.HideQuestionPopUp();
        adjuster.HideOKPopUp();
        adjuster.HideProfileList();
        adjuster.HideImageList();
        adjuster.HideUpdateImage();

        //hide window while values are being changed
        adjuster.HideTextInput();
        adjuster.SetTextInputPrompt(prompt);

        //show the window after changes are made
        adjuster.ShowTextInput();
    }

    private void UIPromptOKDialogue(string prompt)
    {
        //hide everything not in use
        adjuster.HideCameraFeed();
        adjuster.HideUpdateCancelPopUp();
        adjuster.HideTextInput();
        adjuster.HideNoButtonPopUp();
        adjuster.HideQuestionPopUp();
        adjuster.HideProfileList();
        adjuster.HideImageList();
        adjuster.HideUpdateImage();

        //hide window while values are being changed
        adjuster.HideOKPopUp();
        adjuster.SetOKPopUpText(prompt);

        //show the window after changes are made
        adjuster.ShowOKPopUp();
    }

    private void UIListProfiles(string prompt, Dictionary<Tuple<string, string>, string> profiles)
    {
        //hide everything not in use
        adjuster.HideCameraFeed();
        adjuster.HideUpdateCancelPopUp();
        adjuster.HideTextInput();
        adjuster.HideNoButtonPopUp();
        adjuster.HideQuestionPopUp();
        adjuster.HideOKPopUp();
        adjuster.HideImageList();
        adjuster.HideUpdateImage();

        //hide window while values are being changed
        adjuster.HideProfileList();
        adjuster.SetProfileListText(prompt);
        adjuster.UpdateProfileList(profiles);

        //show the window after changes are made
        adjuster.ShowProfileList();
    }

    private void UIListImages(string prompt, Dictionary<Tuple<string, string>, string> profiles)
    {
        //hide everything not in use
        adjuster.HideCameraFeed();
        adjuster.HideUpdateCancelPopUp();
        adjuster.HideTextInput();
        adjuster.HideNoButtonPopUp();
        adjuster.HideQuestionPopUp();
        adjuster.HideOKPopUp();
        adjuster.HideProfileList();
        adjuster.HideUpdateImage();

        //hide window while values are being changed
        adjuster.HideImageList();
        adjuster.SetImageListText(prompt);
        adjuster.UpdateImageList(profiles);

        //show the window after changes are made
        adjuster.ShowImageList();
    }

    private void UIShowWebcam(string prompt, string updateText = "Update", string cancelText = "Cancel")
    {
        //hide everything not in use
        adjuster.HideTextInput();
        adjuster.HideNoButtonPopUp();
        adjuster.HideQuestionPopUp();
        adjuster.HideOKPopUp();
        adjuster.HideProfileList();
        adjuster.HideImageList();
        adjuster.HideUpdateImage();

        //hide window while values are being changed
        adjuster.HideCameraFeed();
        adjuster.HideUpdateCancelPopUp();
        adjuster.SetUpdateCancelPopUpText(prompt);
        adjuster.SetUpdateButtonText(updateText);
        adjuster.SetCancelButtonText(cancelText);

        //show the window after changes are made
        adjuster.ShowUpdateCancelButtonPopUp();
        adjuster.ShowCameraFeed();
    }

    private void UIPicWindow(Sprite pic, string prompt, string updateText = "Update", string cancelText = "Cancel")
    {
        //hide everything not in use
        adjuster.HideTextInput();
        adjuster.HideNoButtonPopUp();
        adjuster.HideQuestionPopUp();
        adjuster.HideOKPopUp();
        adjuster.HideProfileList();
        adjuster.HideImageList();
        adjuster.HideCameraFeed();

        //hide window while values are being changed
        adjuster.HideUpdateCancelPopUp();
        adjuster.HideUpdateImage();
        adjuster.SetUpdateCancelPopUpText(prompt);
        adjuster.SetUpdateButtonText(updateText);
        adjuster.SetCancelButtonText(cancelText);
        adjuster.ChangeUpdateImage(pic);

        //show the window after changes are made
        adjuster.ShowUpdateCancelButtonPopUp();
        adjuster.ShowUpdateImage();
    }

    private void UIPromptNoButtonPopUp(string prompt)
    {
        //hide everything not in use
        adjuster.HideCameraFeed();
        adjuster.HideUpdateCancelPopUp();
        adjuster.HideTextInput();
        adjuster.HideOKPopUp();
        adjuster.HideQuestionPopUp();
        adjuster.HideProfileList();
        adjuster.HideImageList();
        adjuster.HideUpdateImage();

        //hide window while values are being changed
        adjuster.HideNoButtonPopUp();
        adjuster.SetNoButtonPopUpText(prompt);

        //show the window after changes are made
        adjuster.ShowNoButtonPopUp();
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
            tex = Resources.Load(GameController.UNKNOWN_IMG) as Texture2D;
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
        Texture2D tex = Resources.Load(GameController.SADFACE_IMG) as Texture2D;
        Sprite newImage = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0, 0));
        return newImage;
    }

    private static string DetermineSavePath()
    {
        string ret = Path.Combine(Directory.GetCurrentDirectory(), "ProfileData");

        #if UNITY_ANDROID
        Debug.Log("Unity Android Detected");
        ret = Path.Combine("sdcard", "PersonalRobotsGroup.FaceIDApp", "ProfileData");
        //ret = "/sdcard/PersonalRobotsGroup.FaceIDApp/ProfileData";
        #endif

        return ret;
    }
}
