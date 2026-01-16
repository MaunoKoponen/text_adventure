using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enhanced enemy data for SRD 5.1 style combat.
/// Can be loaded from JSON or defined in code.
/// </summary>
[Serializable]
public class EnemyData
{
    public string enemyId;
    public string enemyName;
    public string description;

    // Combat stats
    public int armorClass = 10;
    public int maxHitPoints = 10;
    public int currentHitPoints = 10;
    public int challengeRating = 1;     // For XP calculation
    public int initiativeBonus = 0;

    // Ability scores (simplified - mainly for saves and checks)
    public AbilityScores abilities = new AbilityScores();

    // Attack options
    public List<EnemyAttack> attacks = new List<EnemyAttack>();

    // Backward compatibility with simple combat
    public string[] combat_actions;     // Legacy: ["Attack", "Use Item", "Flee"]
    public int enemyDamage = 5;         // Legacy: fixed damage

    // AI behavior
    public EnemyBehavior behavior = EnemyBehavior.Aggressive;
    public int fleeThresholdPercent = 20; // Flee when HP drops below this %

    // Resistances and immunities
    public List<DamageType> immunities = new List<DamageType>();
    public List<DamageType> resistances = new List<DamageType>();
    public List<DamageType> vulnerabilities = new List<DamageType>();
    public List<string> conditionImmunities = new List<string>();

    // Loot
    public LootTable lootTable;

    // Visual
    public string imagePath;

    /// <summary>
    /// Check if this enemy uses the enhanced combat system.
    /// </summary>
    public bool UsesEnhancedCombat => attacks != null && attacks.Count > 0;

    /// <summary>
    /// Get XP reward for defeating this enemy (based on CR).
    /// Simplified SRD 5.1 XP by CR table.
    /// </summary>
    public int GetXPReward()
    {
        int[] xpByCR = { 10, 25, 50, 100, 200, 450, 700, 1100, 1800, 2300,
                         2900, 3900, 5000, 5900, 7200, 8400, 10000, 11500, 13000, 15000,
                         18000, 20000, 22000, 25000, 33000, 41000, 50000, 62000, 75000, 90000, 105000, 120000, 135000, 155000 };

        if (challengeRating < 0) return 0;
        if (challengeRating >= xpByCR.Length) return xpByCR[xpByCR.Length - 1];
        return xpByCR[challengeRating];
    }

    /// <summary>
    /// Select an attack to use (AI decision).
    /// </summary>
    public EnemyAttack SelectAttack()
    {
        if (attacks == null || attacks.Count == 0)
        {
            // Return a default attack using legacy damage
            return new EnemyAttack
            {
                attackName = "Attack",
                attackBonus = 0,
                damageDiceNotation = $"1d{enemyDamage * 2}",
                damageType = DamageType.Bludgeoning,
                description = $"The {enemyName} attacks!"
            };
        }

        // Simple AI: pick random attack, with preference for stronger attacks at low HP
        if (attacks.Count == 1)
        {
            return attacks[0];
        }

        // More sophisticated: could weight by damage, current situation, etc.
        return attacks[UnityEngine.Random.Range(0, attacks.Count)];
    }

    /// <summary>
    /// Calculate damage after applying resistances/vulnerabilities.
    /// </summary>
    public int ApplyDamageModifiers(int damage, DamageType damageType)
    {
        if (immunities.Contains(damageType))
        {
            return 0;
        }

        if (resistances.Contains(damageType))
        {
            return damage / 2;
        }

        if (vulnerabilities.Contains(damageType))
        {
            return damage * 2;
        }

        return damage;
    }

    /// <summary>
    /// Take damage and return actual damage dealt.
    /// </summary>
    public int TakeDamage(int damage, DamageType damageType)
    {
        int actualDamage = ApplyDamageModifiers(damage, damageType);
        currentHitPoints = Mathf.Max(0, currentHitPoints - actualDamage);
        return actualDamage;
    }

    /// <summary>
    /// Check if enemy is alive.
    /// </summary>
    public bool IsAlive => currentHitPoints > 0;

