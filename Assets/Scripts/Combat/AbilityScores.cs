using System;

/// <summary>
/// SRD 5.1 style ability scores with modifier calculation.
/// Standard array: 15, 14, 13, 12, 10, 8 (or point buy/rolled)
/// </summary>
[Serializable]
public class AbilityScores
{
    public int Strength = 10;       // Melee attack/damage, carry capacity, athletics
    public int Dexterity = 10;      // AC bonus, ranged attack, initiative, stealth, acrobatics
    public int Constitution = 10;   // HP bonus per level, fortitude saves
    public int Intelligence = 10;   // Arcana, history, investigation, nature, religion
    public int Wisdom = 10;         // Perception, insight, medicine, survival, animal handling
    public int Charisma = 10;       // Persuasion, deception, intimidation, performance

    /// <summary>
    /// Calculate ability modifier from score.
    /// Formula: (score - 10) / 2, rounded down
    /// Score 10-11 = +0, 12-13 = +1, 8-9 = -1, etc.
    /// </summary>
    public static int GetModifier(int score)
    {
        return (score - 10) / 2;
    }

    public int StrengthMod => GetModifier(Strength);
    public int DexterityMod => GetModifier(Dexterity);
    public int ConstitutionMod => GetModifier(Constitution);
    public int IntelligenceMod => GetModifier(Intelligence);
    public int WisdomMod => GetModifier(Wisdom);
    public int CharismaMod => GetModifier(Charisma);

    /// <summary>
    /// Get modifier for a named ability.
    /// </summary>
    public int GetModifierByName(string abilityName)
    {
        switch (abilityName.ToLower())
        {
            case "strength":
            case "str":
                return StrengthMod;
            case "dexterity":
            case "dex":
                return DexterityMod;
            case "constitution":
            case "con":
                return ConstitutionMod;
            case "intelligence":
            case "int":
                return IntelligenceMod;
            case "wisdom":
            case "wis":
                return WisdomMod;
            case "charisma":
            case "cha":
                return CharismaMod;
            default:
                return 0;
        }
    }

    /// <summary>
    /// Get score for a named ability.
    /// </summary>
    public int GetScoreByName(string abilityName)
    {
        switch (abilityName.ToLower())
        {
            case "strength":
            case "str":
                return Strength;
            case "dexterity":
            case "dex":
                return Dexterity;
            case "constitution":
            case "con":
                return Constitution;
            case "intelligence":
            case "int":
                return Intelligence;
            case "wisdom":
            case "wis":
                return Wisdom;
            case "charisma":
            case "cha":
                return Charisma;
            default:
                return 10;
        }
    }

    /// <summary>
    /// Set score for a named ability.
    /// </summary>
    public void SetScoreByName(string abilityName, int value)
    {
        switch (abilityName.ToLower())
        {
            case "strength":
            case "str":
                Strength = value;
                break;
            case "dexterity":
            case "dex":
                Dexterity = value;
                break;
            case "constitution":
            case "con":
                Constitution = value;
                break;
            case "intelligence":
            case "int":
                Intelligence = value;
                break;
            case "wisdom":
            case "wis":
                Wisdom = value;
                break;
            case "charisma":
            case "cha":
                Charisma = value;
                break;
        }
    }

    /// <summary>
    /// Create default ability scores (all 10s).
    /// </summary>
    public static AbilityScores CreateDefault()
    {
        return new AbilityScores();
    }

    /// <summary>
    /// Create ability scores using standard array (15, 14, 13, 12, 10, 8).
    /// Values assigned in order: STR, DEX, CON, INT, WIS, CHA
    /// </summary>
    public static AbilityScores CreateStandardArray()
    {
        return new AbilityScores
        {
            Strength = 15,
            Dexterity = 14,
            Constitution = 13,
            Intelligence = 12,
            Wisdom = 10,
            Charisma = 8
        };
    }

    /// <summary>
    /// Create ability scores with custom values.
    /// </summary>
    public static AbilityScores Create(int str, int dex, int con, int intel, int wis, int cha)
    {
        return new AbilityScores
        {
            Strength = str,
            Dexterity = dex,
            Constitution = con,
            Intelligence = intel,
            Wisdom = wis,
            Charisma = cha
        };
    }
}
