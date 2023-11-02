using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainUIButtons : MonoBehaviour
{
    public Button InventoryButton;
    public Button DiaryButton;
    public Button SettingsButton;
    public Button MapButton;

    public GameObject InventoryGO;
    public GameObject DiaryGO;
    public GameObject SettingsGO;
    public GameObject MapGO;

    public InventoryView inventoryView;
    
    void Start()
    {
        List<GameObject> GOs = new List<GameObject>();
        GOs.Add(InventoryGO);
        GOs.Add(DiaryGO);
        //GOs.Add(SettingsGO);
        GOs.Add(MapGO);
        
        InventoryButton.onClick.AddListener(() =>
        {
            if (InventoryGO.activeSelf)
            {
               
                InventoryGO.SetActive(false);
                return;
            }
            
            foreach (var go in GOs)
            {
                go.SetActive(false);
            }
            InventoryGO.SetActive(! InventoryGO.activeSelf);


            if (InventoryGO.activeSelf)
            {
                inventoryView.ResetInventory();
                inventoryView.CreateInventory();
            }
           
            
            
        });
        
        DiaryButton.onClick.AddListener(() =>
        {
            if (DiaryGO.activeSelf)
            {
                DiaryGO.SetActive(false);
                return;
            }
            
            foreach (var go in GOs)
            {
                go.SetActive(false);
            }
            DiaryGO.SetActive(! DiaryGO.activeSelf);
        });
        
        SettingsButton.onClick.AddListener(() =>
        {
            if (SettingsGO.activeSelf)
            {
                SettingsGO.SetActive(false);
                return;
            }
            
            foreach (var go in GOs)
            {
                go.SetActive(false);
            }
            SettingsGO.SetActive(! SettingsGO.activeSelf);
        });
        
        MapButton.onClick.AddListener(() =>
        {
            if (MapGO.activeSelf)
            {
                MapGO.SetActive(false);
                return;
            }
            
            foreach (var go in GOs)
            {
                go.SetActive(false);
            }
            MapGO.SetActive(! MapGO.activeSelf);
        });
    }

        
   
}
