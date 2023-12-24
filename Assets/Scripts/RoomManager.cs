using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;



public class RoomManager : MonoBehaviour
{
    public ScrollManager scrollManager;
    public ImageFade imageFadeInstance;
    public TMP_Text roomDescriptionText;
    public TMP_Text inventoryText;
    public TMP_Text flagsText;
    public TMP_Text healthText;

    
    public GameObject actionButtonPrefab;
    public GameObject combatInventoryPrefab;

    
    public Transform actionButtonContainer;
    public Transform itemButtonContainer;

    public Narrator narrator;
    
    public Room currentRoom;
    public string previousRoom;
    public string respawnRoom;


    public static  PlayerData playerData = new PlayerData();
    private int currentDialogueStep = -1;
    private string currentNPC = "";

    
    public List<string> combatLog = new List<string>();

    public Diary diary;
    public static Diary Diary;

    public ShopView shopView;
    public SettingsUI settingsUI;

    public Combat Combat
    {
        get { return _combat; }
    }

    public void SetHealth(int value)
    {
        playerData.health = value;
        healthText.text = value.ToString();
    }

    private void Awake()
    {
        Diary = diary;
    }

    void Start()
    {
        
        
        // Example initialization
        SetHealth(500);
        /*
        player.equippedWeapon = new Weapon 
        {
            name = "Sword",
            damageAmount = 10,
            damageType = Weapon.DamageType.Physical
        };
        */
        
        
        
        //-------------- Init values ------------//

        if (SaveGameManager.SaveFileExists())
        {
            SaveGameManager.LoadGame();
        }
        else
        {
            settingsUI.NewGame();
        }
        
        respawnRoom = "temple_of_lost_souls_resurrect";
        
        // for overriding saved location, when testing things
        //playerData.currentRoom = "town_square";
        
        LoadRoomFromJson( playerData.currentRoom);

        diary.LoadQuestLog();
        Diary = diary;
    }

    
    
    
    public void LoadRoomFromJson(string roomId, string extraString = "")
    {
        playerData.currentRoom = roomId;
        SaveGameManager.SaveGame(playerData);
        
        Debug.Log(">>>>> LoadRoomFromJSON: " + roomId);
        
        TextAsset roomData = Resources.Load<TextAsset>("Rooms/" + roomId);

        if (roomData != null)
        {
            string jsonData = roomData.text;
            Debug.Log("JSON string: " + jsonData);

            if (currentRoom != null)
            {
                previousRoom = currentRoom.room_id;
                
            }
            
            currentRoom = JsonUtility.FromJson<Room>(jsonData);
            
            DisplayRoomInfo(extraString);
        }
        else
        {
            Debug.LogError("Room not found!");
        }
    }

    private void ProcessRoomEvents()
    {
        if (currentRoom.events == null) return;

        foreach (Room.RoomEvent roomEvent in currentRoom.events)
        {
            switch (roomEvent.event_type)
            {
                case "add_item":
                    playerData.AddItem(roomEvent.item_id);
                    break;
                case "set_flag":
                    if (playerData.Flags.ContainsKey(roomEvent.flag_name))
                    {
                        playerData.Flags[roomEvent.flag_name] = roomEvent.value;
                    }
                    else
                    {
                        playerData.Flags.Add(roomEvent.flag_name, roomEvent.value);
                    }
                    
                    break;
                case "pick_item":
                    playerData.AddItem(roomEvent.item_id);
                    break;
            }
        }
    }


    private string currentImage = "";
    private readonly Combat _combat;

    public RoomManager()
    {
        _combat = new Combat(this);
    }

