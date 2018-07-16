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
            Logger.Log("duplicate UIElementContainer, destroying");
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

    private void UpdateProfileList(Dictionary<Tuple<string, string>, string> profiles)
    {
        ScrollableList list = profileList.GetComponent<ScrollableList>();
        list.LoadProfiles(profiles);
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

    private void UpdateImageList(Dictionary<Tuple<string, string>, string> profiles)
    {
        ScrollableList list = imageList.GetComponent<ScrollableList>();
        list.LoadImages(profiles);
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

    public Sprite GrabCurrentWebcamFrame()
    {
        return webcamController.GetFrame();
    }

    private void SetUpdateButtonText(string changed)
    {
        updateText.text = changed;
    }

    private void SetCancelButtonText(string changed)
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

    public void AskQuestion(string q)
    {
        //hide everything not in use
        this.HideAllElements();

        //change values
        this.SetQuestionPopUpText(q);

        //show the window after changes are made
        this.ShowQuestionPopUp();
    }

    public void PromptInputText(string prompt)
    {
        //hide everything not in use
        this.HideAllElements();

        //change values
        this.SetTextInputPrompt(prompt);

        //show the window after changes are made
        this.ShowTextInput();
    }

    public void PromptOKDialogue(string prompt)
    {
        //hide everything not in use
        this.HideAllElements();

        //change values
        this.SetOKPopUpText(prompt);

        //show the window after changes are made
        this.ShowOKPopUp();
    }

    public void ListProfiles(string prompt, Dictionary<Tuple<string, string>, string> profiles)
    {
        //hide everything not in use
        this.HideAllElements();

        //change values
        this.SetProfileListText(prompt);
        this.UpdateProfileList(profiles);

        //show the window after changes are made
        this.ShowProfileList(true);
    }

    public void ListImages(string prompt, Dictionary<Tuple<string, string>, string> profiles)
    {
        //hide everything not in use
        this.HideAllElements();

        //change values
        this.SetImageListText(prompt);
        this.UpdateImageList(profiles);

        //show the window after changes are made
        this.ShowImageList(true);
    }

    public void ShowWebcam(string prompt, string updateText = "Update", string cancelText = "Cancel")
    {
        //hide everything not in use
        this.HideAllElements();

        //change values
        this.SetUpdateCancelPopUpText(prompt);
        this.SetUpdateButtonText(updateText);
        this.SetCancelButtonText(cancelText);

        //show the window after changes are made
        this.ShowUpdateCancelButtonPopUp();
        this.ShowCameraFeed();
    }

    public void PicWindow(Sprite pic, string prompt, string updateText = "Update", string cancelText = "Cancel")
    {
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
    }

    public void PromptNoButtonPopUp(string prompt)
    {
        //hide everything not in use
        this.HideAllElements();

        //change values
        this.SetNoButtonPopUpText(prompt);

        //show the window after changes are made
        this.ShowNoButtonPopUp();
    }
}
