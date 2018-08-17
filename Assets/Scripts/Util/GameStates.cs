public enum GameState
{
    // each entry's number value isn't really significant, as long it is unique
    // I tried to organize them as such:
    //  900s represent commands FROM the ROS controller
    //  800s represent requests TO the ROS controller
    //   90s represent Face API errors
    // 9000s represent internal errors (within the unity app)
    // everything else is a Face ID game state
    
    GAMECONTROLLER_STARTING = -1,   //just for debug
    
    ROS_CONNECTION = 0,
    ROS_HELLO_WORLD_ACK = 900,
    ROS_ASK_GROUP_ID = 801,
    ROS_RECEIVED_GROUP_ID = 901,

    // training FSM:
    ROS_ASK_TO_RETRAIN = 802,
   
    ROS_TRAINING_RECEIVED_OBJECT_REQ = 903,
    ROS_TRAINING_SEND_OBJECT_READY = 803,

    ROS_TRAINING_RECEIVED_START_TAKING_PICS = 904,
    ROS_TRAINING_SEND_LOCATION_DONE = 804,

    ROS_TRAINING_RECEIVED_FINISHED = 905,

    // new:
    STARTED = 1,
    LISTING_PROFILES = 2,
    LOGIN_DOUBLE_CHECK = 3,
    CANCELLING_LOGIN = 4,
    LOGGING_IN = 5,
    DECIDING_TO_RETRAIN = 6,


    SAVING_PIC = 11,

    API_ERROR_CREATE = 90,
    API_ERROR_COUNTING_FACES = 91,
    API_ERROR_ADDING_FACE = 92,
    API_ERROR_IDENTIFYING = 93,
    API_ERROR_GET_NAME = 94,
    API_ERROR_TRAINING_STATUS = 95,
    API_ERROR_DELETING_FACE = 96,

    INTERNAL_ERROR_PARSING = 9000,
    INTERNAL_ERROR_NAME_FROM_ID = 9001,
    INTERNAL_ERROR_MISSING_PROFILEDATA = 9002
}