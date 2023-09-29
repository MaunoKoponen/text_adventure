using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using UnityEngine.Events;

public class RoomManager : MonoBehaviour
{
    public ImageFade imageFadeInstance;
    public TMP_Text roomDescriptionText;
    public TMP_Text inventoryText;

    public GameObject actionButtonPrefab;
    public Transform actionButtonContainer;
    public Image image;
    public Narrator narrator; // Link this in the Inspector
    
    private Room currentRoom;
    private PlayerData playerData = new PlayerData();
    private int currentDialogueStep = -1;
    private string currentNPC = "";

    public PlayerStats player = new PlayerStats();
    
    private List<string> combatLog = new List<string>();

    void Start()
    {
        // Example initialization
        player.health = 100;
        player.equippedWeapon = new Weapon 
        {
            name = "Sword",
            damageAmount = 10,
            damageType = Weapon.DamageType.Physical
        };
        
        LoadRoomFromJson("town_square");
        
    }

    private void LoadRoomFromJson(string roomId)
    {
        TextAsset roomData = Resources.Load<TextAsset>("Rooms/" + roomId);

        if (roomData != null)
        {
            string jsonData = roomData.text;
            Debug.Log("JSON string: " + jsonData);
            currentRoom = JsonUtility.FromJson<Room>(jsonData);
            
            DisplayRoomInfo();
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
                    playerData.Inventory.Add(roomEvent.item_id);
                    break;
                case "set_flag":
                    if (playerData.flags.ContainsKey(roomEvent.flag_name))
                    {
                        playerData.flags[roomEvent.flag_name] = roomEvent.value;
                    }
                    else
                    {
                        playerData.flags.Add(roomEvent.flag_name, roomEvent.value);
                    }
                    break;
                case "pick_item":
                    playerData.AddItem(roomEvent.item_id);
                    break;
            }
        }
    }

    
    private void HandleCombatAction(string action)
    {
        Debug.Log("handle combat action");
        
        int totalDamage = player.equippedWeapon.damageAmount;
        
        // Assuming 'player' is an instance of PlayerStats
        if (action == "Attack")
        {
            /* todo later:
            switch (player.equippedWeapon.damageType)
            {
                case Weapon.DamageType.Fire:
                    totalDamage = totalDamage * (100 - player.fireResistance) / 100;
                    break;
                case Weapon.DamageType.Cold:
                    totalDamage = totalDamage * (100 - player.coldResistance) / 100;
                    break;
                case Weapon.DamageType.Poison:
                    totalDamage = totalDamage * (100 - player.poisonResistance) / 100;
                    break;
                
            }
            */

            combatLog.Add($"You attacked the {currentRoom.combat.enemy_name} for {totalDamage} damage!");

            currentRoom.combat.enemy_health -= totalDamage;

            combatLog.Add($" Enemy health is now {currentRoom.combat.enemy_health}");

            Debug.Log("enemy health: " + currentRoom.combat.enemy_health);
            
            // Check if the enemy is defeated
            if (currentRoom.combat.enemy_health <= 0)
            {
                // Enemy is defeated
                Debug.Log($"{currentRoom.combat.enemy_name} has been defeated!");
                ClearCombatLog();
            }
        }

        if (currentRoom.combat.enemy_health > 0)
        {
            EnemyAttack();
        }

        DisplayRoomInfo(); // Refresh the UI
    }

    private void EnemyAttack()
    {
        int enemyDamageDealt = currentRoom.combat.enemyDamage;
        player.health -= enemyDamageDealt;

        combatLog.Add($"{currentRoom.combat.enemy_name} attacked you for {enemyDamageDealt} damage!");
        combatLog.Add($" Your health is now {player.health}");

        
        // Check if the player is defeated:
        if (player.health <= 0)
        {
            combatLog.Add("You have been defeated!");
            // Add logic here for player defeat if necessary (e.g., restart game, etc.)
            ClearCombatLog();
        }
    }
    
    private void ClearCombatLog()
    {
        combatLog.Clear();
    }
    
    private string currentImage = "";
    private void DisplayRoomInfo()
    {
        //roomDescriptionText.text = currentRoom.description;
        roomDescriptionText.text = currentRoom.default_description + "\n\n" + string.Join("\n", combatLog);

        string imageName = currentRoom.room_id;
        if (imageName != currentImage)
        {
            var newSprite = GetSpriteByName(imageName);
            imageFadeInstance.SetImageWithFade(newSprite);
            currentImage = imageName;
        }
        
        narrator.PlayNarration(currentRoom.room_id);
        
        Debug.Log(currentRoom.default_description);
        
        // Clear existing buttons
        foreach (Transform child in actionButtonContainer)
        {
            Destroy(child.gameObject);
        }

        if (currentRoom.combat != null && currentRoom.combat.enemy_health > 0)
        {
            // Display combat actions
            foreach (string action in currentRoom.combat.combat_actions)
            {
                Debug.Log("Combat action: "  + action);
                CreateActionButton(action, () => HandleCombatAction(action));
            }
        }
        else
        {
            // Display room actions and exits
            //foreach (string action in currentRoom.actions)
            //{
            //    Debug.Log("RoomAction ..." + action);
            //    CreateActionButton(action, () => HandleRoomAction(action));
            //}
            
            foreach (var action in currentRoom.actions)
            {
                CreateActionButton(action.action_description, () => HandleRoomAction(action.action_id));
            }
            
            foreach (string item in currentRoom.items)
            {
                CreateActionButton($"Pick up {item}", () => PickUpItem(item));
            }
            
            foreach (Room.Exit exit in currentRoom.exits)
            {
                Debug.Log("exit action: ..." + exit.exit_name );
                CreateActionButton(exit.exit_name, () => LoadRoomFromJson(exit.leads_to));
            }
        }

        string inventoryString = "";
        foreach (var item in playerData.Inventory)
        {
            Debug.Log("add to text: " + item);
            
            inventoryString += " - " + item + "\n";
        }

        inventoryText.text = inventoryString;
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
        Room.NPCDialogue dialogue = currentRoom.npc_dialogues.FirstOrDefault(d => d.npc_name == actionName);
        if (dialogue != null)
        {
            StartDialogue(actionName);
        }

        // Further custom actions (like picking up items, getting aa quest, getting item from NPC ) can be added here
    }

    private void PickUpItem(string itemId)
    {
        playerData.AddItem(itemId);
        currentRoom.items.Remove(itemId);
        DisplayRoomInfo(); // Refresh the UI
    }
    private void ReceiveItem(string itemId)
    {
      Debug.Log("received item   " + itemId);
        
        playerData.AddItem(itemId);
        DisplayRoomInfo(); // Refresh the UI
    }

    
    
    private void StartDialogue(string npc)
    {
        currentNPC = npc;
        currentDialogueStep = 0;
        DisplayDialogue();
        
        var newSprite = GetSpriteByName(npc);
        imageFadeInstance.SetImageWithFade(newSprite);
        currentImage = npc;
        
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
        Room.NPCDialogue dialogue = currentRoom.npc_dialogues.FirstOrDefault(d => d.npc_name == currentNPC);

        if (dialogue == null) return;

        if (currentDialogueStep > 1000)
            currentDialogueStep -= 1000;
        roomDescriptionText.text = ParseMessage(dialogue.dialogues[currentDialogueStep].message);
        
        foreach (Transform child in actionButtonContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var response in dialogue.dialogues[currentDialogueStep].responses)
        {
            int nextStep = response.next_step;
            CreateActionButton(response.text, () => HandleDialogueResponse(nextStep));
        }
    }

    private void HandleDialogueResponse(int nextStep)
    {
        if (nextStep > 1000)
        {
            Debug.Log("should receive item");
            ReceiveItem(currentRoom.npcitems.FirstOrDefault());
        }
        
        if (nextStep == -1)
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
        DisplayRoomInfo();
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
    
        public Sprite GetSpriteByName(string name)
        {
            return Resources.Load<Sprite>($"Images/{name}");
        }
    }



[System.Serializable]
public class Room
{
    public string room_id;
    public string default_description;
    public string[] npcs;
    public List<string> items;
    public List<string> npcitems;

    public Action[] actions;
    public Exit[] exits;
    public Combat combat;
    public RoomEvent[] events;  // This is the new field
    public List<NPCDialogue> npc_dialogues;


    [System.Serializable]
    public class Action
    {
        public string action_description;
        public string action_id;
    }
    
    
    [System.Serializable]
    public class Exit
    {
        public string exit_name;
        public string leads_to;
        public string[] conditions;
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
        public int next_step;
    }

    
    [System.Serializable]
    public class NPCDialogue
    {
        public string npc_name;
        public DialogueStep[] dialogues;
    }
    
    [System.Serializable]
    public class RoomEvent
    {
        public string event_type;
        public string item_id;         // for "add_item" event
        public string flag_name;       // for "set_flag" event
        public bool value;             // for "set_flag" event
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