using System;
using UnityEngine;

/// <summary>
/// Manages equipped items across all equipment slots.
/// </summary>
[Serializable]
public class EquipmentSlots
{
    public EquipmentItem mainHand;      // Primary weapon
    public EquipmentItem offHand;       // Shield, secondary weapon, or two-handed weapon grip
    public EquipmentItem armor;         // Body armor
    public EquipmentItem helmet;        // Head slot
    public EquipmentItem boots;         // Feet slot
    public EquipmentItem gloves;        // Hands slot
    public EquipmentItem ring1;         // Ring slot 1
    public EquipmentItem ring2;         // Ring slot 2
    public EquipmentItem amulet;        // Neck slot

    /// <summary>
    /// Get equipped item by slot type.
    /// </summary>
    public EquipmentItem GetItemInSlot(EquipmentSlotType slotType)
    {
        switch (slotType)
        {
            case EquipmentSlotType.MainHand: return mainHand;
            case EquipmentSlotType.OffHand: return offHand;
            case EquipmentSlotType.Armor: return armor;
            case EquipmentSlotType.Helmet: return helmet;
            case EquipmentSlotType.Boots: return boots;
            case EquipmentSlotType.Gloves: return gloves;
            case EquipmentSlotType.Ring1: return ring1;
            case EquipmentSlotType.Ring2: return ring2;
            case EquipmentSlotType.Amulet: return amulet;
            default: return null;
        }
    }

    /// <summary>
    /// Equip an item to its designated slot.
    /// Returns the previously equipped item (if any) to be returned to inventory.
    /// </summary>
    public EquipmentItem EquipItem(EquipmentItem item)
    {
        if (item == null) return null;

        EquipmentItem previousItem = null;

        // Handle two-handed weapons
        if (item.HasProperty(WeaponProperty.TwoHanded))
        {
            previousItem = mainHand;
            mainHand = item;

            // Two-handed weapons also occupy off-hand
            if (offHand != null)
            {
                // Return off-hand item to inventory as well
                // (Caller needs to handle this case)
            }
            offHand = null;
            return previousItem;
        }

        switch (item.slotType)
        {
            case EquipmentSlotType.MainHand:
                previousItem = mainHand;
                mainHand = item;
                break;

            case EquipmentSlotType.OffHand:
                // Can't equip off-hand if main-hand is two-handed
                if (mainHand != null && mainHand.HasProperty(WeaponProperty.TwoHanded))
                {
                    Debug.LogWarning("Cannot equip off-hand item while wielding a two-handed weapon.");
                    return item; // Return the item being equipped (not equipped)
                }
                previousItem = offHand;
                offHand = item;
                break;

            case EquipmentSlotType.Armor:
                previousItem = armor;
                armor = item;
                break;

            case EquipmentSlotType.Helmet:
                previousItem = helmet;
                helmet = item;
                break;

            case EquipmentSlotType.Boots:
                previousItem = boots;
                boots = item;
                break;

            case EquipmentSlotType.Gloves:
                previousItem = gloves;
                gloves = item;
                break;

            case EquipmentSlotType.Ring1:
                previousItem = ring1;
                ring1 = item;
                break;

            case EquipmentSlotType.Ring2:
                previousItem = ring2;
                ring2 = item;
                break;

            case EquipmentSlotType.Amulet:
                previousItem = amulet;
                amulet = item;
                break;
        }

        return previousItem;
    }

    /// <summary>
    /// Unequip item from a slot.
    /// </summary>
    public EquipmentItem UnequipSlot(EquipmentSlotType slotType)
    {
        EquipmentItem item = null;

        switch (slotType)
        {
            case EquipmentSlotType.MainHand:
                item = mainHand;
                mainHand = null;
                break;

            case EquipmentSlotType.OffHand:
                item = offHand;
                offHand = null;
                break;

            case EquipmentSlotType.Armor:
                item = armor;
                armor = null;
                break;

            case EquipmentSlotType.Helmet:
                item = helmet;
                helmet = null;
                break;

            case EquipmentSlotType.Boots:
                item = boots;
                boots = null;
                break;

            case EquipmentSlotType.Gloves:
                item = gloves;
                gloves = null;
                break;

            case EquipmentSlotType.Ring1:
                item = ring1;
                ring1 = null;
                break;

            case EquipmentSlotType.Ring2:
                item = ring2;
                ring2 = null;
                break;

            case EquipmentSlotType.Amulet:
                item = amulet;
                amulet = null;
                break;
        }

        return item;
    }

    /// <summary>
    /// Get total armor class from equipped armor.
    /// </summary>
    public int GetArmorAC()
    {
        if (armor == null) return 10; // Base AC without armor
        return armor.armorClass;
    }

    /// <summary>
    /// Get shield bonus from off-hand.
    /// </summary>
    public int GetShieldBonus()
    {
        if (offHand == null || !offHand.isShield) return 0;
        return offHand.armorClass;
    }

    /// <summary>
    /// Get maximum DEX bonus allowed by armor.
    /// </summary>
    public int GetMaxDexBonus()
    {
        if (armor == null) return 99; // No limit without armor
        return armor.maxDexBonus;
    }

    /// <summary>
    /// Get total stat bonuses from all equipment.
    /// </summary>
    public int GetStatBonus(string statName)
    {
        int total = 0;

        EquipmentItem[] allEquipped = { mainHand, offHand, armor, helmet, boots, gloves, ring1, ring2, amulet };

        foreach (var item in allEquipped)
        {
            if (item != null && item.statBonuses != null)
            {
                if (item.statBonuses.ContainsKey(statName))
                {
                    total += item.statBonuses[statName];
                }
            }
        }

        return total;
    }

    /// <summary>
    /// Check if player has any weapon equipped.
    /// </summary>
    public bool HasWeaponEquipped()
    {
        return mainHand != null;
    }

    /// <summary>
    /// Get the current weapon (main hand or unarmed).
    /// </summary>
    public EquipmentItem GetWeapon()
    {
        return mainHand;
    }
}

/// <summary>
/// Types of equipment slots.
/// </summary>
public enum EquipmentSlotType
{
    MainHand,
    OffHand,
    Armor,
    Helmet,
    Boots,
    Gloves,
    Ring1,
    Ring2,
    Amulet
}
