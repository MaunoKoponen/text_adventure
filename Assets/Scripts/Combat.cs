using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Combat
{
    private RoomManager _roomManager;

    //public string enemy_name;
    //public int enemy_health;
    //public string[] combat_actions;
    //public int enemyDamage = 5;  // How much damage the enemy does
    
    public Combat(RoomManager roomManager)
    {
        _roomManager = roomManager;
    }

    public void HandleCombatAction(string action)
    {
        if (RoomManager.playerData.health <= 0)
        {
            Debug.Log(">>>>> HandleCombatAction  Player health  zero!");
        }

        
        // Assuming 'player' is an instance of PlayerStats
        if (action == "Attack")
        {

            Item mainMandWeapon = ItemRegistry.GetItem(RoomManager.playerData.Equipments["MainHand"]);
            _roomManager.combatLog.Add(mainMandWeapon.usageSuccess);

            int totalDamage = mainMandWeapon.effectAmount;  //15; //player.equippedWeapon.damageAmount;

            
            //_roomManager.combatLog.Add($"You attacked the {_roomManager.currentRoom.combat.enemy_name} for {totalDamage} damage!");

            _roomManager.currentRoom.combat.enemy_health -= totalDamage;
            
            _roomManager.combatLog.Add($" Enemy health is now {_roomManager.currentRoom.combat.enemy_health}");

            Debug.Log("enemy health: " + _roomManager.currentRoom.combat.enemy_health);
        }

        if (action == "Flee")
        {
            ClearCombatLog();
            _roomManager.currentRoom.description = "You cowardly flee from the Battle!";
            _roomManager.currentRoom.combat = null;
            _roomManager.LoadRoomFromJson(_roomManager.previousRoom, "You cowardly flee from the Battle!\n");
            return;
        }
        
        if (action == "Use Item")
        {
            ToggleCombatInventory(true);
            // make inventory buttons
            foreach (var item in RoomManager.playerData.Inventory)
            {
                _roomManager.CreateInventoryButton(item.shortDescription, () =>
                {
                    ToggleCombatInventory(false);
                    
                    if (!item.stacking)
                    {
                        RoomManager.playerData.RemoveItem(item);
                    }
                    if (item.stacking)
                    {
                        RoomManager.playerData.DecreaseStackSize(item, 1);
                    }

                    // TODO: check if usage possible (for example if target has immunity)
                    
                    if (item.target == Item.Target.Self)
                    {
                        if (item.effectType == Item.EffectType.Heal)
                        {
                            RoomManager.playerData.health += item.effectAmount;
                            _roomManager.combatLog.Add("\n" + item.usageSuccess);
                            _roomManager.combatLog.Add("your health is now " + RoomManager.playerData.health);

                        }
                    }    
                    
                    if (item.target == Item.Target.NPC)
                    {
                        if (item.effectType == Item.EffectType.Damage)
                        {
                            _roomManager.currentRoom.combat.enemy_health -= item.effectAmount;

                            _roomManager.combatLog.Add("\n" + item.usageSuccess);
                            _roomManager.combatLog.Add($" Enemy health is now {_roomManager.currentRoom.combat.enemy_health}");
                        }
                    }

                    _roomManager.DisplayRoomInfo(""); // Refresh the UI
                    
                });
            }

            _roomManager.CreateInventoryButton("Back", () =>
            {
                ToggleCombatInventory(false);
                _roomManager.combatLog.Add("You stopped rummaging through you bag...");
            });
        }
        
        // Check if the enemy is defeated
        if (_roomManager.currentRoom.combat.enemy_health <= 0)
        {
            // Enemy is defeated
            Debug.Log($"{_roomManager.currentRoom.combat.enemy_name} has been defeated!");
            _roomManager.currentRoom.description = "You defeat the enemy!";
            ClearCombatLog();
            _roomManager.DisplayRoomInfo("");
            return; // Refresh the UI
        }
        else if(_roomManager.currentRoom.combat.enemy_health > 0)
        {
            EnemyAttack();
        }

        _roomManager.DisplayRoomInfo(""); // Refresh the UI
    }

    private void ToggleCombatInventory(bool value)
    {
        if (! value)  // clear the list of buttons 
        {
            foreach (Transform child in _roomManager.combatInventoryPrefab.transform)
            {
                Object.Destroy(child.gameObject);
            }
        }

        _roomManager.combatInventoryPrefab.SetActive(value);
    }

    private void EnemyAttack()
    {
        int enemyDamageDealt = _roomManager.currentRoom.combat.enemyDamage;
        _roomManager.SetHealth(RoomManager.playerData.health - enemyDamageDealt);

        _roomManager.combatLog.Add($"{_roomManager.currentRoom.combat.enemy_name} attacked you for {enemyDamageDealt} damage!");
        _roomManager.combatLog.Add($" Your health is now {RoomManager.playerData.health} ");
        _roomManager.combatLog.Add("\n");
        
        // Check if the player is defeated:
        if (RoomManager.playerData.health <= 0)
        {
            Debug.Log(">>>>> EnemyAttack -> player health ZERO");

            RoomManager.playerData.SetFlag("Dead","true");
            Debug.Log($"{_roomManager.currentRoom.combat.enemy_name} has defeated you!");
            ClearCombatLog();
            _roomManager.currentRoom.combat = null;
            _roomManager.SetHealth(0);

            _roomManager.LoadRoomFromJson(_roomManager.respawnRoom, "You wake up from odd dream.\n");
        }
    }

    private void ClearCombatLog()
    {
        Debug.Log("-------- CLEAR COMBAT LOG ------");
        _roomManager.combatLog.Clear();
    }
}
