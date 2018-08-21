using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ObjectShaker : MonoBehaviour {

    // The singleton instance.
    public static ObjectShaker instance = null;

    private float delay = 1.0f * Constants.TRAINING_IMG_ROT_DELAY_MS;

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
        transform.Rotate(new Vector3(0, 0, 30.0f));
	}
	
	// Update is called once per frame
	void Update()
    {
        if (delay > 0)
        {
            delay -= Time.deltaTime;
            return;
        }
        
        Vector3 rot = -transform.rotation.eulerAngles;
        transform.Rotate(rot == Vector3.zero ? new Vector3(0, 0, 30.0f) : rot);
        delay = 1.0f * Constants.TRAINING_IMG_ROT_DELAY_MS;
	}
}
