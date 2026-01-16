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
    
    public Button ExitButton;

    
    private void Awake()
    {
        ExitButton.onClick.AddListener(CloseView);
    }
    public void CreateInventory()
    {
        foreach (var slot in RoomManager.playerData.Inventory)
        {
            Item item = slot.GetItem();
            if (item == null) continue;

            // Capture slot for closure
            var currentSlot = slot;
            var currentItem = item;

            CreateInventoryButton(currentItem, currentSlot.quantity, () =>
            {
                Debug.Log($"Button Clicked: {currentItem.shortDescription} x{currentSlot.quantity}");
                description.text = currentItem.description;
                name.text = currentSlot.quantity > 1
                    ? $"{currentItem.shortDescription} x{currentSlot.quantity}"
                    : currentItem.shortDescription;
                string path = "InventoryItems/" + currentItem.image;
                image.sprite = Resources.Load<Sprite>(path);
            });
        }
    }

    /// <summary>
    /// Refresh the inventory display.
    /// </summary>
    public void UpdateInventory()
    {
        ResetInventory();
        CreateInventory();
    }

    public void ResetInventory()
    {
        foreach (Transform child in actionButtonContainer)
        {
            Destroy(child.gameObject);
        }
    }

    void CreateInventoryButton(Item item, int quantity, UnityAction callback)
    {
        GameObject buttonObj = Instantiate(actionButtonPrefab, actionButtonContainer);
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

        string path = "InventoryItems/" + item.image;
        buttonComponent.GetComponent<Image>().sprite = Resources.Load<Sprite>(path);
    }
    
    void CloseView()
    {
        this.gameObject.SetActive(false);
    }
}
