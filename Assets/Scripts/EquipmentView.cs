using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class EquipmentView : MonoBehaviour
{
    public TMP_Text name;
    public Image image;
    
    public Image mainHand_image;
    public Image secondaryHand_image;
    
    
    public TMP_Text description;
    
    public GameObject actionButtonPrefab;
    public Transform actionButtonContainer;
    public RoomManager roomManager;
    
    public Button ExitButton;

    
    private void Awake()
    {
        ExitButton.onClick.AddListener(CloseView);
    }
    public void CreateInventory()
    {
        foreach (var item in RoomManager.playerData.Inventory)
        {
            CreateInventoryButton(item, () =>
            {
                Debug.Log("Button Clicked");
                //description.text = item.description;
                //name.text = item.shortDescription;
                //string path = "InventoryItems/" + item.image;
                //image.sprite = Resources.Load<Sprite>(path);
                Equip(item);
                
            });
        }
     
        UpdatePaperDollImages();
    }

    void UpdatePaperDollImages()
    {
        foreach (var entry in RoomManager.playerData.Equipments)
        {
            Debug.Log(entry.Key +  " ----------------> " +entry.Value);

            mainHand_image.sprite = Resources.Load<Sprite>("InventoryItems/empty");
            secondaryHand_image.sprite = Resources.Load<Sprite>("InventoryItems/empty");
            
            if (entry.Key == "MainHand")
            {
                string path = "InventoryItems/" + ItemRegistry.GetItem(entry.Value).image;
                mainHand_image.sprite = Resources.Load<Sprite>(path);
            }
        
            if (entry.Key == "SecondaryHand")
            {
                string path = "InventoryItems/" + ItemRegistry.GetItem(entry.Value).image;
                secondaryHand_image.sprite = Resources.Load<Sprite>(path);
            }
        }
    }
    
    
    
    void Equip(Item item)
    {
        Debug.Log("equipping " + item.shortDescription  + " to slot " + item.equipSlot.ToString());

        // add equipment currently in use to inventory
        if (RoomManager.playerData.Equipments.ContainsKey(item.equipSlot.ToString()) &&
                                                          RoomManager.playerData.Equipments
                                                              [item.equipSlot.ToString()] != "none")
        {
            RoomManager.playerData.Inventory.Add(ItemRegistry.GetItem(RoomManager.playerData.Equipments
                [item.equipSlot.ToString()]));
        }
            
        RoomManager.playerData.Equipments[item.equipSlot.ToString()] = item.shortDescription;
        RoomManager.playerData.Inventory.Remove(item);
        
        ResetInventory();
        CreateInventory();
        
        Debug.Log("equipped " + RoomManager.playerData.Equipments[item.equipSlot.ToString()]);
        
        UpdatePaperDollImages();
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
        //image.sprite = Resources.Load<Sprite>(path);
        
        buttonComponent.GetComponent<Image>().sprite = Resources.Load<Sprite>(path);
    }
    
    void CloseView()
    {
        this.gameObject.SetActive(false);
    }
}