    public void DisplayRoomInfo(string extraString)
    {
        Debug.Log(">>>>> DisplayRoomInfo");
        
        roomDescriptionText.text = extraString + currentRoom.description + "\n\n" + string.Join("\n", combatLog);

        string imageName = currentRoom.room_id;
        if (imageName != currentImage)
        {
            var newSprite = GetSpriteByName(imageName);
            imageFadeInstance.SetImageWithFade(newSprite);
            currentImage = imageName;
        }
        
        narrator.PlayNarration(currentRoom.room_id);
        
        Debug.Log(currentRoom.description);
        
        // Clear existing buttons
        foreach (Transform child in actionButtonContainer)
        {
            Destroy(child.gameObject);
        }

        /*
        if (currentRoom.combat != null && player.health < 0)
        {
            Debug.Log(">>>>> DisplayRoomInfo -> zero -> load new room");
            
            Debug.Log($"{currentRoom.combat.enemy_name} has defeated you!");
            ClearCombatLog();
            currentRoom.combat = null;
            player.health = 0;
            LoadRoomFromJson(respawnRoom, "You wake up from odd dream.\n");
            return;
        }
*/

        if (currentRoom.combat != null && currentRoom.combat.enemy_health > 0)
        {
            // Display combat actions
            foreach (string action in currentRoom.combat.combat_actions)
            {
                Debug.Log("Combat action: "  + action);
                CreateActionButton(action, () => Combat.HandleCombatAction(action));
            }
        }
        else
        {
            if (currentRoom.actions != null)
            {
                foreach (var action in currentRoom.actions)
                {
                    if (action.flag_true != null && playerData.Flags.ContainsKey(action.flag_true) &&
                        playerData.GetFlag(action.flag_true) == "true")
                    {
                        Debug.Log("Had true flag");
                        CreateActionButton(action.action_description, () => HandleRoomAction(action.action_id));
                    }
                
                    if (action.flag_false != null && playerData.Flags.ContainsKey(action.flag_false) &&
                        playerData.GetFlag(action.flag_false) == "false")
                    {
                        Debug.Log("Had false flag");
                        CreateActionButton(action.action_description, () => HandleRoomAction(action.action_id));
                    }

                    if (action.flag_true == null && action.flag_false == null)
                    {
                   
                        Debug.Log("Had no flag");

                        CreateActionButton(action.action_description, () => HandleRoomAction(action.action_id));
                    }

                }    
            }
            
            foreach (Room.Exit exit in currentRoom.exits)
            {
                Debug.Log("exit action: ..." + exit.exit_name);
                bool any = false;    

                // Note, currently if one of any is ok, then player can pass
                
                
                foreach (var condition in exit.conditions)
                {
                    foreach (var item in playerData.Inventory)
                    {
                        if (condition == item.shortDescription)
                            any = true;
                    }

                    foreach (var flag in playerData.Flags)
                    {
                        Debug.Log("checking condition: " + flag.Key + " and its ... " + flag.Value);
                        
                        if (condition == flag.Key && flag.Value == "true")
                        {
                            any = true;
                        }
                    }
                }
                
                foreach (var condition in exit.conditions_not)
                {

                    foreach (var flag in playerData.Flags)
                    {
                        Debug.Log("checking negative condition: " + flag.Key + " and its ... " + flag.Value);

                        if (condition == flag.Key && flag.Value == "false")
                        {
                            Debug.Log("found a flag that is supposed to be false and it is");
                            any = true;
                        }
                    }
                }

                if (exit.conditions.Length == 0 && exit.conditions_not.Length == 0)
                {
                    Debug.Log("there wasn't any condition at all, so you may pass");
                    any = true;
                }
                
                if(any)
                    CreateActionButton(exit.exit_name, () => LoadRoomFromJson(exit.leads_to));
            }
        }

        /*
        string inventoryString = "";
        foreach (var item in playerData.Inventory)
        {
            Debug.Log("add to text: " + item.shortDescription);
            
            inventoryString += " - " + item.shortDescription + "\n";
        }
        inventoryText.text = inventoryString;
        */
        
        string flagsString = "";
        foreach (var item in playerData.Flags)
        {
            flagsString += " - " + item.Key + " " + item.Value + "\n";
        }
        flagsText.text = flagsString;
        
        scrollManager.ScrollToBottom();
        
    }
    
