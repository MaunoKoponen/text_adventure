using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Extended player statistics for SRD 5.1 style combat.
/// Classless progression with skill ranks.
/// </summary>
[Serializable]
public class PlayerStats
{
    public AbilityScores abilities = new AbilityScores();

    // Level and experience
    public int level = 1;
    public int experiencePoints = 0;

    // Hit points
    public int maxHitPoints = 10;
    public int currentHitPoints = 10;
    public int temporaryHitPoints = 0;

    // Base armor class (without equipment)
    public int baseArmorClass = 10;

    // Skill ranks (classless system - players invest points in skills)
    // Key: skill name (e.g., "melee_combat", "stealth", "arcana")
    // Value: number of ranks (0-5 typical range)
    public SerializableDictionary<string, int> skillRanks = new SerializableDictionary<string, int>();

    // Proficiencies (what the player is trained in)
    public List<string> weaponProficiencies = new List<string>();  // "simple", "martial", "sword", etc.
    public List<string> armorProficiencies = new List<string>();   // "light", "medium", "heavy", "shield"

    // Status effects
    public List<string> conditions = new List<string>();  // "poisoned", "blinded", etc.

    /// <summary>
    /// Proficiency bonus based on level (SRD 5.1 formula).
    /// Levels 1-4: +2, 5-8: +3, 9-12: +4, 13-16: +5, 17-20: +6
    /// </summary>
    public int ProficiencyBonus => 2 + ((level - 1) / 4);

    /// <summary>
    /// Calculate total armor class with equipment.
    /// </summary>
    public int CalculateAC(EquipmentSlots equipment)
    {
        if (equipment == null || equipment.armor == null)
        {
            // No armor: 10 + DEX mod
            return baseArmorClass + abilities.DexterityMod;
        }

        int armorAC = equipment.armor.armorClass;
        int maxDexBonus = equipment.armor.maxDexBonus;
        int effectiveDex = Mathf.Min(abilities.DexterityMod, maxDexBonus);

        int shieldBonus = 0;
        if (equipment.offHand != null && equipment.offHand.isShield)
        {
            shieldBonus = equipment.offHand.armorClass;
        }

        return armorAC + effectiveDex + shieldBonus;
    }

    /// <summary>
    /// Get total bonus for a skill check.
    /// </summary>
    public int GetSkillBonus(string skillName)
    {
        int ranks = 0;
        if (skillRanks.ContainsKey(skillName))
        {
            ranks = skillRanks[skillName];
        }

        int abilityMod = GetAbilityModifierForSkill(skillName);

        // If player has ranks, they're considered proficient
        int profBonus = ranks > 0 ? ProficiencyBonus : 0;

        return abilityMod + ranks + profBonus;
    }

    /// <summary>
    /// Get the governing ability modifier for a skill.
    /// </summary>
    public int GetAbilityModifierForSkill(string skillName)
    {
        // Map skills to abilities (SRD 5.1 based)
        switch (skillName.ToLower())
        {
            // Strength skills
            case "athletics":
            case "melee_combat":
                return abilities.StrengthMod;

            // Dexterity skills
            case "acrobatics":
            case "sleight_of_hand":
            case "stealth":
            case "ranged_combat":
                return abilities.DexterityMod;

            // Intelligence skills
            case "arcana":
            case "history":
            case "investigation":
            case "nature":
            case "religion":
                return abilities.IntelligenceMod;

            // Wisdom skills
            case "animal_handling":
            case "insight":
            case "medicine":
            case "perception":
            case "survival":
                return abilities.WisdomMod;

            // Charisma skills
            case "deception":
            case "intimidation":
            case "performance":
            case "persuasion":
                return abilities.CharismaMod;

            default:
                return 0;
        }
    }

