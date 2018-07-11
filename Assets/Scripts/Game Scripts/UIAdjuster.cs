using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityFaceIDHelper;

public class UIAdjuster : MonoBehaviour {

    // The singleton instance.
    public static UIAdjuster instance = null;

    public Canvas questionPopUp;
    public Text questionText;

    public Canvas textInput;
    public Text textInputPrompt;
    public InputField inputField;

    public Canvas okPopUp;
    public Text okDialogueText;

    public Canvas noButtonPopUp;
    public Text noButtonText;

    public Canvas updateCancelPopUp;
    public Text updateCancelText;

    public Canvas profileListWindow;
    public Text profileListText;
    public GameObject profileList;

    public Canvas imageListWindow;
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

    private WebcamController webcamController;

    void Awake()
    {
        // Enforce singleton pattern.
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Debug.Log("duplicate UIElementContainer, destroying");
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);
    }

    // Use this for initialization
    void Start () {
        questionPopUpGrp = questionPopUp.GetComponent<CanvasGroup>();
        textInputGrp = textInput.GetComponent<CanvasGroup>();
        okPopUpGrp = okPopUp.GetComponent<CanvasGroup>();
        noButtonPopUpGrp = noButtonPopUp.GetComponent<CanvasGroup>();
        updateCancelPopUpGrp = updateCancelPopUp.GetComponent<CanvasGroup>();
        profileListGrp = profileListWindow.GetComponent<CanvasGroup>();
        imageListGrp = imageListWindow.GetComponent<CanvasGroup>();
        cameraFeedGrp = cameraFeed.GetComponent<CanvasGroup>();
        updateImgGrp = updateBoxImg.GetComponent<CanvasGroup>();

        webcamController = webcamDisplay.GetComponent<WebcamController>();
	}

    // Modifiers for the Yes/No window:

    public void HideQuestionPopUp()
    {
        questionPopUpGrp.alpha = 0;
        questionPopUpGrp.interactable = false;
        questionPopUp.gameObject.SetActive(false);
    }

    public void ShowQuestionPopUp()
    {
        questionPopUpGrp.alpha = 1;
        questionPopUpGrp.interactable = true;
        questionPopUp.gameObject.SetActive(true);
    }

    public void SetQuestionPopUpText(string s)
    {
        questionText.text = s;
    }

    // Modifiers for the Text input window:

    public void HideTextInput()
    {
        textInputGrp.alpha = 0;
        textInputGrp.interactable = false;
        textInput.gameObject.SetActive(false);
    }

    public void ShowTextInput()
    {
        textInputGrp.alpha = 1;
        textInputGrp.interactable = true;
        textInput.gameObject.SetActive(true);
    }

    public void SetTextInputPrompt(string s)
    {
        textInputPrompt.text = s;
    }

    public string GetTypedInput()
    {
        return inputField.text;
    }

    // Modifiers for the OK Button Dialogue window:

    public void HideOKPopUp()
    {
        okPopUpGrp.alpha = 0;
        okPopUpGrp.interactable = false;
        okPopUp.gameObject.SetActive(false);
    }

    public void ShowOKPopUp()
    {
        okPopUpGrp.alpha = 1;
        okPopUpGrp.interactable = true;
        okPopUp.gameObject.SetActive(true);
    }

    public void SetOKPopUpText(string s)
    {
        okDialogueText.text = s;
    }

    // Modifiers for the no-button Dialogue window:

    public void HideNoButtonPopUp()
    {
        noButtonPopUpGrp.alpha = 0;
        noButtonPopUpGrp.interactable = false;
        noButtonPopUp.gameObject.SetActive(false);
    }

    public void ShowNoButtonPopUp()
    {
        noButtonPopUpGrp.alpha = 1;
        noButtonPopUpGrp.interactable = true;
        noButtonPopUp.gameObject.SetActive(true);
    }

    public void SetNoButtonPopUpText(string s)
    {
        noButtonText.text = s;
    }

    // Modifiers for the Webcam/Image window:

    public void HideUpdateCancelPopUp()
    {
        updateCancelPopUpGrp.alpha = 0;
        updateCancelPopUpGrp.interactable = false;
        updateCancelPopUp.gameObject.SetActive(false);
    }

    public void ShowUpdateCancelButtonPopUp()
    {
        updateCancelPopUpGrp.alpha = 1;
        updateCancelPopUpGrp.interactable = true;
        updateCancelPopUp.gameObject.SetActive(true);
    }

    public void SetUpdateCancelPopUpText(string s)
    {
        updateCancelText.text = s;
    }

    // Modifiers for the Profile List window:

    public void HideProfileList()
    {
        profileListGrp.alpha = 0;
        profileListGrp.interactable = false;
        profileListWindow.gameObject.SetActive(false);

        ScrollableList list = profileList.GetComponent<ScrollableList>();
        list.SetItemActives(false);
    }

    public void ShowProfileList(bool showProfiles)
    {
        profileListGrp.alpha = 1;
        profileListGrp.interactable = true;
        profileListWindow.gameObject.SetActive(true);

        ScrollableList list = profileList.GetComponent<ScrollableList>();
        list.SetItemActives(showProfiles);
    }

    public void SetProfileListText(string s)
    {
        profileListText.text = s;
    }

    public void UpdateProfileList(Dictionary<Tuple<string, string>, string> profiles)
    {
        ScrollableList list = profileList.GetComponent<ScrollableList>();
        list.LoadProfiles(profiles);
    }

    // Modifiers for the Image List window:

    public void HideImageList()
    {
        imageListGrp.alpha = 0;
        imageListGrp.interactable = false;
        imageListWindow.gameObject.SetActive(false);

        ScrollableList list = imageList.GetComponent<ScrollableList>();
        list.SetItemActives(false);
    }

    public void ShowImageList(bool showImages)
    {
        imageListGrp.alpha = 1;
        imageListGrp.interactable = true;
        imageListWindow.gameObject.SetActive(true);

        ScrollableList list = imageList.GetComponent<ScrollableList>();
        list.SetItemActives(showImages);
    }

    public void SetImageListText(string s)
    {
        imageListText.text = s;
    }

    public void UpdateImageList(Dictionary<Tuple<string, string>, string> profiles)
    {
        ScrollableList list = imageList.GetComponent<ScrollableList>();
        list.LoadImages(profiles);
    }
    
    // Will need to turn off feed before hiding it
    public void HideCameraFeed()
    {
        cameraFeedGrp.alpha = 0;
        webcamController.DisableCamera();
    }

    // Will need to turn on feed before showing it
    public void ShowCameraFeed()
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

    public void HideUpdateImage()
    {
        updateImgGrp.alpha = 0;
        updateBoxImg.gameObject.SetActive(false);
    }

    public void ShowUpdateImage()
    {
        updateImgGrp.alpha = 1;
        updateBoxImg.gameObject.SetActive(true);
    }

    public void ChangeUpdateImage(Sprite newImg)
    {
        updateBoxImg.sprite = newImg;
    }

    public Sprite GrabCurrentWebcamFrame()
    {
        return webcamController.GetFrame();
    }

    public void SetUpdateButtonText(string changed)
    {
        updateText.text = changed;
    }

    public void SetCancelButtonText(string changed)
    {
        cancelText.text = changed;
    }

    public void HideAllElements()
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
}
