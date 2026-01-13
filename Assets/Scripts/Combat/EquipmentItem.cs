using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Equipment item that can be worn or wielded.
/// Extends the base Item class with combat-relevant properties.
/// </summary>
[Serializable]
public class EquipmentItem : Item
{
    // Equipment slot
    public EquipmentSlotType slotType;

    // Armor properties
    public bool isShield;
    public int armorClass;          // AC value (armor) or AC bonus (shield)
    public int maxDexBonus = 99;    // Maximum DEX bonus allowed (99 = unlimited)
    public bool stealthDisadvantage; // Heavy armor typically causes stealth disadvantage

    // Weapon properties
    public string damageDiceNotation;   // e.g., "1d8", "2d6"
    public DamageType damageType;
    public List<WeaponProperty> properties = new List<WeaponProperty>();
    public int range;                   // For ranged/thrown weapons (in feet)
    public int longRange;               // Long range (with disadvantage)

    // Magic properties
    public int magicBonus;              // +1, +2, +3 weapon/armor
    public bool isAttuned;              // Requires attunement

    // Stat bonuses (e.g., Ring of Strength +2)
    public SerializableDictionary<string, int> statBonuses = new SerializableDictionary<string, int>();

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

    /// <summary>
    /// Check if weapon has a specific property.
    /// </summary>
    public bool HasProperty(WeaponProperty property)
    {
        return properties != null && properties.Contains(property);
    }

    /// <summary>
    /// Roll weapon damage.
    /// </summary>
    public int RollDamage()
    {
        if (DamageDice == null)
        {
            Debug.LogWarning($"No damage dice defined for {shortDescription}");
            return 1; // Minimum 1 damage
        }
        return DamageDice.Roll();
    }

    /// <summary>
    /// Roll critical hit damage (double dice).
    /// </summary>
    public int RollCriticalDamage()
    {
        if (DamageDice == null)
        {
            return 2;
        }
        return DamageDice.RollCritical();
    }

    /// <summary>
    /// Get damage dice as RollResult for detailed display.
    /// </summary>
    public RollResult RollDamageWithDetails()
    {
        if (DamageDice == null)
        {
            return new RollResult { total = 1, notation = "1", individualRolls = new int[] { 1 } };
        }
        return DamageDice.RollWithDetails();
    }

    // ============================================
    // Factory methods for common equipment
    // ============================================

    #region Weapons

    public static EquipmentItem CreateDagger()
    {
        return new EquipmentItem
        {
            shortDescription = "Dagger",
            description = "A small, easily concealed blade.",
            slotType = EquipmentSlotType.MainHand,
            damageDiceNotation = "1d4",
            damageType = DamageType.Piercing,
            properties = new List<WeaponProperty> { WeaponProperty.Finesse, WeaponProperty.Light, WeaponProperty.Thrown },
            range = 20,
            longRange = 60,
            category = "simple",
            buyPrice = 2,
            sellPrice = 1,
            image = "weapon_dagger"
        };
    }

    public static EquipmentItem CreateShortsword()
    {
        return new EquipmentItem
        {
            shortDescription = "Shortsword",
            description = "A versatile one-handed sword.",
            slotType = EquipmentSlotType.MainHand,
            damageDiceNotation = "1d6",
            damageType = DamageType.Piercing,
            properties = new List<WeaponProperty> { WeaponProperty.Finesse, WeaponProperty.Light },
            category = "martial",
            buyPrice = 10,
            sellPrice = 5,
            image = "weapon_sword"
        };
    }

    public static EquipmentItem CreateLongsword()
    {
        return new EquipmentItem
        {
            shortDescription = "Longsword",
            description = "A reliable one-handed sword that can be wielded with two hands for more damage.",
            slotType = EquipmentSlotType.MainHand,
            damageDiceNotation = "1d8",
            damageType = DamageType.Slashing,
            properties = new List<WeaponProperty> { WeaponProperty.Versatile },
            category = "martial",
            buyPrice = 15,
            sellPrice = 7,
            image = "weapon_longsword"
        };
    }

    public static EquipmentItem CreateGreatsword()
    {
        return new EquipmentItem
        {
            shortDescription = "Greatsword",
            description = "A massive two-handed blade that deals devastating damage.",
            slotType = EquipmentSlotType.MainHand,
            damageDiceNotation = "2d6",
            damageType = DamageType.Slashing,
            properties = new List<WeaponProperty> { WeaponProperty.TwoHanded, WeaponProperty.Heavy },
            category = "martial",
            buyPrice = 50,
            sellPrice = 25,
            image = "weapon_greatsword"
        };
    }

    public static EquipmentItem CreateShortbow()
    {
        return new EquipmentItem
        {
            shortDescription = "Shortbow",
            description = "A lightweight bow suitable for quick shots.",
            slotType = EquipmentSlotType.MainHand,
            damageDiceNotation = "1d6",
            damageType = DamageType.Piercing,
            properties = new List<WeaponProperty> { WeaponProperty.Ranged, WeaponProperty.TwoHanded },
            range = 80,
            longRange = 320,
            category = "simple",
            buyPrice = 25,
            sellPrice = 12,
            image = "weapon_bow"
        };
    }

