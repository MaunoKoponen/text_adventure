using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapToggle : MonoBehaviour
{
    public Button button;
    public GameObject go;
    
    
    void Start()
    {
        button.onClick.AddListener(ToggleState);
        
    }

    void ToggleState()
    {
        go.gameObject.SetActive(! go.activeSelf);
    }    
   
}
