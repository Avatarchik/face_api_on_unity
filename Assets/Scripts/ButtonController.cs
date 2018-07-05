using UnityEngine;

public class ButtonController : MonoBehaviour {

    public GameObject brain;

    private GameController controller;

    void Start()
    {
        controller = brain.GetComponent<GameController>();
    }

    public void OnClickYes()
    {
        Debug.Log("Pressed Yes");
        switch (controller.GetGameState())
        {
            case "started": controller.ChangeState("newPrompt"); break;
            case "newPrompt": controller.ChangeState("profileName"); break;
            default: Debug.Log("Unhandled button press! Button = Yes, game state = " + controller.GetGameState()); break;   
        }
    }

    public void OnClickNo()
    {
        Debug.Log("Pressed No");
        switch (controller.GetGameState())
        {
            case "started": controller.ChangeState("listProfiles"); break;
            case "newPrompt": controller.ChangeState("mustLogin"); break;
            default: Debug.Log("Unhandled button press! Button = No, game state = " + controller.GetGameState()); break;
        }
    }

    public void OnClickDoneTyping()
    {
        Debug.Log("Pressed done");
        switch (controller.GetGameState())
        {
            case "profileName": controller.ChangeState("afterNameTyped"); break;
            default: Debug.Log("Unhandled button press! Button = Done, game state = " + controller.GetGameState()); break;
        }
    }

    public void OnClickOK()
    {
        Debug.Log("Pressed OK");
        switch (controller.GetGameState())
        {
            case "mustLogin": controller.ChangeState("started"); break;
            case "loggingIn": controller.ChangeState("started"); break;    //meaning that the auth rejected them
            case "loggedIn": controller.ChangeState("listFaceImages"); break;
            case "addingImgWebcam": controller.ChangeState("started"); break;
            case "deletePhoto": controller.ChangeState("started"); break;  //meaning that the auth rejected them

            // current solution when receiving an API error: start over and hope it goes away :P
            case "apiErrorCreate": 
            case "apiErrorCountingFaces":
            case "apiErrorDeletingFace":
            case "apiErrorAddingFace":
            case "apiErrorIdentifying":
            case "apiErrorGetNameAfterRejection":
            case "apiErrorTrainingStatus": controller.ChangeState("started"); break;
            default: Debug.Log("Unhandled button press! Button = OK, game state = " + controller.GetGameState()); break;
        }
    }

    public void OnClickUpdate()
    {
        Debug.Log("Pressed Update");
        switch (controller.GetGameState())
        {
            case "addingImgWebcam": controller.ChangeState("addingImgCheckPic"); break;
            case "addingImgShowPic": controller.ChangeState("addingImgSaving"); break;
            case "addingImgTryAgain": controller.ChangeState("addingImgWebcam"); break;
            case "loginAreYouSure": controller.ChangeState("loggingIn"); break;
            case "photoSelected": controller.ChangeState("deletePhoto"); break;
            default: Debug.Log("Unhandled button press! Button = Update, game state = " + controller.GetGameState()); break;
        }
    }

    public void OnClickCancel()
    {
        Debug.Log("Pressed Cancel");
        switch (controller.GetGameState())
        {
            case "addingImgShowPic": controller.ChangeState("addingImgWebcam"); break;
            case "loginAreYouSure": controller.ChangeState("cancelLogin"); break;
            case "photoSelected": controller.ChangeState("listFaceImages"); break;
            case "addingImgWebcam": controller.ChangeState("listFaceImages"); break;
            case "addingImgTryAgain": controller.ChangeState("listFaceImages"); break;
            default: Debug.Log("Unhandled button press! Button = Cancel, game state = " + controller.GetGameState()); break;
        }
    }

    public void OnClickAdd()
    {
        Debug.Log("Pressed Add");
        switch (controller.GetGameState())
        {
            case "listFaceImages": controller.ChangeState("addingImgWebcam"); break;
            default: Debug.Log("Unhandled button press! Button = Add, game state = " + controller.GetGameState()); break;
        }
    }

    public void OnClickLogOff()
    {
        Debug.Log("Pressed LogOff");
        switch (controller.GetGameState())
        {
            case "listFaceImages": controller.ChangeState("started"); break;
            default: Debug.Log("Unhandled button press! Button = LogOff, game state = " + controller.GetGameState()); break;
        }
    }

    public void OnClickBack()
    {
        Debug.Log("Pressed Back");
        switch (controller.GetGameState())
        {
            case "listProfiles": controller.ChangeState("started"); break;
            default: Debug.Log("Unhandled button press! Button = Back, game state = " + controller.GetGameState()); break;
        }
    }

    public void OnClickCancelTyping()
    {
        Debug.Log("Pressed Cancel (on text input window)");
        switch (controller.GetGameState())
        {
            case "profileName": controller.ChangeState("started"); break;
            default: Debug.Log("Unhandled button press! Button = Cancel (on text input window), game state = " + controller.GetGameState()); break;
        }
    }

}
