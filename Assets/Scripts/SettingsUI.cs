using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    public RoomManager roomManager;
    
    
    public Button newGameButton;
    public Button LoadGameButton;
    public Button ExitButton;


    private void Awake()
    {
        newGameButton.onClick.AddListener(NewGame);
        ExitButton.onClick.AddListener(CloseView);
    }

    [System.Serializable]
    private class Flag
    {
        public string Key;
        public string Value;
    }

    [System.Serializable]
    private class Equipment
    {
        public string Key;
        public string Value;
    }

    
    [System.Serializable]
    private class GameParameters
    {
        public string currentRoom;
        public List<Flag> flags;
        public List<Equipment> equipments;
        public List<string> inventory;
        
    }
    
    
    public void NewGame()
    {
        // Load the parameters from the JSON file in the Resources folder
        TextAsset jsonFile = Resources.Load<TextAsset>("NewGameParameters");
        GameParameters parameters = JsonUtility.FromJson<GameParameters>(jsonFile.text);

        // Wipe current progress
        PlayerPrefs.SetString("QuestLog", "");
        PlayerPrefs.Save();

        if (RoomManager.Diary.questLog != null)
        {
            RoomManager.Diary.questLog.Entries.Clear();
        }

        PlayerData tempData = new PlayerData
        {
            currentRoom = parameters.currentRoom
        };

        // Set flags from JSON
        foreach (var flag in parameters.flags)
        {
            tempData.SetFlag(flag.Key, flag.Value);
        }

        // Set flags from JSON
        foreach (var equipment in parameters.equipments)
        {
            tempData.SetEquipment(equipment.Key, equipment.Value);
        }
        
        // Add inventory items from JSON
        foreach (var itemName in parameters.inventory)
        {
            tempData.Inventory.Add(ItemRegistry.GetItem(itemName));
        }

        
        SaveGameManager.SaveGame(tempData);
        SaveGameManager.LoadGame();
        roomManager.LoadRoomFromJson(RoomManager.playerData.currentRoom);
    }
    
    void LoadGame()
    {
        if (SaveGameManager.SaveFileExists())
        {
            SaveGameManager.LoadGame();
        }
    }

    void CloseView()
    {
        this.gameObject.SetActive(false);
    }
    
}