    /// <summary>
    /// Check if player is proficient with a weapon type.
    /// </summary>
    public bool IsProficientWithWeapon(EquipmentItem weapon)
    {
        if (weapon == null) return false;

        // Check if any proficiency matches
        foreach (var prof in weaponProficiencies)
        {
            if (weapon.category.ToLower().Contains(prof.ToLower()) ||
                weapon.shortDescription.ToLower().Contains(prof.ToLower()))
            {
                return true;
            }
        }

        // Simple weapons - everyone is usually proficient
        if (weapon.category.ToLower() == "simple")
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Check if player is proficient with armor type.
    /// </summary>
    public bool IsProficientWithArmor(EquipmentItem armor)
    {
        if (armor == null) return true; // No armor = always OK

        foreach (var prof in armorProficiencies)
        {
            if (armor.category.ToLower().Contains(prof.ToLower()))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Calculate attack bonus for a weapon.
    /// </summary>
    public int GetAttackBonus(EquipmentItem weapon)
    {
        if (weapon == null)
        {
            // Unarmed attack: STR mod + proficiency (always proficient with unarmed)
            return abilities.StrengthMod + ProficiencyBonus;
        }

        int abilityMod;

        // Check for finesse (can use DEX instead of STR)
        if (weapon.HasProperty(WeaponProperty.Finesse))
        {
            abilityMod = Mathf.Max(abilities.StrengthMod, abilities.DexterityMod);
        }
        else if (weapon.HasProperty(WeaponProperty.Ranged))
        {
            abilityMod = abilities.DexterityMod;
        }
        else
        {
            abilityMod = abilities.StrengthMod;
        }

        int profBonus = IsProficientWithWeapon(weapon) ? ProficiencyBonus : 0;
        int magicBonus = weapon.magicBonus;

        return abilityMod + profBonus + magicBonus;
    }

    /// <summary>
    /// Calculate damage bonus for a weapon.
    /// </summary>
    public int GetDamageBonus(EquipmentItem weapon)
    {
        if (weapon == null)
        {
            // Unarmed: just STR mod
            return abilities.StrengthMod;
        }

        int abilityMod;

        if (weapon.HasProperty(WeaponProperty.Finesse))
        {
            abilityMod = Mathf.Max(abilities.StrengthMod, abilities.DexterityMod);
        }
        else if (weapon.HasProperty(WeaponProperty.Ranged))
        {
            abilityMod = abilities.DexterityMod;
        }
        else
        {
            abilityMod = abilities.StrengthMod;
        }

        return abilityMod + weapon.magicBonus;
    }

    /// <summary>
    /// Get initiative modifier.
    /// </summary>
    public int GetInitiativeBonus()
    {
        return abilities.DexterityMod;
    }

    /// <summary>
    /// XP required for next level (simplified SRD progression).
    /// </summary>
    public int GetXPForNextLevel()
    {
        // Simplified XP table
        int[] xpTable = { 0, 300, 900, 2700, 6500, 14000, 23000, 34000, 48000, 64000,
                          85000, 100000, 120000, 140000, 165000, 195000, 225000, 265000, 305000, 355000 };

        if (level >= 20) return int.MaxValue;
        return xpTable[level];
    }

    /// <summary>
    /// Check if player can level up.
    /// </summary>
    public bool CanLevelUp()
    {
        return experiencePoints >= GetXPForNextLevel();
    }

    /// <summary>
    /// Level up the player. Returns skill points to spend.
    /// </summary>
    public int LevelUp()
    {
        if (!CanLevelUp()) return 0;

        level++;

        // Increase HP (average hit die + CON mod)
        // Using d8 as default hit die for classless system
        int hpGain = 5 + abilities.ConstitutionMod;
        maxHitPoints += Mathf.Max(1, hpGain);
        currentHitPoints = maxHitPoints; // Heal on level up

        // Return skill points to spend (2 + INT mod, minimum 1)
        return Mathf.Max(1, 2 + abilities.IntelligenceMod);
    }

    /// <summary>
    /// Take damage, considering temporary HP.
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (temporaryHitPoints > 0)
        {
            int tempDamage = Mathf.Min(temporaryHitPoints, amount);
            temporaryHitPoints -= tempDamage;
            amount -= tempDamage;
        }

        currentHitPoints = Mathf.Max(0, currentHitPoints - amount);
    }

    /// <summary>
    /// Heal hit points.
    /// </summary>
    public void Heal(int amount)
    {
        currentHitPoints = Mathf.Min(maxHitPoints, currentHitPoints + amount);
    }

    /// <summary>
    /// Check if player is alive.
    /// </summary>
    public bool IsAlive => currentHitPoints > 0;

    /// <summary>
    /// Check if player has a condition.
    /// </summary>
    public bool HasCondition(string condition)
    {
        return conditions.Contains(condition.ToLower());
    }

    /// <summary>
    /// Create default starting stats.
    /// </summary>
    public static PlayerStats CreateDefault()
    {
        var stats = new PlayerStats
        {
            abilities = AbilityScores.CreateStandardArray(),
            level = 1,
            experiencePoints = 0
        };

        // Default HP: 10 + CON mod
        stats.maxHitPoints = 10 + stats.abilities.ConstitutionMod;
        stats.currentHitPoints = stats.maxHitPoints;

        // Default proficiencies
        stats.weaponProficiencies.Add("simple");
        stats.armorProficiencies.Add("light");

        return stats;
    }
}

/// <summary>
/// Serializable dictionary wrapper for Unity's JsonUtility.
/// </summary>
[Serializable]
public class SerializableDictionary<TKey, TValue>
{
    [SerializeField]
    private List<TKey> keys = new List<TKey>();
    [SerializeField]
    private List<TValue> values = new List<TValue>();

    public TValue this[TKey key]
    {
        get
        {
            int index = keys.IndexOf(key);
            return index >= 0 ? values[index] : default(TValue);
        }
        set
        {
            int index = keys.IndexOf(key);
            if (index >= 0)
            {
                values[index] = value;
            }
            else
            {
                keys.Add(key);
                values.Add(value);
            }
        }
    }

    public bool ContainsKey(TKey key)
    {
        return keys.Contains(key);
    }

    public void Add(TKey key, TValue value)
    {
        if (!ContainsKey(key))
        {
            keys.Add(key);
            values.Add(value);
        }
    }

    public bool Remove(TKey key)
    {
        int index = keys.IndexOf(key);
        if (index >= 0)
        {
            keys.RemoveAt(index);
            values.RemoveAt(index);
            return true;
        }
        return false;
    }

    public int Count => keys.Count;

    public IEnumerable<TKey> Keys => keys;
    public IEnumerable<TValue> Values => values;
}
