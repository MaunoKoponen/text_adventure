using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central combat logic for SRD 5.1 style combat.
/// Handles initiative, attack rolls, damage calculation, and turn management.
/// </summary>
public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    // Current combat state
    private CombatState currentCombat;

    // Events for UI updates
    public event Action<CombatLogEntry> OnCombatLog;
    public event Action<CombatState> OnCombatStateChanged;
    public event Action<bool> OnCombatEnded; // true = victory, false = defeat

    // Reference to player data (set by RoomManager)
    private PlayerData playerData;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Set the player data reference.
    /// </summary>
    public void SetPlayerData(PlayerData data)
    {
        playerData = data;
    }

    /// <summary>
    /// Check if combat is currently active.
    /// </summary>
    public bool IsInCombat => currentCombat != null && currentCombat.isActive;

    /// <summary>
    /// Get current combat state.
    /// </summary>
    public CombatState CurrentCombat => currentCombat;

    /// <summary>
    /// Start combat with an enemy.
    /// </summary>
    public void StartCombat(EnemyData enemy, PlayerData player)
    {
        playerData = player;

        // Create fresh enemy instance
        var combatEnemy = enemy.CreateInstance();

        // Roll initiative
        int playerInitRoll = DiceRoll.RollD20();
        int enemyInitRoll = DiceRoll.RollD20();

        int playerInitBonus = player.UsesEnhancedStats ? player.stats.GetInitiativeBonus() : 0;
        int enemyInitBonus = combatEnemy.initiativeBonus;

        int playerInit = playerInitRoll + playerInitBonus;
        int enemyInit = enemyInitRoll + enemyInitBonus;

        currentCombat = new CombatState
        {
            enemy = combatEnemy,
            playerInitiative = playerInit,
            enemyInitiative = enemyInit,
            round = 1,
            isPlayerTurn = playerInit >= enemyInit, // Player wins ties
            isActive = true,
            combatLog = new List<CombatLogEntry>()
        };

        // Log combat start
        LogCombat($"Combat begins! You face a {combatEnemy.enemyName}!", CombatLogType.System);
        LogCombat($"Initiative: You rolled {playerInitRoll}+{playerInitBonus}={playerInit}, " +
                  $"{combatEnemy.enemyName} rolled {enemyInitRoll}+{enemyInitBonus}={enemyInit}", CombatLogType.System);

        if (currentCombat.isPlayerTurn)
        {
            LogCombat("You act first!", CombatLogType.System);
        }
        else
        {
            LogCombat($"The {combatEnemy.enemyName} acts first!", CombatLogType.System);
            // Enemy takes first turn
            EnemyTurn();
        }

        OnCombatStateChanged?.Invoke(currentCombat);
    }

    /// <summary>
    /// Start combat using legacy Room.Combat data (backward compatible).
    /// </summary>
    public void StartLegacyCombat(Room.Combat legacyCombat, PlayerData player)
    {
        // Convert legacy combat to EnemyData
        var enemy = new EnemyData
        {
            enemyId = legacyCombat.enemy_name.ToLower().Replace(" ", "_"),
            enemyName = legacyCombat.enemy_name,
            maxHitPoints = legacyCombat.enemy_health,
            currentHitPoints = legacyCombat.enemy_health,
            armorClass = 10, // Default AC for legacy enemies
            enemyDamage = legacyCombat.enemyDamage,
            combat_actions = legacyCombat.combat_actions
        };

        StartCombat(enemy, player);
    }

    /// <summary>
    /// Player attacks with equipped weapon.
    /// </summary>
    public AttackResult PlayerAttack()
    {
        if (!IsInCombat || !currentCombat.isPlayerTurn)
        {
            Debug.LogWarning("Cannot attack: not player's turn or not in combat");
            return null;
        }

        var enemy = currentCombat.enemy;
        EquipmentItem weapon = playerData.equipment?.GetWeapon();

        // Roll attack
        RollResult attackRoll = DiceRoll.RollD20WithDetails();
        int attackBonus = playerData.UsesEnhancedStats
            ? playerData.stats.GetAttackBonus(weapon)
            : 0; // Legacy: no bonus

        int totalAttack = attackRoll.total + attackBonus;
        bool isCritical = attackRoll.isNatural20;
        bool isCriticalMiss = attackRoll.isNatural1;
        bool isHit = isCritical || (!isCriticalMiss && totalAttack >= enemy.armorClass);

        AttackResult result = new AttackResult
        {
            attackerName = "You",
            targetName = enemy.enemyName,
            attackRoll = attackRoll.diceTotal,
            attackBonus = attackBonus,
            totalAttack = totalAttack,
            targetAC = enemy.armorClass,
            isHit = isHit,
            isCritical = isCritical,
            isCriticalMiss = isCriticalMiss
        };

        if (isHit)
        {
            // Calculate damage
            int damageRoll;
            DamageType damageType;

            if (weapon != null && weapon.DamageDice != null)
            {
                if (isCritical)
                {
                    damageRoll = weapon.RollCriticalDamage();
                }
                else
                {
                    damageRoll = weapon.RollDamage();
                }
                damageType = weapon.damageType;
            }
            else
            {
                // Unarmed or legacy: fixed damage
                damageRoll = playerData.UsesEnhancedStats ? DiceRoll.Parse("1d4").Roll() : 15;
                damageType = DamageType.Bludgeoning;
            }

            int damageBonus = playerData.UsesEnhancedStats
                ? playerData.stats.GetDamageBonus(weapon)
                : 0;

            result.damageRoll = damageRoll;
            result.damageBonus = damageBonus;
            result.damageType = damageType;

            // Apply damage with resistance/vulnerability
            int actualDamage = enemy.TakeDamage(damageRoll + damageBonus, damageType);
            result.totalDamage = actualDamage;

            // Log the attack
            string critText = isCritical ? " CRITICAL HIT!" : "";
            LogCombat($"You attack! Roll: {attackRoll.diceTotal}+{attackBonus}={totalAttack} vs AC {enemy.armorClass} - HIT!{critText}",
                CombatLogType.PlayerAttack);
            LogCombat($"Damage: {damageRoll}+{damageBonus}={actualDamage} {damageType}. " +
                      $"{enemy.enemyName} HP: {enemy.currentHitPoints}/{enemy.maxHitPoints}",
                CombatLogType.Damage);
        }
        else
        {
            string missText = isCriticalMiss ? " Critical miss!" : "";
            LogCombat($"You attack! Roll: {attackRoll.diceTotal}+{attackBonus}={totalAttack} vs AC {enemy.armorClass} - MISS!{missText}",
                CombatLogType.PlayerAttack);
        }

        // Check for victory
        if (!enemy.IsAlive)
        {
            EndCombat(true);
            return result;
        }

        // Enemy turn
        currentCombat.isPlayerTurn = false;
        EnemyTurn();

        return result;
    }

    /// <summary>
    /// Player uses an item during combat.
    /// </summary>
    /// <param name="item">The item to use</param>
    /// <param name="itemId">Optional itemId for inventory removal (new system)</param>
    public void PlayerUseItem(Item item, string itemId = null)
    {
        if (!IsInCombat || !currentCombat.isPlayerTurn)
        {
            return;
        }

        // Process item effect
        if (item.target == Item.Target.Self)
        {
            if (item.effectType == Item.EffectType.Heal)
            {
                if (playerData.UsesEnhancedStats)
                {
                    playerData.stats.Heal(item.effectAmount);
                    LogCombat($"You use {item.shortDescription}. {item.usageSuccess} " +
                              $"HP: {playerData.stats.currentHitPoints}/{playerData.stats.maxHitPoints}",
                        CombatLogType.ItemUse);
                }
                else
                {
                    playerData.health += item.effectAmount;
                    LogCombat($"You use {item.shortDescription}. {item.usageSuccess} HP: {playerData.health}",
                        CombatLogType.ItemUse);
                }
            }
        }
        else if (item.target == Item.Target.NPC)
        {
            if (item.effectType == Item.EffectType.Damage)
            {
                var enemy = currentCombat.enemy;
                // Damage items bypass AC (magic effect)
                int damage = enemy.TakeDamage(item.effectAmount, DamageType.Force);
                LogCombat($"You use {item.shortDescription}. {item.usageSuccess} " +
                          $"{enemy.enemyName} takes {damage} damage! HP: {enemy.currentHitPoints}/{enemy.maxHitPoints}",
                    CombatLogType.ItemUse);

                if (!enemy.IsAlive)
                {
                    EndCombat(true);
                    return;
                }
            }
        }

        // Consume item - use itemId if provided (new system), otherwise fall back to Item reference
        string removeId = itemId ?? item.itemId ?? item.shortDescription;
        playerData.RemoveItem(removeId);

        // Enemy turn
        currentCombat.isPlayerTurn = false;
        EnemyTurn();
    }

    /// <summary>
    /// Player attempts to flee.
    /// </summary>
    public bool PlayerFlee()
    {
        if (!IsInCombat) return true;

        // Fleeing always succeeds in this simplified system
        // Could add DEX check vs enemy's speed for more realism
        LogCombat("You flee from combat!", CombatLogType.System);
        EndCombat(false, true);
        return true;
    }

    /// <summary>
    /// Enemy takes their turn.
    /// </summary>
    private void EnemyTurn()
    {
        if (!IsInCombat) return;

        var enemy = currentCombat.enemy;

        // Check if enemy wants to flee
        if (enemy.WantsToFlee())
        {
            LogCombat($"The {enemy.enemyName} flees!", CombatLogType.System);
            EndCombat(true); // Count as victory
            return;
        }

        // Select and execute attack
        EnemyAttack attack = enemy.SelectAttack();

        // Roll attack
        int attackRoll = DiceRoll.RollD20();
        int totalAttack = attackRoll + attack.attackBonus;

        // Get player AC
        int playerAC;
        if (playerData.UsesEnhancedStats)
        {
            playerAC = playerData.stats.CalculateAC(playerData.equipment);
        }
        else
        {
            playerAC = 10; // Legacy: fixed AC
        }

        bool isCritical = attackRoll == 20;
        bool isCriticalMiss = attackRoll == 1;
        bool isHit = isCritical || (!isCriticalMiss && totalAttack >= playerAC);

        if (isHit)
        {
            int damage;
            if (attack.DamageDice != null)
            {
                damage = isCritical ? attack.DamageDice.RollCritical() : attack.DamageDice.Roll();
            }
            else
            {
                // Legacy damage
                damage = enemy.enemyDamage;
            }

            // Apply damage
            if (playerData.UsesEnhancedStats)
            {
                playerData.stats.TakeDamage(damage);
            }
            else
            {
                playerData.health -= damage;
            }

            string critText = isCritical ? " CRITICAL HIT!" : "";
            LogCombat($"{enemy.enemyName} uses {attack.attackName}! " +
                      $"Roll: {attackRoll}+{attack.attackBonus}={totalAttack} vs AC {playerAC} - HIT!{critText}",
                CombatLogType.EnemyAttack);

            int currentHP = playerData.UsesEnhancedStats ? playerData.stats.currentHitPoints : playerData.health;
            int maxHP = playerData.UsesEnhancedStats ? playerData.stats.maxHitPoints : 100;
            LogCombat($"You take {damage} damage! HP: {currentHP}/{maxHP}", CombatLogType.Damage);

            // Check for defeat
            if (currentHP <= 0)
            {
                EndCombat(false);
                return;
            }
        }
        else
        {
            LogCombat($"{enemy.enemyName} uses {attack.attackName}! " +
                      $"Roll: {attackRoll}+{attack.attackBonus}={totalAttack} vs AC {playerAC} - MISS!",
                CombatLogType.EnemyAttack);
        }

        // Back to player's turn
        currentCombat.isPlayerTurn = true;
        currentCombat.round++;
        OnCombatStateChanged?.Invoke(currentCombat);
    }

    /// <summary>
    /// End combat.
    /// </summary>
    private void EndCombat(bool victory, bool fled = false)
    {
        if (!IsInCombat) return;

        if (victory && !fled)
        {
            var enemy = currentCombat.enemy;

            // Award XP
            int xpGain = enemy.GetXPReward();
            if (playerData.UsesEnhancedStats)
            {
                playerData.stats.experiencePoints += xpGain;
                LogCombat($"Victory! {enemy.enemyName} defeated!", CombatLogType.System);
                LogCombat($"You gain {xpGain} XP. Total: {playerData.stats.experiencePoints}", CombatLogType.System);

                // Check level up
                if (playerData.stats.CanLevelUp())
                {
                    int skillPoints = playerData.stats.LevelUp();
                    LogCombat($"LEVEL UP! You are now level {playerData.stats.level}. " +
                              $"Gained {skillPoints} skill points.", CombatLogType.System);
                }
            }
            else
            {
                LogCombat($"Victory! {enemy.enemyName} defeated!", CombatLogType.System);
            }

            // Roll loot
            if (enemy.lootTable != null)
            {
                int gold = enemy.lootTable.RollGold();
                if (gold > 0)
                {
                    playerData.coins += gold;
                    LogCombat($"Found {gold} gold.", CombatLogType.System);
                }

                var items = enemy.lootTable.RollLoot();
                foreach (var itemId in items)
                {
                    playerData.AddItem(itemId);
                    LogCombat($"Found: {itemId}", CombatLogType.System);
                }
            }
        }
        else if (fled)
        {
            LogCombat("You escaped!", CombatLogType.System);
        }
        else
        {
            LogCombat("You have been defeated...", CombatLogType.System);
            if (playerData.UsesEnhancedStats)
            {
                playerData.SetFlag("Dead", "true");
            }
        }

        currentCombat.isActive = false;
        OnCombatEnded?.Invoke(victory);
    }

    /// <summary>
    /// Add entry to combat log.
    /// </summary>
    private void LogCombat(string message, CombatLogType type)
    {
        var entry = new CombatLogEntry
        {
            message = message,
            type = type,
            timestamp = Time.time
        };

        currentCombat?.combatLog?.Add(entry);
        OnCombatLog?.Invoke(entry);
        Debug.Log($"[Combat] {message}");
    }

    /// <summary>
    /// Get full combat log as formatted string.
    /// </summary>
    public string GetCombatLogText()
    {
        if (currentCombat?.combatLog == null) return "";

        var sb = new System.Text.StringBuilder();
        foreach (var entry in currentCombat.combatLog)
        {
            sb.AppendLine(entry.message);
        }
        return sb.ToString();
    }
}

/// <summary>
/// Current state of combat.
/// </summary>
[Serializable]
public class CombatState
{
    public EnemyData enemy;
    public int playerInitiative;
    public int enemyInitiative;
    public int round;
    public bool isPlayerTurn;
    public bool isActive;
    public List<CombatLogEntry> combatLog;
}

/// <summary>
/// Result of an attack action.
/// </summary>
[Serializable]
public class AttackResult
{
    public string attackerName;
    public string targetName;
    public int attackRoll;
    public int attackBonus;
    public int totalAttack;
    public int targetAC;
    public bool isHit;
    public bool isCritical;
    public bool isCriticalMiss;
    public int damageRoll;
    public int damageBonus;
    public int totalDamage;
    public DamageType damageType;
}

/// <summary>
/// Combat log entry.
/// </summary>
[Serializable]
public class CombatLogEntry
{
    public string message;
    public CombatLogType type;
    public float timestamp;
}

/// <summary>
/// Types of combat log messages.
/// </summary>
public enum CombatLogType
{
    System,
    PlayerAttack,
    EnemyAttack,
    Damage,
    ItemUse,
    StatusEffect
}
