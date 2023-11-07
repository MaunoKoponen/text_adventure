using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ShopView : MonoBehaviour
{
    public TMP_Text name;
    public Image image;
    public TMP_Text description;

    public TMP_Text price;
    public TMP_Text coins; // players balance;

    
    public GameObject actionButtonPrefab;
    public Transform actionButtonContainer;
    
    public Transform ShopButtonContainer;

    public List<Item> shopInventory  = new List<Item>();
    
    public RoomManager roomManager;
    //private PlayerData playerData;
    public Button ExitButton;
    public Button BuyButton;

    private Item selectedItem;
    private bool selectedIsOwnItem;
    
    private void Awake()
    {
        ExitButton.onClick.AddListener(CloseView);
        BuyButton.onClick.AddListener(BuyItem);

    }
    
    
    public void SetupShop()
    {
        shopInventory.Add(Item.ScrollOfFireball);
        shopInventory.Add(Item.PotionOfHealing);

        ResetInventory();
        
        CreateInventory();
        CreateShopInventory();

        this.transform.gameObject.SetActive(true);

        selectedItem = null;
    }
    

    public void CreateInventory()
    {
        foreach (var item in RoomManager.playerData.Inventory)
        {
            CreateInventoryButton(item,actionButtonContainer, () =>
            {
                selectedItem = item;
                selectedIsOwnItem = true;
                description.text = item.description;
                name.text = item.shortDescription;
                string path = "InventoryItems/" + item.image;
                image.sprite = Resources.Load<Sprite>(path);

                coins.text = RoomManager.playerData.coins.ToString();
                
                SetBuyButton();

            });
        }

        coins.text = RoomManager.playerData.coins.ToString();
    }
    
    public void CreateShopInventory()
    {
        foreach (var item in shopInventory)
        {
            CreateInventoryButton(item, ShopButtonContainer,() =>
            {
                selectedItem = item;
                selectedIsOwnItem = false;
                
                description.text = item.description;
                name.text = item.shortDescription;
                string path = "InventoryItems/" + item.image;
                image.sprite = Resources.Load<Sprite>(path);
                price.text = "Price: " + item.buyPrice.ToString();
                
                SetBuyButton();
            });
        }
    }


    private void SetBuyButton()
    {
        if (selectedIsOwnItem)
        {
            BuyButton.gameObject.SetActive(false);
        }
        else
        {
            BuyButton.gameObject.SetActive(true);

            Debug.Log("PlayerData coins " + RoomManager.playerData.coins);
            
            if (selectedItem.buyPrice <= RoomManager.playerData.coins)
            {
                Debug.Log("Enable");
                BuyButton.interactable = true;
            }
            else
            {
                
                Debug.Log("Disable");
                BuyButton.interactable = false;
            }
                
        }
    }
    
    void BuyItem()
    {
        //selectedItem.
        RoomManager.playerData.AddItem(selectedItem);
        RoomManager.playerData.coins -= selectedItem.buyPrice;
        //selectedItem = null;
        //selectedIsOwnItem = false; // ??
        //image.sprite = null;
        
        ResetInventory();
        CreateInventory();
        SetBuyButton(); // to refresh the "has enough money" status
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


    void CreateInventoryButton(Item item, Transform container, UnityAction callback )
    {

        GameObject buttonObj = Instantiate(actionButtonPrefab, container);
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
    
    void CloseView()
    {
        this.gameObject.SetActive(false);
    }
    
    
}