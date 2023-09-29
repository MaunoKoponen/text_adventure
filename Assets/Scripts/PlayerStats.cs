[System.Serializable]
public class PlayerStats
{
    public int health = 100;
    public int fireResistance = 0;  // Value between 0 to 100
    public int coldResistance = 0;  // Value between 0 to 100
    public int poisonResistance = 0; // Value between 0 to 100

    public Weapon equippedWeapon;
}

[System.Serializable]
public class EnemyStats
{
    public string name;
    public int health;
}


[System.Serializable]
public class Item
{
    public string name;
    public ItemType type;

    public enum ItemType
    {
        Weapon,
        Potion,
        Misc
    }
}

[System.Serializable]
public class Weapon : Item
{
    public int damageAmount;
    public DamageType damageType;

    public Weapon()
    {
        type = Item.ItemType.Weapon;
    }

    public enum DamageType
    {
        Physical,
        Fire,
        Cold,
        Poison
    }
}