    /// <summary>
    /// Check if enemy wants to flee (based on behavior and HP).
    /// </summary>
    public bool WantsToFlee()
    {
        if (behavior == EnemyBehavior.Cowardly && currentHitPoints < maxHitPoints * 0.5f)
        {
            return true;
        }

        float hpPercent = (float)currentHitPoints / maxHitPoints * 100f;
        return hpPercent <= fleeThresholdPercent && behavior != EnemyBehavior.Aggressive;
    }

    /// <summary>
    /// Create a copy with reset HP (for spawning fresh instances).
    /// </summary>
    public EnemyData CreateInstance()
    {
        var instance = (EnemyData)this.MemberwiseClone();
        instance.currentHitPoints = instance.maxHitPoints;
        return instance;
    }

    // ============================================
    // Factory methods for common enemies
    // ============================================

    public static EnemyData CreateGoblin()
    {
        var goblin = new EnemyData
        {
            enemyId = "goblin",
            enemyName = "Goblin",
            description = "A small, cunning humanoid with green skin and sharp teeth.",
            armorClass = 15,
            maxHitPoints = 7,
            currentHitPoints = 7,
            challengeRating = 1,
            initiativeBonus = 2,
            behavior = EnemyBehavior.Cowardly,
            fleeThresholdPercent = 30,
            imagePath = "enemies/goblin"
        };

        goblin.abilities = AbilityScores.Create(8, 14, 10, 10, 8, 8);

        goblin.attacks.Add(new EnemyAttack
        {
            attackName = "Scimitar",
            attackBonus = 4,
            damageDiceNotation = "1d6+2",
            damageType = DamageType.Slashing,
            description = "The goblin slashes with its scimitar!"
        });

        goblin.attacks.Add(new EnemyAttack
        {
            attackName = "Shortbow",
            attackBonus = 4,
            damageDiceNotation = "1d6+2",
            damageType = DamageType.Piercing,
            description = "The goblin fires an arrow!"
        });

        return goblin;
    }

    public static EnemyData CreateSkeleton()
    {
        var skeleton = new EnemyData
        {
            enemyId = "skeleton",
            enemyName = "Skeleton",
            description = "An animated pile of bones held together by dark magic.",
            armorClass = 13,
            maxHitPoints = 13,
            currentHitPoints = 13,
            challengeRating = 1,
            initiativeBonus = 2,
            behavior = EnemyBehavior.Aggressive,
            imagePath = "enemies/skeleton"
        };

        skeleton.abilities = AbilityScores.Create(10, 14, 15, 6, 8, 5);

        skeleton.vulnerabilities.Add(DamageType.Bludgeoning);
        skeleton.immunities.Add(DamageType.Poison);
        skeleton.conditionImmunities.Add("poisoned");
        skeleton.conditionImmunities.Add("exhaustion");

        skeleton.attacks.Add(new EnemyAttack
        {
            attackName = "Shortsword",
            attackBonus = 4,
            damageDiceNotation = "1d6+2",
            damageType = DamageType.Piercing,
            description = "The skeleton thrusts with its rusty blade!"
        });

        return skeleton;
    }

    public static EnemyData CreateOgre()
    {
        var ogre = new EnemyData
        {
            enemyId = "ogre",
            enemyName = "Ogre",
            description = "A massive, dim-witted brute with tremendous strength.",
            armorClass = 11,
            maxHitPoints = 59,
            currentHitPoints = 59,
            challengeRating = 2,
            initiativeBonus = -1,
            behavior = EnemyBehavior.Aggressive,
            imagePath = "enemies/ogre"
        };

        ogre.abilities = AbilityScores.Create(19, 8, 16, 5, 7, 7);

        ogre.attacks.Add(new EnemyAttack
        {
            attackName = "Greatclub",
            attackBonus = 6,
            damageDiceNotation = "2d8+4",
            damageType = DamageType.Bludgeoning,
            description = "The ogre swings its massive club!"
        });

        return ogre;
    }

