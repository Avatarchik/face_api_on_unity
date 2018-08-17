using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ProfileHandler : MonoBehaviour {
    
    public Image profileImage;
    public Text profileText;

    public IScrollable item;
    public ScrollableType type;

    private GameController controller;

    void Start()
    {
        controller = GameController.instance;
    }

    public void Init()
    {
        this.UpdateText();
        this.UpdateImage();
    }

    public void SetImgToDefault()
    {
        Texture2D tex = Resources.Load(Constants.UNKNOWN_IMG_RSRC_PATH) as Texture2D;
        Sprite newImage = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0, 0));
        profileImage.sprite = newImage;
    }

    public void UpdateImage()
    {
        if (item.ImgPath == Constants.UNKNOWN_IMG_RSRC_PATH || item.ImgPath == "none")
        {
            this.SetImgToDefault();
            return;
        }

        // Create a texture. Texture size does not matter, since
        // LoadImage will replace with with incoming image size.
        Texture2D tex = new Texture2D(2, 2);

        byte[] pngBytes = GetImageAsByteArray(item.ImgPath);
        // Load data into the texture.
        tex.LoadImage(pngBytes);
        
        Sprite newImage = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0, 0));
        profileImage.sprite = newImage;
    }

    public void UpdateText()
    {
        profileText.text = item.DisplayName;
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
        switch (type)
        {
            case ScrollableType.Profile: controller.SelectProfile((GameController.Profile) item); break;
            //case ScrollableType.ProfileImage: controller.SelectPhoto((GameController.ProfileImage) item); break;
            default: Logger.LogError("Unknown ScrollableType type! Type = " + type.ToString()); break;
        }
    }

    public interface IScrollable
    {
        string ImgPath { get; }

        string DisplayName { get; }

        string IdentifyingName { get; }
    }

    public enum ScrollableType
    {
        Profile,        // GameController.Profile
        ProfileImage    // GameController.ProfileImage
    }
}
