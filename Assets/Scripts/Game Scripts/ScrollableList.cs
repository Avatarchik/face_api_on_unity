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

    private Dictionary<ProfileHandler.IScrollable, GameObject> listedItems;

    public void SetItemActives(bool active)
    {
        if (listedItems != null)
        {
            foreach (KeyValuePair<ProfileHandler.IScrollable, GameObject> entry in listedItems)
            {
                entry.Value.gameObject.SetActive(active);
            }
        }
    }

    public void DisplayProfiles(List<GameController.Profile> profiles, int columns = 0)
    {
        List<ProfileHandler.IScrollable> scrollables = new List<ProfileHandler.IScrollable>();
        foreach (GameController.Profile prof in profiles)
        {
            scrollables.Add(prof);
        }
        DisplayObjects(ProfileHandler.ScrollableType.Profile, scrollables, columns);
    }

    public void DisplayImages(List<GameController.ProfileImage> images, int columns = 0)
    {
        List<ProfileHandler.IScrollable> scrollables = new List<ProfileHandler.IScrollable>();
        foreach (GameController.ProfileImage img in images)
        {
            scrollables.Add(img);
        }
        DisplayObjects(ProfileHandler.ScrollableType.ProfileImage, scrollables, columns);
    }

    private void ClearOldList()
    {
        foreach (GameObject obj in listedItems.Values)
        {
            Destroy(obj);
        }
        listedItems = new Dictionary<ProfileHandler.IScrollable, GameObject>();
    }

    private void DisplayObjects(ProfileHandler.ScrollableType scrollableType, List<ProfileHandler.IScrollable> scrollables, int columns = 0)
    {
        if (listedItems != null)
            ClearOldList();
        else
            listedItems = new Dictionary<ProfileHandler.IScrollable, GameObject>();

        itemCount = scrollables.Count;
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

        int j = 0;
        for (int i = 0; i < itemCount; i++)
        {
            //this is used instead of a double for loop because itemCount may not fit perfectly into the rows/columns
            if (i % columnCount == 0)
                j++;

            // ------------------------------------- VV RELEVANT DYNAMIC CODE HERE VV -------------------------------------

            ProfileHandler.IScrollable scrollableItem = scrollables[i];

            //create a new item, name it, and set the parent
            GameObject profileObj = Instantiate(itemPrefab) as GameObject;
            //newItem.name = gameObject.name + " item at (" + i + "," + j + ")";
            profileObj.name = scrollableType.ToString() + ": " + scrollableItem.IdentifyingName;

            ProfileHandler handler = profileObj.GetComponent<ProfileHandler>();

            handler.item = scrollableItem;
            handler.type = scrollableType;
            handler.Init();

            profileObj.transform.SetParent(gameObject.transform);
            listedItems.Add(scrollableItem, profileObj);

            // ------------------------------------- ^^ RELEVANT DYNAMIC CODE HERE ^^ -------------------------------------

            //move and size the new item
            RectTransform rectTransform = profileObj.GetComponent<RectTransform>();

            float x = -containerRectTransform.rect.width / 2 + width * (i % columnCount);
            float y = containerRectTransform.rect.height / 2 - height * j;
            rectTransform.offsetMin = new Vector2(x, y);

            x = rectTransform.offsetMin.x + width;
            y = rectTransform.offsetMin.y + height;
            rectTransform.offsetMax = new Vector2(x, y);

            containerRectTransform.pivot = new Vector2(containerRectTransform.pivot.x, 1);
        }
    }

}