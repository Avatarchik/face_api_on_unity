using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// Script adapted from http://answers.unity.com/answers/1155328/view.html

public class WebcamController : MonoBehaviour
{
	public RawImage image;
	public RectTransform imageParent;
	public AspectRatioFitter imageFitter;

	// Device cameras
	WebCamDevice cameraDevice;

	WebCamTexture cameraTexture;

	// Image rotation
	Vector3 rotationVector = new Vector3(0f, 0f, 0f);

	// Image uvRect
	Rect defaultRect = new Rect(0f, 0f, 1f, 1f);
	Rect fixedRect = new Rect(0f, 1f, 1f, -1f);

	// Image Parent's scale
	Vector3 defaultScale = new Vector3(1f, 1f, 1f);
	Vector3 fixedScale = new Vector3(-1f, 1f, 1f);

	public float scaleMultiplier;

    private static bool camEnabled = false;

	void Start()
	{
		// Check for device cameras
		if (WebCamTexture.devices.Length == 0)
		{
			Debug.Log("No devices cameras found");
			return;
		}

		// Get the device's cameras and create WebCamTextures with them
		cameraDevice = WebCamTexture.devices.Last();
		cameraTexture = new WebCamTexture(cameraDevice.name);

		// Set camera filter modes for a smoother looking image
		cameraTexture.filterMode = FilterMode.Trilinear;

		// Set the camera to use by default
		SetActiveCamera(cameraTexture);
	}

	// Set the device camera to use and start it
	public void SetActiveCamera(WebCamTexture cameraToUse)
	{

		image.texture = cameraToUse;
		image.material.mainTexture = cameraToUse;

        if (camEnabled)
            EnableCamera();
	}

    public void EnableCamera()
    {
        camEnabled = true;
        if (cameraTexture != null)
        {
            cameraTexture.Play();
        }
        else
        {
            Debug.Log("Camera texture is null!");
        }
    }

    public void DisableCamera()
    {
        camEnabled = false;
        if (cameraTexture != null)
        {
            cameraTexture.Stop();
        }
    }

    public Sprite GetFrame()
    {
        Texture2D textureFromCamera = new Texture2D(cameraTexture.width,
                                                    cameraTexture.height);
        textureFromCamera.SetPixels(cameraTexture.GetPixels());
        textureFromCamera.Apply();

        Sprite newImage = Sprite.Create(textureFromCamera, new Rect(0, 0, textureFromCamera.width, textureFromCamera.height), new Vector2(0, 0));
        return newImage;
    }

	// Make adjustments to image every frame to be safe, since Unity isn't 
	// guaranteed to report correct data as soon as device camera is started
	void Update()
	{
        Debug.Log("camEnabled: " + camEnabled);
        if (camEnabled)
        {
            // Skip making adjustment for incorrect camera data
            if (cameraTexture.width < 100)
            {
                Debug.Log("Still waiting another frame for correct info...");
                return;
            }

            // Rotate image to show correct orientation 
            rotationVector.z = -cameraTexture.videoRotationAngle;
            image.rectTransform.localEulerAngles = rotationVector;

            // Set AspectRatioFitter's ratio
            float videoRatio =
                (float)cameraTexture.width / (float)cameraTexture.height;
            imageFitter.aspectRatio = videoRatio;

            // Unflip if vertically flipped
            image.uvRect =
                cameraTexture.videoVerticallyMirrored ? fixedRect : defaultRect;

            // Mirror front-facing camera's image horizontally to look more natural
            imageParent.localScale =
                cameraDevice.isFrontFacing ? fixedScale * scaleMultiplier : defaultScale * scaleMultiplier;
        }
        else
        {
            return;
        }
	}
}