    private void UseItemInCombat(Item item)
    {
        Debug.Log("TODO Use item: " + item.shortDescription);
        
        // check if usage possible (for example if target has immunity)
        
        // if not, show fizzle text
        
        // if possible,
        //calculate effect on target stats
        // create text
        
        combatLog.Add("\n" + "----> Item usage");
        //DisplayRoomInfo(item.usageSuccess);
    }
    
    private void HandleRoomAction(string actionName)
    {
        
        Room.Exit exit = currentRoom.exits.FirstOrDefault(e => e.exit_name == actionName);
        if (exit != null)
        {
            narrator.FadeOut(0.2f); // Fades out the audio over 1 second return;
            LoadRoomFromJson(exit.leads_to);
        }

        // If the action isn't an exit, it might be an NPC interaction
        Room.NPCDialogue dialogue = currentRoom.dialogues.FirstOrDefault(d => d.npc_name == actionName);
        if (dialogue != null)
        {
            StartDialogue(dialogue);
        }

        // Further custom actions (like picking up items, getting aa quest, getting item from NPC ) can be added here
    }
    
    
    private void StartDialogue( Room.NPCDialogue dialogue)
    {
        currentNPC = dialogue.npc_name;
        currentDialogueStep = 0;
        DisplayDialogue();
        
        var newSprite = GetSpriteByName(dialogue.dialogue_image);
        imageFadeInstance.SetImageWithFade(newSprite);
        currentImage = dialogue.dialogue_image;
    }

    
    string ParseMessage(string rawMessage)
    {
        int delimiterIndex = rawMessage.IndexOf('#');
        if (delimiterIndex > 0)
        {
            // Extract and return the part after the delimiter
            return rawMessage.Substring(delimiterIndex + 2);  // +2 to skip the '# ' after the label
        }
        return rawMessage;  // If no delimiter found, return the original
    }
    
    private void DisplayDialogue()
    {
        Room.NPCDialogue dialogue = currentRoom.dialogues.FirstOrDefault(d => d.npc_name == currentNPC);

        if (dialogue == null) return;
        
        roomDescriptionText.text = ParseMessage(dialogue.dialogues[currentDialogueStep].message);
        
        foreach (Transform child in actionButtonContainer)
        {
            Destroy(child.gameObject);
        }

        Debug.Log(">>>>> dialogue.dialogues[currentDialogueStep] + dialogue.dialogues[currentDialogueStep]");
        Debug.Log(">>>>> dialogue.dialogues.Length " + dialogue.dialogues.Length);
        Debug.Log(">>>>> dialogue.dialogues[currentDialogueStep].responses " +dialogue.dialogues[currentDialogueStep].responses.Length);

        foreach (var response in dialogue.dialogues[currentDialogueStep].responses)
        {
            int nextStep = response.next_step;
            string setFlagTrue = null;
            string setFlagFalse = null;
            string setFlagConcluded = null;

            string getItem = null;
            string giveItem = null;
            
            
            if(response.setFlagTrue !=null)
                setFlagTrue = response.setFlagTrue;
            if(response.setFlagFalse !=null)
                setFlagFalse = response.setFlagFalse;
            if(response.setFlagConcluded !=null)
                setFlagConcluded = response.setFlagConcluded;
            if(response.getItem !=null)
                getItem = response.getItem;
            if (response.giveItem != null)
                giveItem = response.giveItem;
        

            CreateActionButton(response.text, () => HandleDialogueResponse(nextStep,setFlagTrue,setFlagFalse,setFlagConcluded,getItem,giveItem));
        }
    }


