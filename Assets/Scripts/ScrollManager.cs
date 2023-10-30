using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScrollManager : MonoBehaviour
{
    public TMP_Text textfield;
    public ScrollRect scrollRect;

    private void Start()
    {
        Debug.Log("starting forcescrolll");
    }

    
    public void ScrollToBottom()
    {
        StartCoroutine("ForceScrollDown");
    }
    
    
    // Called at the end of instantiation function.
    IEnumerator ForceScrollDown () {
        // Wait for end of frame AND force update all canvases before setting to bottom.
        yield return new WaitForEndOfFrame ();
        Canvas.ForceUpdateCanvases ();
        //Debug.Log(scrollRect.normalizedPosition);
        
        scrollRect.normalizedPosition = new Vector2(0, 0);
        Canvas.ForceUpdateCanvases ();
    }


    // Update is called once per frame
    /*
    public void UpdateCScrollBar()
    {
        if (scrollRect.verticalScrollbar.enabled)
            ScrollToBottom();
    }
    */
}
