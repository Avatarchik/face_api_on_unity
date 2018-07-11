﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

using System.Linq;
using System;

public class ScrollableList : MonoBehaviour
{
    public GameObject itemPrefab;
    private int itemCount, columnCount;

    private Dictionary<string, GameObject> listedItems;

    public void SetItemActives(bool active)
    {
        if (listedItems != null)
        {
            foreach (KeyValuePair<string, GameObject> entry in listedItems)
            {
                entry.Value.gameObject.SetActive(active);
            }
        }
    }

    public void LoadProfiles(Dictionary<Tuple<string, string>, string> profiles, int columns = 0)
    {
        LoadObjects("Profile", profiles, columns);
    }

    public void LoadImages(Dictionary<Tuple<string, string>, string> profiles, int columns = 0)
    {
        LoadObjects("Image", profiles, columns);
    }

    private void ClearOldList()
    {
        foreach (GameObject obj in listedItems.Values)
        {
            Destroy(obj);
        }
        listedItems = new Dictionary<string, GameObject>();
    }

    private void LoadObjects(string label, Dictionary<Tuple<string, string>, string> profiles, int columns = 0)
    {
        if (listedItems != null)
            ClearOldList();
        else
            listedItems = new Dictionary<string, GameObject>();

        itemCount = profiles.Keys.Count;
        columnCount = (columns == 0) ? 2 : columns; //arbitrary choice tbh

        RectTransform rowRectTransform = itemPrefab.GetComponent<RectTransform>();
        RectTransform containerRectTransform = gameObject.GetComponent<RectTransform>();

        //calculate the width and height of each child item.
        float width = containerRectTransform.rect.width / columnCount;
        float ratio = width / rowRectTransform.rect.width;
        float height = rowRectTransform.rect.height * ratio;
        int rowCount = itemCount / columnCount;

        if (rowCount == 0)
            rowCount = columnCount;

        if (itemCount % rowCount > 0)
            rowCount++;

        //adjust the height of the container so that it will just barely fit all its children
        float scrollHeight = height * rowCount;
        containerRectTransform.offsetMin = new Vector2(containerRectTransform.offsetMin.x, -scrollHeight / 2);
        containerRectTransform.offsetMax = new Vector2(containerRectTransform.offsetMax.x, scrollHeight / 2);

        // ------------------------------------- VV RELEVANT DYNAMIC CODE HERE VV -------------------------------------

        string[] imgPaths = profiles.Values.ToArray();
        Tuple<string, string>[] profileData = profiles.Keys.ToArray();    //left is display name, right is identifying name

        // ------------------------------------- ^^ RELEVANT DYNAMIC CODE HERE ^^ -------------------------------------

        int j = 0;
        for (int i = 0; i < itemCount; i++)
        {
            //this is used instead of a double for loop because itemCount may not fit perfectly into the rows/columns
            if (i % columnCount == 0)
                j++;

            // ------------------------------------- VV RELEVANT DYNAMIC CODE HERE VV -------------------------------------

            string path = imgPaths[i];
            string displayName = profileData[i].Item1;
            string identifyingName = profileData[i].Item2;

            //create a new item, name it, and set the parent
            GameObject newItem = Instantiate(itemPrefab) as GameObject;
            //newItem.name = gameObject.name + " item at (" + i + "," + j + ")";
            newItem.name = label + ": " + identifyingName;

            ProfileHandler handler = newItem.GetComponent<ProfileHandler>();

            //example of a unknown img path:
            // "(0) " + GameController.UNKNOWN_IMG

            int index = path.IndexOf(')');
            if (index > 0 && path.Substring(index + 2) == GameController.UNKNOWN_IMG)
            {
                handler.SetImgToDefault();
            }
            else
            {
                handler.newImagePath = path;
                handler.UpdateImage();
            }

            handler.newText = displayName;
            handler.UpdateText();

            newItem.transform.SetParent(gameObject.transform);
            listedItems.Add(identifyingName, newItem);

            // ------------------------------------- ^^ RELEVANT DYNAMIC CODE HERE ^^ -------------------------------------

            //move and size the new item
            RectTransform rectTransform = newItem.GetComponent<RectTransform>();

            float x = -containerRectTransform.rect.width / 2 + width * (i % columnCount);
            float y = containerRectTransform.rect.height / 2 - height * j;
            rectTransform.offsetMin = new Vector2(x, y);

            x = rectTransform.offsetMin.x + width;
            y = rectTransform.offsetMin.y + height;
            rectTransform.offsetMax = new Vector2(x, y);

            containerRectTransform.pivot = new Vector2(containerRectTransform.pivot.x, 1);
        }
    }

