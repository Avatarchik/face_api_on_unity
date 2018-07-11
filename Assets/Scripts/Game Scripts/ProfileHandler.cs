using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ProfileHandler : MonoBehaviour {




    public string newImagePath, newText;

    public Image profileImage;
    public Text profileText;

    public string profileID;

    private GameObject brain;

    private GameController controller;

    void Start()
    {
        brain = GameObject.Find("Brain");
        controller = brain.GetComponent<GameController>();
    }

    public void SetImgToDefault()
    {
        Texture2D tex = Resources.Load(GameController.UNKNOWN_IMG) as Texture2D;
        Sprite newImage = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0, 0));
        profileImage.sprite = newImage;
    }

    public void UpdateImage()
    {
        // Create a texture. Texture size does not matter, since
        // LoadImage will replace with with incoming image size.
        Texture2D tex = new Texture2D(2, 2);

        byte[] pngBytes = GetImageAsByteArray(newImagePath);
        // Load data into the texture.
        tex.LoadImage(pngBytes);
        
        Sprite newImage = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0, 0));
        profileImage.sprite = newImage;
    }

    public void UpdateText()
    {
        profileText.text = newText;
    }

    byte[] GetImageAsByteArray(string imageFilePath)
    {
        using (FileStream fileStream =
            new FileStream(imageFilePath, FileMode.Open, FileAccess.Read))
        {
            BinaryReader binaryReader = new BinaryReader(fileStream);
            return binaryReader.ReadBytes((int)fileStream.Length);
        }
    }

    public void WhenSelected()
    {
        string[] split = name.Split(':');
        if (split.Length < 2)
        {
            Debug.LogError("incorrect naming used for clickable GameObject. Name = " + name);
            return;
        }

        string label = name.Split(':')[0];
        string identifier = name.Split(':')[1].Substring(1);
        switch (label)
        {
            case "Image": controller.SelectPhoto(identifier); break;
            case "Profile": controller.LoginAreYouSure(identifier); break;
            default: Debug.LogError("Unknown profile type! Type = " + label); break;
        }
    }
}
