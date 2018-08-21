using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityFaceIDHelper;

public class UIAdjuster : MonoBehaviour {

    // The singleton instance.
    public static UIAdjuster instance = null;

    // unity is very anal about how its "single-threaded", so all UI stuff will now happen on a Queue...
    private Queue<Action> taskQueue = new Queue<Action>();

    public GameObject questionPopUp;
    public Text questionText;

    public GameObject textInput;
    public Text textInputPrompt;
    public InputField inputField;

    public GameObject okPopUp;
    public Text okDialogueText;

    public Canvas trainingObjImgContainer;

    public GameObject noButtonPopUp;
    public Text noButtonText;

    public GameObject updateCancelPopUp;
    public Text updateCancelText;

    public GameObject profileListWindow;
    public Text profileListText;
    public GameObject profileList;

    public GameObject imageListWindow;
    public Text imageListText;
    public GameObject imageList;

    public GameObject cameraFeed;
    public GameObject webcamDisplay;

    public Image updateBoxImg;

    public Text updateText;
    public Text cancelText;

    private CanvasGroup questionPopUpGrp;
    private CanvasGroup textInputGrp;
    private CanvasGroup okPopUpGrp;
    private CanvasGroup noButtonPopUpGrp;
    private CanvasGroup updateCancelPopUpGrp;
    private CanvasGroup profileListGrp;
    private CanvasGroup imageListGrp;
    private CanvasGroup cameraFeedGrp;
    private CanvasGroup updateImgGrp;
    private CanvasGroup objectImgGrp;

    private WebcamController webcamController;

    private Sprite savedSprite;