    void LoadTest()
    {
        itemCount = 10;
        columnCount = 1;
        RectTransform rowRectTransform = itemPrefab.GetComponent<RectTransform>();
        RectTransform containerRectTransform = gameObject.GetComponent<RectTransform>();

        //calculate the width and height of each child item.
        float width = containerRectTransform.rect.width / columnCount;
        float ratio = width / rowRectTransform.rect.width;
        float height = rowRectTransform.rect.height * ratio;
        int rowCount = itemCount / columnCount;
        if (itemCount % rowCount > 0)
            rowCount++;

        //adjust the height of the container so that it will just barely fit all its children
        float scrollHeight = height * rowCount;
        containerRectTransform.offsetMin = new Vector2(containerRectTransform.offsetMin.x, -scrollHeight / 2);
        containerRectTransform.offsetMax = new Vector2(containerRectTransform.offsetMax.x, scrollHeight / 2);

        int j = 0;
        for (int i = 0; i < itemCount; i++)
        {
            //this is used instead of a double for loop because itemCount may not fit perfectly into the rows/columns
            if (i % columnCount == 0)
                j++;

            // ------------------------------------- VV RELEVANT DYNAMIC CODE HERE VV -------------------------------------

            string[] randomImgPaths = {"/Users/yaseen/Documents/GitHub/face_matching/res/SampleData/PersonGroup/Family1-Dad/Family1-Dad1.jpg",
                                        "/Users/yaseen/Documents/GitHub/face_matching/res/SampleData/PersonGroup/Family1-Dad/Family1-Dad2.jpg",
                                        "/Users/yaseen/Documents/GitHub/face_matching/res/SampleData/PersonGroup/Family1-Dad/Family1-Dad3.jpg",
                "/Users/yaseen/Documents/GitHub/face_matching/res/SampleData/PersonGroup/Family1-Mom/Family1-Mom1.jpg",
                "/Users/yaseen/Documents/GitHub/face_matching/res/SampleData/PersonGroup/Family1-Mom/Family1-Mom2.jpg"};
            string[] randomNames = {"Alpha", "Beta", "Chi", "Delta", "Epsilon"};

            //create a new item, name it, and set the parent
            GameObject newItem = Instantiate(itemPrefab) as GameObject;
            newItem.name = gameObject.name + " item at (" + i + "," + j + ")";

            System.Random random = new System.Random();
            ProfileHandler handler = newItem.GetComponent<ProfileHandler>();
            handler.newImagePath = randomImgPaths[random.Next(randomImgPaths.Length)];
            handler.newText = randomNames[random.Next(randomNames.Length)];
            handler.UpdateImage();
            handler.UpdateText();

            newItem.transform.parent = gameObject.transform;

            // ------------------------------------- ^^ RELEVANT DYNAMIC CODE HERE ^^ -------------------------------------

            //move and size the new item
            RectTransform rectTransform = newItem.GetComponent<RectTransform>();

            float x = -containerRectTransform.rect.width / 2 + width * (i % columnCount);
            float y = containerRectTransform.rect.height / 2 - height * j;
            rectTransform.offsetMin = new Vector2(x, y);

            x = rectTransform.offsetMin.x + width;
            y = rectTransform.offsetMin.y + height;
            rectTransform.offsetMax = new Vector2(x, y);
        }
    }

}