    public static EquipmentItem CreateMace()
    {
        return new EquipmentItem
        {
            shortDescription = "Mace",
            description = "A heavy bludgeoning weapon effective against armored foes.",
            slotType = EquipmentSlotType.MainHand,
            damageDiceNotation = "1d6",
            damageType = DamageType.Bludgeoning,
            properties = new List<WeaponProperty>(),
            category = "simple",
            buyPrice = 5,
            sellPrice = 2,
            image = "weapon_mace"
        };
    }

    #endregion

    #region Armor

    public static EquipmentItem CreateLeatherArmor()
    {
        return new EquipmentItem
        {
            shortDescription = "Leather Armor",
            description = "Basic protection made from treated animal hides.",
            slotType = EquipmentSlotType.Armor,
            armorClass = 11,
            maxDexBonus = 99, // No limit
            stealthDisadvantage = false,
            category = "light",
            buyPrice = 10,
            sellPrice = 5,
            image = "armor_leather"
        };
    }

    public static EquipmentItem CreateStuddedLeather()
    {
        return new EquipmentItem
        {
            shortDescription = "Studded Leather",
            description = "Tough leather reinforced with metal studs.",
            slotType = EquipmentSlotType.Armor,
            armorClass = 12,
            maxDexBonus = 99,
            stealthDisadvantage = false,
            category = "light",
            buyPrice = 45,
            sellPrice = 22,
            image = "armor_studded"
        };
    }

    public static EquipmentItem CreateChainmail()
    {
        return new EquipmentItem
        {
            shortDescription = "Chainmail",
            description = "Interlocking metal rings providing solid protection.",
            slotType = EquipmentSlotType.Armor,
            armorClass = 16,
            maxDexBonus = 0, // No DEX bonus
            stealthDisadvantage = true,
            category = "heavy",
            buyPrice = 75,
            sellPrice = 37,
            image = "armor_chain"
        };
    }

    public static EquipmentItem CreatePlateArmor()
    {
        return new EquipmentItem
        {
            shortDescription = "Plate Armor",
            description = "The finest protection available, forged from interlocking metal plates.",
            slotType = EquipmentSlotType.Armor,
            armorClass = 18,
            maxDexBonus = 0,
            stealthDisadvantage = true,
            category = "heavy",
            buyPrice = 1500,
            sellPrice = 750,
            image = "armor_plate"
        };
    }

    public static EquipmentItem CreateShield()
    {
        return new EquipmentItem
        {
            shortDescription = "Shield",
            description = "A wooden or metal shield providing additional protection.",
            slotType = EquipmentSlotType.OffHand,
            isShield = true,
            armorClass = 2, // +2 AC bonus
            category = "shield",
            buyPrice = 10,
            sellPrice = 5,
            image = "shield_basic"
        };
    }

    #endregion

    #region Accessories

    public static EquipmentItem CreateRingOfProtection()
    {
        return new EquipmentItem
        {
            shortDescription = "Ring of Protection",
            description = "A magical ring that provides a +1 bonus to AC.",
            slotType = EquipmentSlotType.Ring1,
            magicBonus = 1,
            category = "ring",
            buyPrice = 500,
            sellPrice = 250,
            image = "ring_protection"
        };
    }

    public static EquipmentItem CreateAmuletOfHealth()
    {
        var amulet = new EquipmentItem
        {
            shortDescription = "Amulet of Health",
            description = "This magical amulet grants +2 to Constitution.",
            slotType = EquipmentSlotType.Amulet,
            category = "amulet",
            buyPrice = 800,
            sellPrice = 400,
            image = "amulet_health"
        };
        amulet.statBonuses["Constitution"] = 2;
        return amulet;
    }

    #endregion
}

/// <summary>
/// Damage types (SRD 5.1).
/// </summary>
public enum DamageType
{
    Slashing,
    Piercing,
    Bludgeoning,
    Fire,
    Cold,
    Lightning,
    Thunder,
    Poison,
    Acid,
    Necrotic,
    Radiant,
    Force,
    Psychic
}

/// <summary>
/// Weapon properties (SRD 5.1).
/// </summary>
public enum WeaponProperty
{
    Finesse,        // Can use DEX instead of STR
    Versatile,      // Can be used one or two-handed (higher damage two-handed)
    TwoHanded,      // Requires both hands
    Light,          // Good for dual wielding
    Heavy,          // Small creatures have disadvantage
    Ranged,         // Uses DEX for attack/damage, has range
    Thrown,         // Can be thrown (uses STR or DEX for finesse)
    Reach,          // Extra 5 feet of reach
    Loading,        // Requires action to reload
    Ammunition      // Requires ammunition
}