    void Awake()
    {
        // Enforce singleton pattern.
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Logger.Log("duplicate UIElementContainer, destroying");
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);
    }

    // Use this for initialization
    void Start ()
    {
        questionPopUpGrp = questionPopUp.GetComponent<CanvasGroup>();
        textInputGrp = textInput.GetComponent<CanvasGroup>();
        okPopUpGrp = okPopUp.GetComponent<CanvasGroup>();
        noButtonPopUpGrp = noButtonPopUp.GetComponent<CanvasGroup>();
        updateCancelPopUpGrp = updateCancelPopUp.GetComponent<CanvasGroup>();
        profileListGrp = profileListWindow.GetComponent<CanvasGroup>();
        imageListGrp = imageListWindow.GetComponent<CanvasGroup>();
        cameraFeedGrp = cameraFeed.GetComponent<CanvasGroup>();
        updateImgGrp = updateBoxImg.GetComponent<CanvasGroup>();
        objectImgGrp = trainingObjImgContainer.GetComponent<CanvasGroup>();

        webcamController = webcamDisplay.GetComponent<WebcamController>();
	}

    void Update()
    {
        HandleTaskQueue();
    }

    // Handle main task queue.
    private void HandleTaskQueue()
    {
        // Pop tasks from the task queue and perform them.
        // Tasks are added from other threads, usually in response to ROS msgs.
        if (this.taskQueue.Count > 0)
        {
            try
            {
                Logger.Log("Got a task from queue in UIAdjuster");
                this.taskQueue.Dequeue().Invoke();
            }
            catch (Exception e)
            {
                Logger.LogError("Error invoking action on main thread!\n" + e);
            }
        }
    }

    // Modifiers for the Yes/No window:

    private void HideQuestionPopUp()
    {
        questionPopUpGrp.alpha = 0;
        questionPopUpGrp.interactable = false;
        questionPopUp.gameObject.SetActive(false);
    }

    private void ShowQuestionPopUp()
    {
        questionPopUpGrp.alpha = 1;
        questionPopUpGrp.interactable = true;
        questionPopUp.gameObject.SetActive(true);
    }

    private void SetQuestionPopUpText(string s)
    {
        questionText.text = s;
    }

    // Modifiers for the Text input window:

    private void HideTextInput()
    {
        textInputGrp.alpha = 0;
        textInputGrp.interactable = false;
        textInput.gameObject.SetActive(false);
    }

    private void ShowTextInput()
    {
        textInputGrp.alpha = 1;
        textInputGrp.interactable = true;
        textInput.gameObject.SetActive(true);
    }

    private void SetTextInputPrompt(string s)
    {
        textInputPrompt.text = s;
    }

    public string GetTypedInput()
    {
        return inputField.text;
    }

    // Modifiers for the OK Button Dialogue window:

    private void HideOKPopUp()
    {
        okPopUpGrp.alpha = 0;
        okPopUpGrp.interactable = false;
        okPopUp.gameObject.SetActive(false);
    }

    private void ShowOKPopUp()
    {
        okPopUpGrp.alpha = 1;
        okPopUpGrp.interactable = true;
        okPopUp.gameObject.SetActive(true);
    }

    private void SetOKPopUpText(string s)
    {
        okDialogueText.text = s;
    }

    // Modifiers for the no-button Dialogue window:

    private void HideNoButtonPopUp()
    {
        noButtonPopUpGrp.alpha = 0;
        noButtonPopUpGrp.interactable = false;
        noButtonPopUp.gameObject.SetActive(false);
    }

    private void ShowNoButtonPopUp()
    {
        noButtonPopUpGrp.alpha = 1;
        noButtonPopUpGrp.interactable = true;
        noButtonPopUp.gameObject.SetActive(true);
    }

    private void SetNoButtonPopUpText(string s)
    {
        noButtonText.text = s;
    }

    private void SetNoButtonPopUpColor(Color c)
    {
        // get reference of the UI element
        GameObject panel = noButtonPopUp;

        Image img = panel.GetComponent<Image>();
        img.color = c;
    }

    private void HideTrainingObjectImage()
    {
        objectImgGrp.alpha = 0;
        trainingObjImgContainer.gameObject.SetActive(false);
    }

    private void ShowTrainingObjectImage()
    {
        objectImgGrp.alpha = 1;
        trainingObjImgContainer.gameObject.SetActive(true);
    }

    private void ChangeTrainingObjectImage(Sprite newImg, Vector2 location)
    {
        RectTransform rtImg = this.trainingObjImgContainer.transform.GetChild(0).GetComponent<RectTransform>();
        Image objImg = this.trainingObjImgContainer.transform.GetChild(0).GetComponent<Image>();
        AspectRatioFitter arf = this.trainingObjImgContainer.transform.GetChild(0).GetComponent<AspectRatioFitter>();
        arf.aspectRatio = newImg.rect.width / newImg.rect.height;
        objImg.sprite = newImg;
        rtImg.localScale = new Vector3(1, 1);

        RectTransform rt = trainingObjImgContainer.gameObject.GetComponent<RectTransform>();
        Rect panelRect = trainingObjImgContainer.transform.parent.GetComponent<RectTransform>().rect;

        // trainingObjImgContainer's size will be 95% of its parent "quadrant" (currently there are 9 quadrants)
        // objImg will be stretched to fit into its parent (trainingObjImgContainer) using an AspectRatioFitter
        float ratio = 0.95f;
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, ratio * Constants.W_MULT * panelRect.width);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, ratio * Constants.H_MULT * panelRect.height);
        rt.localPosition = Vector2.Scale(location, new Vector2(panelRect.width, panelRect.height));
    }

    private void SetNoButtonPopUpObject(Sprite img, Vector3 location)
    {
        this.HideTrainingObjectImage();
        if (img != null)
        {
            this.ChangeTrainingObjectImage(img, location);
        }
        this.ShowTrainingObjectImage();
    }

    // Modifiers for the Webcam/Image window:

    private void HideUpdateCancelPopUp()
    {
        updateCancelPopUpGrp.alpha = 0;
        updateCancelPopUpGrp.interactable = false;
        updateCancelPopUp.gameObject.SetActive(false);
    }

    private void ShowUpdateCancelButtonPopUp()
    {
        updateCancelPopUpGrp.alpha = 1;
        updateCancelPopUpGrp.interactable = true;
        updateCancelPopUp.gameObject.SetActive(true);
    }

    private void SetUpdateCancelPopUpText(string s)
    {
        updateCancelText.text = s;
    }

    // Modifiers for the Profile List window:

    private void HideProfileList()
    {
        profileListGrp.alpha = 0;
        profileListGrp.interactable = false;
        profileListWindow.gameObject.SetActive(false);

        ScrollableList list = profileList.GetComponent<ScrollableList>();
        list.SetItemActives(false);
    }

    private void ShowProfileList(bool showProfiles)
    {
        profileListGrp.alpha = 1;
        profileListGrp.interactable = true;
        profileListWindow.gameObject.SetActive(true);

        ScrollableList list = profileList.GetComponent<ScrollableList>();
        list.SetItemActives(showProfiles);
    }

    private void SetProfileListText(string s)
    {
        profileListText.text = s;
    }

    private void UpdateProfileList(List<GameController.Profile> profiles, int columns=0)
    {
        ScrollableList list = profileList.GetComponent<ScrollableList>();
        list.DisplayProfiles(profiles, columns);
    }

    // Modifiers for the Image List window:

    private void HideImageList()
    {
        imageListGrp.alpha = 0;
        imageListGrp.interactable = false;
        imageListWindow.gameObject.SetActive(false);

        ScrollableList list = imageList.GetComponent<ScrollableList>();
        list.SetItemActives(false);
    }

    private void ShowImageList(bool showImages)
    {
        imageListGrp.alpha = 1;
        imageListGrp.interactable = true;
        imageListWindow.gameObject.SetActive(true);

        ScrollableList list = imageList.GetComponent<ScrollableList>();
        list.SetItemActives(showImages);
    }

    private void SetImageListText(string s)
    {
        imageListText.text = s;
    }

    private void UpdateImageList(List<GameController.ProfileImage> profiles)
    {
        ScrollableList list = imageList.GetComponent<ScrollableList>();
        list.DisplayImages(profiles);
    }
    
    // Will need to turn off feed before hiding it
    private void HideCameraFeed()
    {
        cameraFeedGrp.alpha = 0;
        webcamController.DisableCamera();
    }

    // Will need to turn on feed before showing it
    private void ShowCameraFeed()
    {
        cameraFeedGrp.alpha = 1;
        webcamController.EnableCamera();
    }

    public void EnableCamera()
    {
        webcamController.EnableCamera();
    }

    public void DisableCamera()
    {
        webcamController.DisableCamera();
    }

    private void HideUpdateImage()
    {
        updateImgGrp.alpha = 0;
        updateBoxImg.gameObject.SetActive(false);
    }

    private void ShowUpdateImage()
    {
        updateImgGrp.alpha = 1;
        updateBoxImg.gameObject.SetActive(true);
    }

    private void ChangeUpdateImage(Sprite newImg)
    {
        updateBoxImg.sprite = newImg;
    }

    public void GrabCurrentWebcamFrame()
    {
        savedSprite = webcamController.GetFrame();
    }

    public Sprite GetCurrentSavedFrame()
    {
        return savedSprite;
    }

    private void SetUpdateButtonText(string changed)
    {
        updateText.text = changed;
    }

    private void SetCancelButtonText(string changed)
    {
        cancelText.text = changed;
    }

    private void SetProfileListBackButtonState(bool status)
    {
        // get references of the UI elements
        GameObject panel = profileListWindow;
        
        GameObject backBtn = panel.transform.GetChild(0).gameObject;
        GameObject listContainer = panel.transform.GetChild(2).gameObject;
        GameObject scrollBar = panel.gameObject.transform.GetChild(3).gameObject;

        // hide/show the button
        backBtn.SetActive(status);

        // change size of the list window + scrollbar
        RectTransform rtL = listContainer.GetComponent<RectTransform>();
        rtL.anchorMin = new Vector2(rtL.anchorMin.x, status ? 0.35f : 0.15f);

        RectTransform rtSb = scrollBar.GetComponent<RectTransform>();
        rtSb.anchorMin = new Vector2(rtSb.anchorMin.x, status ? 0.35f : 0.15f);
    }

    private void HideAllElements()
    {
        this.HideOKPopUp();
        this.HideImageList();
        this.HideTextInput();
        this.HideCameraFeed();
        this.HideProfileList();
        this.HideUpdateImage();
        this.HideNoButtonPopUp();
        this.HideQuestionPopUp();
        this.HideUpdateCancelPopUp();
    }

    public void HideAllElementsAction()
    {
        this.taskQueue.Enqueue(() => {
            this.HideAllElements();
        });
    }

    public void AskQuestionAction(string q)
    {
        this.taskQueue.Enqueue(() => {
            //hide everything not in use
            this.HideAllElements();

            //change values
            this.SetQuestionPopUpText(q);

            //show the window after changes are made
            this.ShowQuestionPopUp();
        });
    }

    public void PromptInputTextAction(string prompt)
    {
        this.taskQueue.Enqueue(() => {
            //hide everything not in use
            this.HideAllElements();

            //change values
            this.SetTextInputPrompt(prompt);

            //show the window after changes are made
            this.ShowTextInput();
        });
    }

    public void PromptOKDialogueAction(string prompt)
    {
        this.taskQueue.Enqueue(() => {
            //hide everything not in use
            this.HideAllElements();

            //change values
            this.SetOKPopUpText(prompt);

            //show the window after changes are made
            this.ShowOKPopUp();
        });
    }

    public void ListProfilesAction(string prompt, List<GameController.Profile> profiles, bool backButton=true)
    {
        this.taskQueue.Enqueue(() => {
            //hide everything not in use
            this.HideAllElements();

            //change values
            this.SetProfileListText(prompt);
            this.UpdateProfileList(profiles);
            this.SetProfileListBackButtonState(backButton);

            //show the window after changes are made
            this.ShowProfileList(true);
        });
    }

    public void ListImagesAction(string prompt, List<GameController.ProfileImage> profiles)
    {
        this.taskQueue.Enqueue(() => {
            //hide everything not in use
            this.HideAllElements();

            //change values
            this.SetImageListText(prompt);
            this.UpdateImageList(profiles);

            //show the window after changes are made
            this.ShowImageList(true);
        });
    }

    public void ShowWebcamAction(string prompt, string updateText = "Update", string cancelText = "Cancel")
    {
        this.taskQueue.Enqueue(() => {
            //hide everything not in use
            this.HideAllElements();

            //change values
            this.SetUpdateCancelPopUpText(prompt);
            this.SetUpdateButtonText(updateText);
            this.SetCancelButtonText(cancelText);

            //show the window after changes are made
            this.ShowUpdateCancelButtonPopUp();
            this.ShowCameraFeed();
        });
    }

    public void PicWindowAction(Sprite pic, string prompt, string updateText = "Update", string cancelText = "Cancel")
    {
        this.taskQueue.Enqueue(() => {
            //hide everything not in use
            this.HideAllElements();

            //change values
            this.SetUpdateCancelPopUpText(prompt);
            this.SetUpdateButtonText(updateText);
            this.SetCancelButtonText(cancelText);
            this.ChangeUpdateImage(pic);

            //show the window after changes are made
            this.ShowUpdateCancelButtonPopUp();
            this.ShowUpdateImage();
        });
    }

    public void PromptNoButtonPopUpAction(string prompt)
    {
        this.taskQueue.Enqueue(() => {
            //hide everything not in use
            this.HideAllElements();

            //change values
            this.SetNoButtonPopUpText(prompt);
            this.SetNoButtonPopUpColor(new Color(255, 255, 255, 100));
            this.SetNoButtonPopUpObject(null, new Vector3());

            //show the window after changes are made
            this.ShowNoButtonPopUp();
        });
    }    

    public void ShowObjectOnScreenAction(Sprite img, Vector2 location, Color color)
    {
        this.taskQueue.Enqueue(() => {
            //hide everything not in use
            this.HideAllElements();

            //change values
            this.SetNoButtonPopUpText("");
            this.SetNoButtonPopUpColor(color);
            this.SetNoButtonPopUpObject(img, location);

            this.ShowNoButtonPopUp();
            this.ShowTrainingObjectImage();
        });
    }
}
