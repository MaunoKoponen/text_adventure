using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
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
    
    public Transform shopButtonContainer;

    public List<Item> shopInventory = new List<Item>();

    public RoomManager roomManager;
    public Button ExitButton;
    public Button BuyButton;
    public Button SellButton;  // New: Sell button

    private Item selectedItem;
    private string selectedItemId;  // Track itemId for inventory operations
    private bool selectedIsOwnItem;

    private List<GameObject> shopItemGOs = new List<GameObject>();
    private void Awake()
    {
        ExitButton.onClick.AddListener(CloseView);
        BuyButton.onClick.AddListener(BuyItem);
        if (SellButton != null)
            SellButton.onClick.AddListener(SellItem);
    }
    
    
    public void SetupShop(List<string> items)
    {
        ResetInventory();

        // these should come from room json or somewhere else        
        //shopInventory.Add(Item.ScrollOfFireball);
        //shopInventory.Add(Item.PotionOfHealing);

        foreach (var itemString in items)
        {
            Item item = ItemRegistry.GetItem(itemString);
            shopInventory.Add(item);
        }
        
        CreateInventory();
        CreateShopInventory();

        this.transform.gameObject.SetActive(true);

        selectedItem = null;
    }
    

    public void CreateInventory()
    {
        foreach (var slot in RoomManager.playerData.Inventory)
        {
            Item item = slot.GetItem();
            if (item == null) continue;

            // Capture for closure
            var currentSlot = slot;
            var currentItem = item;

            CreateInventoryButton(currentItem, currentSlot.quantity, actionButtonContainer, () =>
            {
                selectedItem = currentItem;
                selectedItemId = currentSlot.itemId;
                selectedIsOwnItem = true;

                description.text = currentItem.description;
                name.text = currentSlot.quantity > 1
                    ? $"{currentItem.shortDescription} x{currentSlot.quantity}"
                    : currentItem.shortDescription;
                string path = "InventoryItems/" + currentItem.image;
                image.sprite = Resources.Load<Sprite>(path);
                price.text = $"Sell: {currentItem.sellPrice}";

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
            if (item == null) continue;

            // Capture for closure
            var currentItem = item;

            GameObject go = CreateInventoryButton(currentItem, 1, shopButtonContainer, () =>
            {
                selectedItem = currentItem;
                selectedItemId = currentItem.itemId;
                selectedIsOwnItem = false;

                description.text = currentItem.description;
                name.text = currentItem.shortDescription;
                string path = "InventoryItems/" + currentItem.image;
                image.sprite = Resources.Load<Sprite>(path);
                price.text = $"Buy: {currentItem.buyPrice}";

                SetBuyButton();
            });

            shopItemGOs.Add(go);
        }
    }


    private void SetBuyButton()
    {
        if (selectedItem == null)
        {
            BuyButton.gameObject.SetActive(false);
            if (SellButton != null) SellButton.gameObject.SetActive(false);
            return;
        }

        if (selectedIsOwnItem)
        {
            // Player's item selected - show Sell, hide Buy
            BuyButton.gameObject.SetActive(false);

            if (SellButton != null)
            {
                SellButton.gameObject.SetActive(true);
                SellButton.interactable = true;
            }
        }
        else
        {
            // Shop item selected - show Buy, hide Sell
            if (SellButton != null) SellButton.gameObject.SetActive(false);
            BuyButton.gameObject.SetActive(true);

            Debug.Log("PlayerData coins " + RoomManager.playerData.coins);

            if (selectedItem.buyPrice <= RoomManager.playerData.coins)
            {
                Debug.Log("Enable Buy");
                BuyButton.interactable = true;
            }
            else
            {
                Debug.Log("Disable Buy - insufficient funds");
                BuyButton.interactable = false;
            }
        }
    }

    void BuyItem()
    {
        if (selectedItem == null || string.IsNullOrEmpty(selectedItemId)) return;

        RoomManager.playerData.AddItem(selectedItemId);
        RoomManager.playerData.coins -= selectedItem.buyPrice;

        RefreshUI();
    }

    void SellItem()
    {
        if (selectedItem == null || string.IsNullOrEmpty(selectedItemId)) return;

        RoomManager.playerData.coins += selectedItem.sellPrice;
        RoomManager.playerData.RemoveItem(selectedItemId);

        // Clear selection since item is gone
        selectedItem = null;
        selectedItemId = null;

        RefreshUI();
    }

    void RefreshUI()
    {
        ResetInventory();
        CreateInventory();
        coins.text = RoomManager.playerData.coins.ToString();
        SetBuyButton();
    }

    void UpdateInventory()
    {
        RefreshUI();
    }

    public void ResetInventory()
    {
        foreach (Transform child in actionButtonContainer)
        {
            Destroy(child.gameObject);
        }
    }


    GameObject CreateInventoryButton(Item item, int quantity, Transform container, UnityAction callback)
    {
        GameObject buttonObj = Instantiate(actionButtonPrefab, container);
        Button buttonComponent = buttonObj.GetComponent<Button>();
        buttonComponent.onClick.AddListener(callback);
        TMP_Text buttonText = buttonObj.GetComponentInChildren<TMP_Text>();
        if (buttonText)
        {
            // Show quantity for stacked items
            buttonText.text = quantity > 1
                ? $"{item.shortDescription} x{quantity}"
                : item.shortDescription;
        }

        Debug.Log($"Item: {item.shortDescription} x{quantity}, image: {item.image}");
        string path = "InventoryItems/" + item.image;

        buttonComponent.GetComponent<Image>().sprite = Resources.Load<Sprite>(path);

        return buttonObj;
    }
    
    void CloseView()
    {
        SaveGameManager.SaveGame(RoomManager.playerData);
        
        ResetInventory();
        
        // shopItemGOs
        foreach (var go  in shopItemGOs)
        {
            Destroy(go);
        }
        shopItemGOs.Clear();
        
        shopInventory.Clear();

        image.sprite = null;
        name.text = "";
        gameObject.SetActive(false);
    }
    
    
}