using System.Collections.Generic;
using UnityEngine.Serialization;

[System.Serializable]
public class Item
{
    public string usageSuccess;
    public string usageFail;
    public string description;
    public string shortDescription;
    public int buyPrice;
    public int sellPrice;
    public EffectType effectType;
    public int effectAmount; // This is to denote the strength/quantity of the effect
    public Category category;
    public Target target;
    public bool stacking;
    public int count = 0;
    public string image;
    public EquipSlot equipSlot;
    
    public enum EffectType
    {
        Damage,
        Heal,
        Bless,
        CurePoison,
        Open,
        Cold
        // ... More effects here.
    }

    public enum Category
    {
        Scroll,
        Potion,
        Key,
        Valuables,
        Rune,
        Armor,
        Weapon,
        Shield,
        Ring,
        Necklace
    }
    
    public enum EquipSlot
    {
        Helmet,
        Neck,
        Breast,
        Pants,
        Bracers,
        Gauntlets,
        Boots,
        MainHand,
        SecondaryHand,
        MainFinger,
        SecondaryFinger,
        None
    }

    
    
    public enum Target
    {
        NPC,
        Self,
        Lock,
        None
    }

    // Example items:

    public static Item BasicSword = new Item
    {
        usageSuccess = "You smash the enemy with you sword!",
        usageFail = "You swing your sword. it cuts through the air. But not through the enemy.",
        description = "An ordinary looking one-hand sword with no markings on it",
        shortDescription = "A Sword",
        buyPrice = 100,
        sellPrice = 80,
        effectType = EffectType.Damage,
        effectAmount = 10, // For example, 50 points of fire damage.
        category = Category.Weapon,
        equipSlot = EquipSlot.MainHand,
        target = Target.NPC,
        stacking = false,
        image = "sword_01"
    };
    
    public static Item IceSword = new Item
    {
        usageSuccess = "Shards of ice hit yor enemy",
        usageFail = "You swing your sword. Shards of ice appear from the blade, fly towards enemy, but misses their target!",
        description = "One-hand sword with icy touch and cold blue appearance",
        shortDescription = "An Icy Sword",
        buyPrice = 200,
        sellPrice = 120,
        effectType = EffectType.Cold,
        effectAmount = 10, // For example, 50 points of fire damage.
        category = Category.Weapon,
        equipSlot = EquipSlot.MainHand,
        target = Target.NPC,
        stacking = false,
        image = "sword_ice"
    };
    
    
    public static Item SoulStone = new Item
    {
        usageSuccess = "You focus your energy on the stone. Divine aura surround you.",
        usageFail = "You focus your energy on the stone, but nothing seems to happen. You feel cold",
        description = "A ordinary looking stone with a faint rune on it",
        shortDescription = "Soul Stone",
        buyPrice = 0,
        sellPrice = 0,
        effectType = EffectType.Bless,
        effectAmount = 1,
        category = Category.Rune,
        target = Target.Self,
        stacking = false,
        image = "scroll_red"
    };
    
    public static Item PotionOfHealing = new Item
    {
        usageSuccess = "You drink the potion and feel your condition improves.",
        usageFail = "You drink the potion, but nothing seems to happen.",
        description = "A red potion that heals the drinker.",
        shortDescription = "Healing Potion",
        buyPrice = 75,
        sellPrice = 60,
        effectType = EffectType.Heal,
        effectAmount = 20,
        category = Category.Potion,
        target = Target.Self,
        stacking = false,
        image = "potion_fire"
    };

    public static Item ScrollOfFire = new Item
    {
        usageSuccess = "You read the scroll and flames erupt from your fingertips!",
        usageFail = "You read the scroll, but the incantation fizzles out.",
        description = "An ancient scroll inscribed with fiery runes.",
        shortDescription = "Scroll of Fire",
        buyPrice = 100,
        sellPrice = 80,
        effectType = EffectType.Damage,
        effectAmount = 50, // For example, 50 points of fire damage.
        category = Category.Scroll,
        target = Target.NPC,
        stacking = false,
        image = "scroll_red"
    };

    public static Item Antidote = new Item
    {
        usageSuccess = "You drink the antidote and feel the poison leaving your system.",
        usageFail = "You drink the antidote, but it doesn't seem necessary.",
        description = "A vial containing a remedy for most common poisons.",
        shortDescription = "Antidote",
        buyPrice = 50,
        sellPrice = 40,
        effectType = EffectType.CurePoison,
        effectAmount = 1,
        category = Category.Potion,
        target = Target.Self,
        stacking = true,
        image = "potion_green_red"
    };

    // Some invented items:

    public static Item StoneOfEvasion = new Item
    {
        usageSuccess = "You activate the stone and feel light on your feet, evading incoming attacks.",
        usageFail = "You try to activate the stone, but its magic remains dormant.",
        description = "A smooth stone imbued with the essence of wind, granting increased evasion.",
        shortDescription = "Stone of Evasion",
        buyPrice = 120,
        sellPrice = 100,
        effectType = EffectType.Bless,
        effectAmount = 1,
        category = Category.Rune,
        target = Target.Self,
        stacking = false,
        image = "scroll_red"
    };

    public static Item ElixirOfStrength = new Item
    {
        usageSuccess = "You drink the elixir and feel a surge of power coursing through your muscles.",
        usageFail = "You drink the elixir, but feel no different.",
        description = "A rare concoction said to temporarily bestow great strength upon the drinker.",
        shortDescription = "Elixir of Strength",
        buyPrice = 150,
        sellPrice = 125,
        effectType = EffectType.Bless,
        effectAmount = 1,
        category = Category.Potion,
        target = Target.Self,
        stacking = false,
        image = "potion_green_red"
    };

    public static Item GateKey = new Item
    {
        usageSuccess = "You unlock the gate",
        usageFail = "Key does not fit the lock",
        description = "A large rusty key.",
        shortDescription = "Gate key",
        buyPrice = 100,
        sellPrice = 90,
        effectType = EffectType.Open,
        effectAmount = 1,
        category = Category.Key,
        target = Target.Lock,
        stacking = false,
        image = "key_copper"
    };

    public static Item ScrollOfFireball = new Item
    {

        usageSuccess = "You unroll the scroll and a fiery ball of destruction erupts from your hand!",
        usageFail = "You attempt to use the scroll but nothing happens.",
        description = "A magical scroll containing instructions for casting the powerful fireball spell.",
        shortDescription = "Scroll of Fireball",
        buyPrice = 500,
        sellPrice = 400,
        effectType = EffectType.Damage,
        effectAmount = 1,
        category = Category.Scroll,
        target = Target.NPC,
        stacking = false,
        image = "scroll_red"

    };

    
}


public static class ItemRegistry
{
    public static Dictionary<string, Item> items = new Dictionary<string, Item>
    {
        {"Antidote", Item.Antidote},
        {"Healing Potion", Item.PotionOfHealing},
        {"Scroll of Fire", Item.ScrollOfFire},
        {"Soul Stone", Item.SoulStone},
        {"Gate key", Item.GateKey},
        {"A Sword", Item.BasicSword},
        {"An Icy Sword", Item.IceSword}
        // ... add other items here.
    };

    public static Item GetItem(string itemName)
    {
        if (items.TryGetValue(itemName, out Item item))
        {
            return item;
        }
        return null; // or handle this in some other way if the item doesn't exist.
    }
}