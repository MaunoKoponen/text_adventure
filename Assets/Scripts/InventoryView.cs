using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class InventoryView : MonoBehaviour
{
    public TMP_Text name;
    public Image image;
    public TMP_Text description;
    
    public GameObject actionButtonPrefab;
    public Transform actionButtonContainer;
    public RoomManager roomManager;
    //private PlayerData playerData;
    

    public void CreateInventory()
    {
        foreach (var item in RoomManager.playerData.Inventory)
        {
            CreateInventoryButton(item, () =>
            {
                Debug.Log("Button Clicked");
                description.text = item.description;
                name.text = item.shortDescription;
                string path = "InventoryItems/" + item.image;
                image.sprite = Resources.Load<Sprite>(path);
                
            });
        }
    }
    void UpdateInventory()
    {
        // todo
    }

    public void ResetInventory()
    {
        foreach (Transform child in actionButtonContainer)
        {
            Destroy(child.gameObject);
        }
    }


    void CreateInventoryButton(Item item, UnityAction callback)
    {

        GameObject buttonObj = Instantiate(actionButtonPrefab, actionButtonContainer);
        Button buttonComponent = buttonObj.GetComponent<Button>();
        buttonComponent.onClick.AddListener(callback);
        TMP_Text buttonText = buttonObj.GetComponentInChildren<TMP_Text>();
        if (buttonText)
        {
            buttonText.text = item.shortDescription;
        }

        string path = "InventoryItems/" + item.image;
        image.sprite = Resources.Load<Sprite>(path);
        
        buttonComponent.GetComponent<Image>().sprite = Resources.Load<Sprite>(path);
    }
}
