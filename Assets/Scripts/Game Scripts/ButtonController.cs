using System.Collections.Generic;
using UnityEngine;

public class ButtonController : MonoBehaviour {

    private GameController controller;
    private UIAdjuster adjuster;

    private Dictionary<string, object> parameters;

    void Start()
    {
        controller = GameController.instance;
        adjuster = UIAdjuster.instance;
    }

    public void OnClickYes()
    {
        Logger.Log("Pressed Yes");
        switch (controller.GetGameState())
        {
            //case GameState.STARTED: controller.AddTask(GameState.NEW_PROFILE_PROMPT); break;
            //case GameState.NEW_PROFILE_PROMPT: controller.AddTask(GameState.ENTER_NAME_PROMPT); break;
            default: Logger.LogWarning("Unhandled button press! Button = Yes, game state = " + controller.GetGameState()); break;   
        }
    }

    public void OnClickNo()
    {
        Logger.Log("Pressed No");
        switch (controller.GetGameState())
        {
            case GameState.STARTED: controller.AddTask(GameState.LISTING_PROFILES); break;
            //case GameState.NEW_PROFILE_PROMPT: controller.AddTask(GameState.MUST_LOGIN_PROMPT); break;
            default: Logger.LogWarning("Unhandled button press! Button = No, game state = " + controller.GetGameState()); break;
        }
    }

    public void OnClickDoneTyping()
    {
        Logger.Log("Pressed done");
        switch (controller.GetGameState())
        {
          /*case GameState.ENTER_NAME_PROMPT:
                string typed = adjuster.GetTypedInput();

                parameters = new Dictionary<string, object>
                {
                    { "typedName", typed }
                };

                controller.AddTask(GameState.EVALUATING_TYPED_NAME, parameters);
                break;*/
            default: Logger.LogWarning("Unhandled button press! Button = Done, game state = " + controller.GetGameState()); break;
        }
    }

    public void OnClickOK()
    {
        Logger.Log("Pressed OK");
        switch (controller.GetGameState())
        {
            // old (commented): 
            //case GameState.MUST_LOGIN_PROMPT: controller.AddTask(GameState.STARTED); break;

            //case GameState.WELCOME_SCREEN: controller.AddTask(GameState.LISTING_IMAGES); break;
            //case GameState.TAKING_WEBCAM_PIC: controller.AddTask(GameState.STARTED); break;

            // cases in which user is rejected by the Identify call
            //case GameState.REJECTION_PROMPT: controller.AddTask(GameState.STARTED); break;

            // current solution when receiving an API error: start over and hope it goes away :P
            case GameState.API_ERROR_CREATE: 
            case GameState.API_ERROR_COUNTING_FACES:
            case GameState.API_ERROR_DELETING_FACE:
            case GameState.API_ERROR_ADDING_FACE:
            case GameState.API_ERROR_IDENTIFYING:
            case GameState.API_ERROR_GET_NAME:
            case GameState.API_ERROR_TRAINING_STATUS: controller.AddTask(GameState.STARTED); break;
            default: Logger.LogWarning("Unhandled button press! Button = OK, game state = " + controller.GetGameState()); break;
        }
    }

    public void OnClickUpdate()
    {
        Logger.Log("Pressed Update");
        switch (controller.GetGameState())
        {
            case GameState.LOGIN_DOUBLE_CHECK:

                parameters = new Dictionary<string, object>
                {
                    { "profile", controller.GetSelectedProfile() }
                };

                controller.AddTask(GameState.LOGGING_IN, parameters);
                break;
            // old (commented): 
          /*case GameState.TAKING_WEBCAM_PIC:

                adjuster.GrabCurrentWebcamFrame();

                Sprite frame = adjuster.GetCurrentSavedFrame();

                parameters = new Dictionary<string, object>
                {
                    { "photo", frame }
                };

                controller.AddTask(GameState.CHECKING_TAKEN_PIC, parameters); 
                break;
            case GameState.PIC_APPROVAL:

                Sprite pic = adjuster.GetCurrentSavedFrame(); //risky assumption that CurrentSavedFrame hasn't changed

                parameters = new Dictionary<string, object>
                {
                    { "photo", pic }
                };

                controller.AddTask(GameState.SAVING_PIC, parameters);
                break;
            case GameState.PIC_DISAPPROVAL: controller.AddTask(GameState.TAKING_WEBCAM_PIC); break;
            case GameState.LOGIN_DOUBLE_CHECK:

                parameters = new Dictionary<string, object>
                {
                    { "profile", controller.GetSelectedProfile() }
                };

                controller.AddTask(GameState.LOGGING_IN, parameters); 
                break;
            case GameState.SHOWING_SELECTED_PHOTO:
                parameters = new Dictionary<string, object>
                {
                    { "profileImg", controller.GetSelectedProfileImage() }
                };

                controller.AddTask(GameState.DELETING_PHOTO, parameters);
                break;*/
            default: Logger.LogWarning("Unhandled button press! Button = Update, game state = " + controller.GetGameState()); break;
        }
    }

    public void OnClickCancel()
    {
        Logger.Log("Pressed Cancel");
        switch (controller.GetGameState())
        {
            //case GameState.PIC_APPROVAL: controller.AddTask(GameState.TAKING_WEBCAM_PIC); break;
            case GameState.LOGIN_DOUBLE_CHECK: controller.AddTask(GameState.CANCELLING_LOGIN); break;
                
            //case GameState.SHOWING_SELECTED_PHOTO:
            //case GameState.TAKING_WEBCAM_PIC:
            //case GameState.PIC_DISAPPROVAL: controller.AddTask(GameState.LISTING_IMAGES); break;
            default: Logger.LogWarning("Unhandled button press! Button = Cancel, game state = " + controller.GetGameState()); break;
        }
    }

    public void OnClickAdd()
    {
        Logger.Log("Pressed Add");
        switch (controller.GetGameState())
        {
            //case GameState.LISTING_IMAGES: controller.AddTask(GameState.TAKING_WEBCAM_PIC); break;
            default: Logger.LogWarning("Unhandled button press! Button = Add, game state = " + controller.GetGameState()); break;
        }
    }

    public void OnClickLogOff()
    {
        Logger.Log("Pressed LogOff");
        switch (controller.GetGameState())
        {
            //case GameState.LISTING_IMAGES: controller.AddTask(GameState.STARTED); break;
            default: Logger.LogWarning("Unhandled button press! Button = LogOff, game state = " + controller.GetGameState()); break;
        }
    }

    public void OnClickBack()
    {
        Logger.Log("Pressed Back");
        switch (controller.GetGameState())
        {
            case GameState.LISTING_PROFILES: controller.AddTask(GameState.STARTED); break;
            default: Logger.LogWarning("Unhandled button press! Button = Back, game state = " + controller.GetGameState()); break;
        }
    }

    public void OnClickCancelTyping()
    {
        Logger.Log("Pressed Cancel (on text input window)");
        switch (controller.GetGameState())
        {
            //case GameState.ENTER_NAME_PROMPT: controller.AddTask(GameState.STARTED); break;
            default: Logger.LogWarning("Unhandled button press! Button = Cancel (on text input window), game state = " + controller.GetGameState()); break;
        }
    }

}
