using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Diary : MonoBehaviour
{
    public Button ExitButton;
    
    private void Awake()
    {
        ExitButton.onClick.AddListener(CloseView);
    }
    
    void CloseView()
    {
        this.gameObject.SetActive(false);
    }
}