    public static EnemyData CreateKnight()
    {
        var knight = new EnemyData
        {
            enemyId = "knight",
            enemyName = "Knight",
            description = "A heavily armored warrior skilled in combat.",
            armorClass = 18,
            maxHitPoints = 52,
            currentHitPoints = 52,
            challengeRating = 3,
            initiativeBonus = 0,
            behavior = EnemyBehavior.Tactical,
            imagePath = "enemies/knight"
        };

        knight.abilities = AbilityScores.Create(16, 11, 14, 11, 11, 15);

        knight.attacks.Add(new EnemyAttack
        {
            attackName = "Greatsword",
            attackBonus = 5,
            damageDiceNotation = "2d6+3",
            damageType = DamageType.Slashing,
            description = "The knight swings their greatsword with precision!"
        });

        knight.attacks.Add(new EnemyAttack
        {
            attackName = "Heavy Crossbow",
            attackBonus = 2,
            damageDiceNotation = "1d10",
            damageType = DamageType.Piercing,
            description = "The knight fires a crossbow bolt!"
        });

        return knight;
    }
}

/// <summary>
/// A single attack option for an enemy.
/// </summary>
[Serializable]
public class EnemyAttack
{
    public string attackName;
    public int attackBonus;             // Added to d20 roll
    public string damageDiceNotation;   // e.g., "1d8+3"
    public DamageType damageType;
    public string description;          // Combat log text
    public int rechargeRoll;            // 0 = always available, 5-6 = recharge on 5 or 6

    // Parsed damage dice (lazy-loaded)
    [NonSerialized]
    private DiceRoll _damageDice;

    public DiceRoll DamageDice
    {
        get
        {
            if (_damageDice == null && !string.IsNullOrEmpty(damageDiceNotation))
            {
                _damageDice = DiceRoll.Parse(damageDiceNotation);
            }
            return _damageDice;
        }
    }

    public int RollDamage()
    {
        return DamageDice?.Roll() ?? 1;
    }
}

/// <summary>
/// Enemy AI behavior patterns.
/// </summary>
public enum EnemyBehavior
{
    Aggressive,     // Always attacks, never flees
    Defensive,      // Prioritizes survival, may use healing
    Tactical,       // Uses abilities strategically
    Cowardly,       // Flees when hurt
    Support         // Helps other enemies (buffs, heals)
}

/// <summary>
/// Loot dropped by enemies.
/// Supports inline loot entries or reference to a shared loot table.
/// </summary>
[Serializable]
public class LootTable
{
    // Option 1: Reference a shared loot table by ID (for future use)
    // If set, this takes priority over inline items
    public string lootTableId;

    // Option 2: Inline loot definition
    public int goldMin;
    public int goldMax;
    public List<LootEntry> items = new List<LootEntry>();

    // Guaranteed drops (always given, not rolled)
    public List<string> guaranteedItems = new List<string>();

    public int RollGold()
    {
        return UnityEngine.Random.Range(goldMin, goldMax + 1);
    }

    public List<string> RollLoot()
    {
        List<string> droppedItems = new List<string>();

        // Add guaranteed drops first
        if (guaranteedItems != null)
        {
            droppedItems.AddRange(guaranteedItems);
        }

        // Roll for random drops
        foreach (var entry in items)
        {
            float roll = UnityEngine.Random.Range(0f, 100f);
            if (roll <= entry.dropChance)
            {
                int quantity = UnityEngine.Random.Range(entry.minQuantity, entry.maxQuantity + 1);
                for (int i = 0; i < quantity; i++)
                {
                    droppedItems.Add(entry.itemId);
                }
            }
        }

        return droppedItems;
    }

    /// <summary>
    /// Check if this should use a shared loot table instead of inline items.
    /// </summary>
    public bool UsesSharedTable => !string.IsNullOrEmpty(lootTableId);
}

[Serializable]
public class LootEntry
{
    public string itemId;
    public float dropChance;    // Percentage (0-100)
    public int minQuantity = 1;
    public int maxQuantity = 1;
}
