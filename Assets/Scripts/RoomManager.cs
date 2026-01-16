using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using UnityEngine.Events;
using UnityEngine.Serialization;

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

    private Room currentRoom;
    private string previousRoom;
    private string respawnRoom;


    public static  PlayerData playerData = new PlayerData();
    private int currentDialogueStep = -1;
    private string currentNPC = "";


    private List<string> combatLog = new List<string>();

    public Diary diary;
    public static Diary Diary;

    public ShopView shopView;
    public SettingsUI settingsUI;

    // Enhanced combat system
    private CombatManager combatManager;
    private bool useEnhancedCombat = false;
    
    void SetHealth(int value)
    {
        playerData.CurrentHP = value;
        UpdateHealthDisplay();
    }

    void UpdateHealthDisplay()
    {
        if (playerData.UsesEnhancedStats)
        {
            healthText.text = $"{playerData.stats.currentHitPoints}/{playerData.stats.maxHitPoints}";
        }
        else
        {
            healthText.text = playerData.health.ToString();
        }
    }

    private void Awake()
    {
        Diary = diary;

        // Initialize CombatManager
        combatManager = FindObjectOfType<CombatManager>();
        if (combatManager == null)
        {
            // Create CombatManager if it doesn't exist
            var go = new GameObject("CombatManager");
            combatManager = go.AddComponent<CombatManager>();
        }

        // Subscribe to combat events
        combatManager.OnCombatLog += OnCombatLogEntry;
        combatManager.OnCombatEnded += OnCombatEnded;
    }

    private void OnDestroy()
    {
        if (combatManager != null)
        {
            combatManager.OnCombatLog -= OnCombatLogEntry;
            combatManager.OnCombatEnded -= OnCombatEnded;
        }
    }

    private void OnCombatLogEntry(CombatLogEntry entry)
    {
        combatLog.Add(entry.message);
        DisplayRoomInfo("");
    }

    private void OnCombatEnded(bool victory)
    {
        useEnhancedCombat = false; // Reset for next combat
        ClearCombatLog();
        currentRoom.combat = null;

        // Check if we're in dev/debug mode
        bool isDebugMode = StoryManager.Instance != null &&
                           StoryManager.Instance.CurrentStory != null &&
                           StoryManager.Instance.CurrentStory.enableDebugCommands;

        if (victory)
        {
            // Victory message
            DisplayRoomInfo("<color=green><b>VICTORY!</b></color> You defeated your enemy!\n\n");
        }
        else
        {
            // Player defeated
            if (isDebugMode)
            {
                // Debug mode: Reset HP and return to hub with loss message
                ResetHealthToMax();
                string hubRoom = StoryManager.Instance.CurrentStory.startingRoom ?? "dev_hub";
                LoadRoomFromJson(hubRoom, "<color=red><b>DEFEAT!</b></color> You were defeated in combat.\n<i>(Debug mode: HP restored, returned to hub)</i>\n\n");
            }
            else
            {
                // Normal mode: Death and respawn at temple
                SetHealth(0);
                playerData.SetFlag("Dead", "true");
                LoadRoomFromJson(respawnRoom, "<color=red><b>DEFEAT!</b></color> You wake up from an odd dream...\n\n");
            }
        }
        UpdateHealthDisplay();
    }

    /// <summary>
    /// Reset player health to maximum (used in debug mode).
    /// </summary>
    private void ResetHealthToMax()
    {
        if (playerData.UsesEnhancedStats && playerData.stats != null)
        {
            playerData.stats.currentHitPoints = playerData.stats.maxHitPoints;
        }
        else
        {
            playerData.health = 100; // Default max health for legacy system
        }
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

        // Try story-specific path first (if StoryManager exists)
        TextAsset roomData = null;
        if (StoryManager.Instance != null && StoryManager.Instance.IsStoryLoaded)
        {
            Debug.Log($"[RoomManager] StoryManager active, story: {StoryManager.Instance.CurrentStoryId}");
            roomData = StoryManager.Instance.LoadRoomData(roomId);
            if (roomData != null)
            {
                Debug.Log($"[RoomManager] Loaded room from story path");
            }
        }
        else
        {
            Debug.Log("[RoomManager] StoryManager not active, using legacy path");
        }

        // Fall back to legacy path
        if (roomData == null)
        {
            roomData = Resources.Load<TextAsset>("Rooms/" + roomId);
            if (roomData != null)
            {
                Debug.Log($"[RoomManager] Loaded room from legacy path: Rooms/{roomId}");
            }
        }

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

    
    private void HandleCombatAction(string action)
    {
        // Use enhanced combat system if player has enhanced stats
        if (useEnhancedCombat && combatManager != null && combatManager.IsInCombat)
        {
            HandleEnhancedCombatAction(action);
            return;
        }

        // Legacy combat system (backward compatible)
        HandleLegacyCombatAction(action);
    }

    private void HandleEnhancedCombatAction(string action)
    {
        if (action == "Attack")
        {
            combatManager.PlayerAttack();
            UpdateHealthDisplay();
        }
        else if (action == "Flee")
        {
            if (combatManager.PlayerFlee())
            {
                LoadRoomFromJson(previousRoom, "You cowardly flee from the Battle!\n");
            }
        }
        else if (action == "Use Item")
        {
            ToggleCombatInventory(true);
            foreach (var slot in playerData.Inventory)
            {
                Item item = slot.GetItem();
                if (item == null) continue;

                // Only show combat-usable items
                if (!item.combatUsable) continue;

                // Capture for closure
                var currentSlot = slot;
                var currentItem = item;

                string buttonText = currentSlot.quantity > 1
                    ? $"{currentItem.shortDescription} x{currentSlot.quantity}"
                    : currentItem.shortDescription;

                CreateInventoryButton(buttonText, () =>
                {
                    ToggleCombatInventory(false);
                    combatManager.PlayerUseItem(currentItem, currentSlot.itemId);
                    UpdateHealthDisplay();
                    DisplayRoomInfo("");
                });
            }

            CreateInventoryButton("Back", () =>
            {
                ToggleCombatInventory(false);
                combatLog.Add("You stopped rummaging through your bag...");
                DisplayRoomInfo("");
            });
        }
    }

    private void HandleLegacyCombatAction(string action)
    {
        if (playerData.health <= 0)
        {
            Debug.Log(">>>>> HandleCombatAction  Player health  zero!");
        }

        int totalDamage = 15; //player.equippedWeapon.damageAmount;

        // Assuming 'player' is an instance of PlayerStats
        if (action == "Attack")
        {
            combatLog.Add($"You attacked the {currentRoom.combat.enemy_name} for {totalDamage} damage!");
            currentRoom.combat.enemy_health -= totalDamage;
            combatLog.Add($" Enemy health is now {currentRoom.combat.enemy_health}");
        }

        if (action == "Flee")
        {
            ClearCombatLog();
            currentRoom.description = "You cowardly flee from the Battle!";
            currentRoom.combat = null;
            LoadRoomFromJson(previousRoom, "You cowardly flee from the Battle!\n");
            return;
        }

        if (action == "Use Item")
        {
            ToggleCombatInventory(true);
            // Make inventory buttons
            foreach (var slot in playerData.Inventory)
            {
                Item item = slot.GetItem();
                if (item == null) continue;

                // Only show combat-usable items
                if (!item.combatUsable) continue;

                // Capture for closure
                var currentSlot = slot;
                var currentItem = item;

                string buttonText = currentSlot.quantity > 1
                    ? $"{currentItem.shortDescription} x{currentSlot.quantity}"
                    : currentItem.shortDescription;

                CreateInventoryButton(buttonText, () =>
                {
                    ToggleCombatInventory(false);

                    // Consume the item
                    playerData.RemoveItem(currentSlot.itemId);

                    if (currentItem.target == Item.Target.Self)
                    {
                        if (currentItem.effectType == Item.EffectType.Heal)
                        {
                            playerData.health += currentItem.effectAmount;
                            combatLog.Add("\n" + currentItem.usageSuccess);
                            combatLog.Add("Your health is now " + playerData.health);
                        }
                    }

                    if (currentItem.target == Item.Target.NPC)
                    {
                        if (currentItem.effectType == Item.EffectType.Damage)
                        {
                            currentRoom.combat.enemy_health -= currentItem.effectAmount;
                            combatLog.Add("\n" + currentItem.usageSuccess);
                            combatLog.Add($"Enemy health is now {currentRoom.combat.enemy_health}");
                        }
                    }

                    DisplayRoomInfo(""); // Refresh the UI
                });
            }

            CreateInventoryButton("Back", () =>
            {
                ToggleCombatInventory(false);
                combatLog.Add("You stopped rummaging through your bag...");
            });
        }

        // Check if the enemy is defeated
        if (currentRoom.combat.enemy_health <= 0)
        {
            // Enemy is defeated
            Debug.Log($"{currentRoom.combat.enemy_name} has been defeated!");
            currentRoom.description = "You defeat the enemy!";
            ClearCombatLog();
            DisplayRoomInfo("");
            return; // Refresh the UI
        }
        else if (currentRoom.combat.enemy_health > 0)
        {
            EnemyAttack();
        }

        DisplayRoomInfo(""); // Refresh the UI
    }

    void ToggleCombatInventory(bool value)
    {
        if (! value)  // clear the list of buttons 
        {
            foreach (Transform child in combatInventoryPrefab.transform)
            {
                Destroy(child.gameObject);
            }
        }
        
        combatInventoryPrefab.SetActive(value);
    }
    
    private void EnemyAttack()
    {
        int enemyDamageDealt = currentRoom.combat.enemyDamage;
        SetHealth(playerData.health - enemyDamageDealt);

        combatLog.Add($"{currentRoom.combat.enemy_name} attacked you for {enemyDamageDealt} damage!");
        combatLog.Add($" Your health is now {playerData.health} ");
        combatLog.Add("\n");

        // Check if the player is defeated:
        if (playerData.health <= 0)
        {
            Debug.Log(">>>>> EnemyAttack -> player health ZERO");
            Debug.Log($"{currentRoom.combat.enemy_name} has defeated you!");
            ClearCombatLog();
            currentRoom.combat = null;

            // Check if we're in dev/debug mode
            bool isDebugMode = StoryManager.Instance != null &&
                               StoryManager.Instance.CurrentStory != null &&
                               StoryManager.Instance.CurrentStory.enableDebugCommands;

            if (isDebugMode)
            {
                // Debug mode: Reset HP and return to hub
                ResetHealthToMax();
                string hubRoom = StoryManager.Instance.CurrentStory.startingRoom ?? "dev_hub";
                LoadRoomFromJson(hubRoom, "<color=red><b>DEFEAT!</b></color> You were defeated in combat.\n<i>(Debug mode: HP restored, returned to hub)</i>\n\n");
            }
            else
            {
                // Normal mode: Death and respawn
                playerData.SetFlag("Dead", "true");
                SetHealth(0);
                LoadRoomFromJson(respawnRoom, "<color=red><b>DEFEAT!</b></color> You wake up from an odd dream...\n\n");
            }
        }
    }
    
    private void ClearCombatLog()
    {
        Debug.Log("-------- CLEAR COMBAT LOG ------");
        combatLog.Clear();
    }
    
    private string currentImage = "";
    private void DisplayRoomInfo(string extraString)
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

        // Check for enhanced combat (CombatManager-based)
        if (useEnhancedCombat && combatManager != null && combatManager.IsInCombat)
        {
            // Display combat actions for enhanced combat
            string[] enhancedCombatActions = { "Attack", "Use Item", "Flee" };
            foreach (string action in enhancedCombatActions)
            {
                Debug.Log("Enhanced combat action: " + action);
                string actionCopy = action; // Avoid closure issue
                CreateActionButton(action, () => HandleCombatAction(actionCopy));
            }
        }
        // Legacy combat (Room.Combat-based)
        else if (currentRoom.combat != null && currentRoom.combat.enemy_health > 0)
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
                    foreach (var slot in playerData.Inventory)
                    {
                        Item item = slot.GetItem();
                        if (item != null && condition == item.shortDescription)
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
            string goToRoom = null;
            string startCombat = null;

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
            if (response.go_to_room != null)
                goToRoom = response.go_to_room;
            if (response.startCombat != null)
                startCombat = response.startCombat;

            CreateActionButton(response.text, () => HandleDialogueResponse(nextStep, setFlagTrue, setFlagFalse, setFlagConcluded, getItem, giveItem, goToRoom, startCombat));
        }
    }


    private void HandleDialogueResponse(int nextStep, string FlagToSetTrue = null, string FlagToSetFalse = null, string FlagToSetConcluded = null, string getItem = null, string giveItem = null, string goToRoom = null, string startCombatEnemy = null)
    {
        if (FlagToSetTrue != null)
        {
            // Quest?
            playerData.SetFlag(FlagToSetTrue, "true");
            if (FlagToSetTrue.Contains("quest"))
            {
                SaveGameManager.SaveGame(playerData);
                Debug.Log("Quest start detected!");
                diary.OnQuestReceived(FlagToSetTrue);
                Diary = diary;
            }
        }

        if (FlagToSetFalse != null)
            playerData.SetFlag(FlagToSetFalse, "false");

        if (FlagToSetConcluded != null)
        {
            // Quest?
            playerData.SetFlag(FlagToSetConcluded, "concluded");
            if (FlagToSetConcluded.Contains("quest"))
            {
                SaveGameManager.SaveGame(playerData);
                Debug.Log("Quest finished detected!");
                diary.OnQuestConcluded(FlagToSetConcluded);
                Diary = diary;
            }
        }

        if (getItem != null)
            playerData.AddItem(getItem);
        if (giveItem != null)
            playerData.RemoveItem(giveItem);

        // Handle navigation to another room
        if (goToRoom != null)
        {
            EndDialogue();
            LoadRoomFromJson(goToRoom);
            return;
        }

        // Handle starting combat with an enemy
        if (startCombatEnemy != null)
        {
            EndDialogue();
            StartCombatWithEnemy(startCombatEnemy);
            return;
        }

        if (nextStep == 1000)
        {
            shopView.SetupShop(currentRoom.shop_inventory);
            EndDialogue();
        }
        else if (nextStep == -1)
        {
            EndDialogue();
        }
        else
        {
            currentDialogueStep = nextStep;
            DisplayDialogue();
        }
    }

    /// <summary>
    /// Start combat with an enemy loaded from Enemies/ folder.
    /// </summary>
    private void StartCombatWithEnemy(string enemyId)
    {
        Debug.Log($"[RoomManager] Starting combat with enemy: {enemyId}");

        // Try to load enemy data
        TextAsset enemyAsset = null;
        if (StoryManager.Instance != null && StoryManager.Instance.IsStoryLoaded)
        {
            enemyAsset = StoryManager.Instance.LoadEnemyData(enemyId);
        }

        // Fallback to legacy path
        if (enemyAsset == null)
        {
            enemyAsset = Resources.Load<TextAsset>($"Enemies/{enemyId}");
        }

        if (enemyAsset == null)
        {
            Debug.LogError($"Enemy not found: {enemyId}");
            DisplayRoomInfo("The enemy could not be found!\n");
            return;
        }

        // Parse enemy data
        EnemyData enemyData = JsonUtility.FromJson<EnemyData>(enemyAsset.text);
        if (enemyData == null)
        {
            Debug.LogError($"Failed to parse enemy data: {enemyId}");
            return;
        }

        // Create a fresh instance (reset HP)
        enemyData = enemyData.CreateInstance();

        // Start enhanced combat if CombatManager is available
        if (combatManager != null)
        {
            useEnhancedCombat = true;
            combatManager.StartCombat(enemyData, playerData);
            DisplayRoomInfo($"A {enemyData.enemyName} attacks!\n");
        }
        else
        {
            // Fallback: create legacy combat from enemy data
            currentRoom.combat = new Room.Combat
            {
                enemy_name = enemyData.enemyName,
                enemy_health = enemyData.maxHitPoints,
                enemyDamage = enemyData.enemyDamage,
                combat_actions = new string[] { "Attack", "Use Item", "Flee" }
            };
            DisplayRoomInfo($"A {enemyData.enemyName} attacks!\n");
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
    
    private void CreateInventoryButton(string itemName, UnityAction callback)
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
        public string go_to_room; // navigate to another room
        public string startCombat; // start combat with enemy ID (loads from Enemies/)

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
        // Legacy fields (backward compatible)
        public string enemy_name;
        public int enemy_health;
        public string[] combat_actions;
        public int enemyDamage = 5;  // How much damage the enemy does

        // Enhanced combat - reference enemy by ID (loaded from Enemies/ folder)
        public string enemyId;

        // Random encounter from pool
        public string[] enemies;     // Array of enemy IDs to pick from
        public bool random = false;  // If true, pick random enemy from array

        /// <summary>
        /// Check if this uses enhanced enemy data.
        /// </summary>
        public bool UsesEnhancedEnemy => !string.IsNullOrEmpty(enemyId) || (enemies != null && enemies.Length > 0);
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