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


    void NewGame()
    {
       // wipe current progress

       PlayerData tempData = new PlayerData();
       
       tempData.currentRoom = "town_square";
       tempData.SetFlag("HasSoulStone",false);
       
       tempData.SetFlag("quest_cartographer_01",false);
       tempData.SetFlag("Dead",false);
       tempData.SetFlag("gate_key",false);
       
       tempData.Inventory.Add(Item.ScrollOfFire);
/*       tempData.Inventory.Add(Item.PotionOfHealing);
       tempData.Inventory.Add(Item.SoulStone);
       tempData.Inventory.Add(Item.Antidote);
       tempData.Inventory.Add(Item.ElixirOfStrength);
       tempData.Inventory.Add(Item.StoneOfEvasion);
       tempData.Inventory.Add(Item.ScrollOfFireball);
  */     
       
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
