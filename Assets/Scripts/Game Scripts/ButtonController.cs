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
        Logger.Log("Pressed Yes");
        switch (controller.GetGameState())
        {
            case GameState.STARTED: controller.AddTask(GameState.NEW_PROFILE_PROMPT); break;
            case GameState.NEW_PROFILE_PROMPT: controller.AddTask(GameState.ENTER_NAME_PROMPT); break;
            default: Logger.Log("Unhandled button press! Button = Yes, game state = " + controller.GetGameState()); break;   
        }
    }

    public void OnClickNo()
    {
        Logger.Log("Pressed No");
        switch (controller.GetGameState())
        {
            case GameState.STARTED: controller.AddTask(GameState.LISTING_PROFILES); break;
            case GameState.NEW_PROFILE_PROMPT: controller.AddTask(GameState.MUST_LOGIN_PROMPT); break;
            default: Logger.Log("Unhandled button press! Button = No, game state = " + controller.GetGameState()); break;
        }
    }

    public void OnClickDoneTyping()
    {
        Logger.Log("Pressed done");
        switch (controller.GetGameState())
        {
            case GameState.ENTER_NAME_PROMPT: controller.AddTask(GameState.EVALUATING_TYPED_NAME); break;
            default: Logger.Log("Unhandled button press! Button = Done, game state = " + controller.GetGameState()); break;
        }
    }

    public void OnClickOK()
    {
        Logger.Log("Pressed OK");
        switch (controller.GetGameState())
        {
            case GameState.MUST_LOGIN_PROMPT: controller.AddTask(GameState.STARTED); break;

            case GameState.WELCOME_SCREEN: controller.AddTask(GameState.LISTING_IMAGES); break;
            case GameState.TAKING_WEBCAM_PIC: controller.AddTask(GameState.STARTED); break;

            // cases in which user is rejected by the Identify call
            case GameState.LOGGING_IN:
            case GameState.CHECKING_TAKEN_PIC:
            case GameState.DELETING_PHOTO: controller.AddTask(GameState.STARTED); break;

            // current solution when receiving an API error: start over and hope it goes away :P
            case GameState.API_ERROR_CREATE: 
            case GameState.API_ERROR_COUNTING_FACES:
            case GameState.API_ERROR_DELETING_FACE:
            case GameState.API_ERROR_ADDING_FACE:
            case GameState.API_ERROR_IDENTIFYING:
            case GameState.API_ERROR_GET_NAME:
            case GameState.API_ERROR_TRAINING_STATUS: controller.AddTask(GameState.STARTED); break;
            default: Logger.Log("Unhandled button press! Button = OK, game state = " + controller.GetGameState()); break;
        }
    }

    public void OnClickUpdate()
    {
        Logger.Log("Pressed Update");
        switch (controller.GetGameState())
        {
            case GameState.TAKING_WEBCAM_PIC: controller.AddTask(GameState.CHECKING_TAKEN_PIC); break;
            case GameState.PIC_APPROVAL: controller.AddTask(GameState.SAVING_PIC); break;
            case GameState.PIC_DISAPPROVAL: controller.AddTask(GameState.TAKING_WEBCAM_PIC); break;
            case GameState.LOGIN_DOUBLE_CHECK: controller.AddTask(GameState.LOGGING_IN); break;
            case GameState.SHOWING_SELECTED_PHOTO: controller.AddTask(GameState.DELETING_PHOTO); break;
            default: Logger.Log("Unhandled button press! Button = Update, game state = " + controller.GetGameState()); break;
        }
    }

    public void OnClickCancel()
    {
        Logger.Log("Pressed Cancel");
        switch (controller.GetGameState())
        {
            case GameState.PIC_APPROVAL: controller.AddTask(GameState.TAKING_WEBCAM_PIC); break;
            case GameState.LOGIN_DOUBLE_CHECK: controller.AddTask(GameState.CANCELLING_LOGIN); break;
                
            case GameState.SHOWING_SELECTED_PHOTO:
            case GameState.TAKING_WEBCAM_PIC:
            case GameState.PIC_DISAPPROVAL: controller.AddTask(GameState.LISTING_IMAGES); break;
            default: Logger.Log("Unhandled button press! Button = Cancel, game state = " + controller.GetGameState()); break;
        }
    }

    public void OnClickAdd()
    {
        Logger.Log("Pressed Add");
        switch (controller.GetGameState())
        {
            case GameState.LISTING_IMAGES: controller.AddTask(GameState.TAKING_WEBCAM_PIC); break;
            default: Logger.Log("Unhandled button press! Button = Add, game state = " + controller.GetGameState()); break;
        }
    }

    public void OnClickLogOff()
    {
        Logger.Log("Pressed LogOff");
        switch (controller.GetGameState())
        {
            case GameState.LISTING_IMAGES: controller.AddTask(GameState.STARTED); break;
            default: Logger.Log("Unhandled button press! Button = LogOff, game state = " + controller.GetGameState()); break;
        }
    }

    public void OnClickBack()
    {
        Logger.Log("Pressed Back");
        switch (controller.GetGameState())
        {
            case GameState.LISTING_PROFILES: controller.AddTask(GameState.STARTED); break;
            default: Logger.Log("Unhandled button press! Button = Back, game state = " + controller.GetGameState()); break;
        }
    }

    public void OnClickCancelTyping()
    {
        Logger.Log("Pressed Cancel (on text input window)");
        switch (controller.GetGameState())
        {
            case GameState.ENTER_NAME_PROMPT: controller.AddTask(GameState.STARTED); break;
            default: Logger.Log("Unhandled button press! Button = Cancel (on text input window), game state = " + controller.GetGameState()); break;
        }
    }

}