    private void HandleDialogueResponse(int nextStep, string FlagToSetTrue = null, string FlagToSetFalse = null, string FlagToSetConcluded = null, string getItem=null, string giveItem = null)
    {
        if (FlagToSetTrue != null)
        {
            // Quest?
            playerData.SetFlag(FlagToSetTrue,"true");
            if (FlagToSetTrue.Contains("quest"))
            {
                SaveGameManager.SaveGame(playerData);
                Debug.Log("Quest start detected!");
                diary.OnQuestReceived(FlagToSetTrue);
                Diary = diary;
                
            }
        }
           
       if(FlagToSetFalse !=null)
            playerData.SetFlag(FlagToSetFalse,"false");
       
       if (FlagToSetConcluded != null)
       {
           // Quest?
           playerData.SetFlag(FlagToSetConcluded,"concluded");
           if (FlagToSetConcluded.Contains("quest"))
           {
               SaveGameManager.SaveGame(playerData);
               Debug.Log("Quest finished detected!");
               diary.OnQuestConcluded(FlagToSetConcluded);
               Diary = diary;
                
           }
       }

       
       
       if(getItem !=null)
            playerData.AddItem(getItem);
       if(giveItem !=null)
            playerData.RemoveItem(giveItem);
        
        
       if (nextStep == 1000)
       {
           shopView.SetupShop(currentRoom.shop_inventory);
           EndDialogue();
       }else if (nextStep == -1)
       {
            EndDialogue();
       }
       else
       {
            currentDialogueStep = nextStep;
            DisplayDialogue();
       }
    }

    private void EndDialogue()
    {
        currentNPC = "";
        currentDialogueStep = -1;
        DisplayRoomInfo("");
    }
    

    private void CreateActionButton(string actionName, UnityAction callback)
    {
        GameObject buttonObj = Instantiate(actionButtonPrefab, actionButtonContainer);
        Button buttonComponent = buttonObj.GetComponent<Button>();
        buttonComponent.onClick.AddListener(callback);
        TMP_Text buttonText = buttonObj.GetComponentInChildren<TMP_Text>();
        if (buttonText)
        {
            buttonText.text = actionName;
        }
    }
    
    public void CreateInventoryButton(string itemName, UnityAction callback)
    {
        
        GameObject buttonObj = Instantiate(actionButtonPrefab, itemButtonContainer);
        Button buttonComponent = buttonObj.GetComponent<Button>();
        buttonComponent.onClick.AddListener(callback);
        TMP_Text buttonText = buttonObj.GetComponentInChildren<TMP_Text>();
        if (buttonText)
        {
            buttonText.text = itemName;
        }
    }
    
    
    public Sprite GetSpriteByName(string name)
    {
        return Resources.Load<Sprite>($"Images/{name}");
    }
}



[System.Serializable]
public class Room
{
    public string room_id;
    public string description;
    public string[] npcs;
    public List<string> items;
    public List<string> shop_inventory;
    public Action[] actions;
    public Exit[] exits;
    public Combat combat;
    public RoomEvent[] events;
    public List<NPCDialogue> dialogues;


    [System.Serializable]
    public class Action
    {
        public string action_description;
        public string action_id;
        public string flag_false;
        public string flag_true;

    }
    
    
    [System.Serializable]
    public class Exit
    {
        public string exit_name;
        public string leads_to;
        public string[] conditions;
        public string[] conditions_not
            ;
    }
    
    [System.Serializable]
    public class DialogueStep
    {
        public string message;
        public Response[] responses;
    }

    [System.Serializable]
    public class Response
    {
        public string text;
        public string setFlagTrue;
        public string setFlagFalse;
        public string setFlagConcluded; // finish quest, for example
        public string getItem;  // get item from NPC
        public string giveItem; // give item to NPC

        public int next_step;
    }

    
    [System.Serializable]
    public class NPCDialogue
    {
        public string npc_name;
        public string dialogue_image;
        public DialogueStep[] dialogues;
    }
    
    [System.Serializable]
    public class RoomEvent
    {
        public string event_type;
        public string item_id;         // for "add_item" event
        public string flag_name;       // for "set_flag" event
        public string value;             // for "set_flag" event
    }
    
    [System.Serializable]
    public class Combat
    {
        public string enemy_name;
        public int enemy_health;
        public string[] combat_actions;
        public int enemyDamage = 5;  // How much damage the enemy does
    }
}


[System.Serializable]
public class NPC
{
    public string name;
    public string type;
    public int level;
    public string dialogue;
    public string item;
}