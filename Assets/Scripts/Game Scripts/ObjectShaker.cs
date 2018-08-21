using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ObjectShaker : MonoBehaviour {

    // The singleton instance.
    public static ObjectShaker instance = null;

    private float delay = Constants.TRAINING_IMG_ROT_DELAY_MS / 1000.0f;
    private bool CCW = false;

    private readonly float degOfRotation = 15.0f;

    void Awake()
    {
        // Enforce singleton pattern.
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Logger.Log("duplicate ObjectShaker, destroying");
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);
    }

    // Use this for initialization
    void Start()
    {
	}
	
	// Update is called once per frame
	void Update()
    {
        if (delay > 0)
        {
            delay -= Time.deltaTime;
            return;
        }
        if (transform.rotation.eulerAngles == Vector3.zero) transform.Rotate(new Vector3(0, 0, -degOfRotation));

        Vector3 rot = new Vector3(0, 0, 2*degOfRotation);
        rot *= CCW ? -1 : 1;
        transform.Rotate(rot);
        CCW = !CCW;
        delay = Constants.TRAINING_IMG_ROT_DELAY_MS / 1000.0f;
	}
}
