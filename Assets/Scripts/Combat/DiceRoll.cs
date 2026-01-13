using System;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Represents a dice roll in standard notation (e.g., "2d6+3", "1d20", "3d8-2").
/// Handles parsing, rolling, and result tracking.
/// </summary>
[Serializable]
public class DiceRoll
{
    public int numberOfDice = 1;
    public int dieType = 6;      // d4, d6, d8, d10, d12, d20
    public int modifier = 0;

    // Regex pattern for dice notation: XdY+Z or XdY-Z or XdY
    private static readonly Regex DicePattern = new Regex(@"^(\d+)?d(\d+)([+-]\d+)?$", RegexOptions.IgnoreCase);

    public DiceRoll() { }

    public DiceRoll(int numDice, int dieType, int mod = 0)
    {
        this.numberOfDice = numDice;
        this.dieType = dieType;
        this.modifier = mod;
    }

    /// <summary>
    /// Parse dice notation string (e.g., "2d6+3", "1d20", "d8").
    /// </summary>
    public static DiceRoll Parse(string notation)
    {
        if (string.IsNullOrEmpty(notation))
        {
            Debug.LogWarning("DiceRoll.Parse: Empty notation, defaulting to 1d6");
            return new DiceRoll(1, 6, 0);
        }

        notation = notation.Trim().ToLower();
        Match match = DicePattern.Match(notation);

        if (!match.Success)
        {
            Debug.LogWarning($"DiceRoll.Parse: Invalid notation '{notation}', defaulting to 1d6");
            return new DiceRoll(1, 6, 0);
        }

        int numDice = 1;
        if (!string.IsNullOrEmpty(match.Groups[1].Value))
        {
            numDice = int.Parse(match.Groups[1].Value);
        }

        int dieType = int.Parse(match.Groups[2].Value);

        int mod = 0;
        if (!string.IsNullOrEmpty(match.Groups[3].Value))
        {
            mod = int.Parse(match.Groups[3].Value);
        }

        return new DiceRoll(numDice, dieType, mod);
    }

    /// <summary>
    /// Roll the dice and return total result.
    /// </summary>
    public int Roll()
    {
        int total = modifier;
        for (int i = 0; i < numberOfDice; i++)
        {
            total += UnityEngine.Random.Range(1, dieType + 1);
        }
        return total;
    }

    /// <summary>
    /// Roll the dice and return detailed results.
    /// </summary>
    public RollResult RollWithDetails()
    {
        RollResult result = new RollResult
        {
            notation = ToString(),
            individualRolls = new int[numberOfDice],
            modifier = this.modifier
        };

        int diceTotal = 0;
        for (int i = 0; i < numberOfDice; i++)
        {
            int roll = UnityEngine.Random.Range(1, dieType + 1);
            result.individualRolls[i] = roll;
            diceTotal += roll;
        }

        result.diceTotal = diceTotal;
        result.total = diceTotal + modifier;

        // Check for critical (only relevant for d20)
        if (dieType == 20 && numberOfDice == 1)
        {
            result.isNatural20 = result.individualRolls[0] == 20;
            result.isNatural1 = result.individualRolls[0] == 1;
        }

        return result;
    }

    /// <summary>
    /// Roll with advantage (roll twice, take higher).
    /// Only applies to single d20 rolls in the SRD sense.
    /// </summary>
    public RollResult RollWithAdvantage()
    {
        RollResult roll1 = RollWithDetails();
        RollResult roll2 = RollWithDetails();

        RollResult result = roll1.total >= roll2.total ? roll1 : roll2;
        result.hadAdvantage = true;
        result.discardedRoll = roll1.total >= roll2.total ? roll2.diceTotal : roll1.diceTotal;

        return result;
    }

    /// <summary>
    /// Roll with disadvantage (roll twice, take lower).
    /// </summary>
    public RollResult RollWithDisadvantage()
    {
        RollResult roll1 = RollWithDetails();
        RollResult roll2 = RollWithDetails();

        RollResult result = roll1.total <= roll2.total ? roll1 : roll2;
        result.hadDisadvantage = true;
        result.discardedRoll = roll1.total <= roll2.total ? roll2.diceTotal : roll1.diceTotal;

        return result;
    }

    /// <summary>
    /// Roll for critical hit damage (double the dice).
    /// </summary>
    public int RollCritical()
    {
        int total = modifier;
        // Double the number of dice for critical
        for (int i = 0; i < numberOfDice * 2; i++)
        {
            total += UnityEngine.Random.Range(1, dieType + 1);
        }
        return total;
    }

    /// <summary>
    /// Get the average result for this roll.
    /// </summary>
    public float GetAverage()
    {
        float avgPerDie = (1 + dieType) / 2f;
        return (numberOfDice * avgPerDie) + modifier;
    }

    /// <summary>
    /// Get the minimum possible result.
    /// </summary>
    public int GetMinimum()
    {
        return numberOfDice + modifier;
    }

    /// <summary>
    /// Get the maximum possible result.
    /// </summary>
    public int GetMaximum()
    {
        return (numberOfDice * dieType) + modifier;
    }

    public override string ToString()
    {
        string result = $"{numberOfDice}d{dieType}";
        if (modifier > 0)
            result += $"+{modifier}";
        else if (modifier < 0)
            result += modifier.ToString();
        return result;
    }

    // Static helpers for common rolls

    public static int RollD20()
    {
        return UnityEngine.Random.Range(1, 21);
    }

    public static RollResult RollD20WithDetails()
    {
        return new DiceRoll(1, 20, 0).RollWithDetails();
    }

    public static int RollD6()
    {
        return UnityEngine.Random.Range(1, 7);
    }

    public static int Roll4D6DropLowest()
    {
        int[] rolls = new int[4];
        int min = int.MaxValue;
        int total = 0;

        for (int i = 0; i < 4; i++)
        {
            rolls[i] = RollD6();
            total += rolls[i];
            if (rolls[i] < min) min = rolls[i];
        }

        return total - min;
    }
}

/// <summary>
/// Detailed result of a dice roll.
/// </summary>
[Serializable]
public class RollResult
{
    public string notation;
    public int[] individualRolls;
    public int diceTotal;
    public int modifier;
    public int total;

    public bool isNatural20;
    public bool isNatural1;

    public bool hadAdvantage;
    public bool hadDisadvantage;
    public int discardedRoll;

    /// <summary>
    /// Format result for display in combat log.
    /// Example: "1d20+5 = [17] + 5 = 22"
    /// </summary>
    public string ToDisplayString()
    {
        string rollsStr = string.Join(", ", individualRolls);
        string result = $"{notation} = [{rollsStr}]";

        if (modifier != 0)
        {
            result += modifier > 0 ? $" + {modifier}" : $" - {Math.Abs(modifier)}";
        }

        result += $" = {total}";

        if (isNatural20) result += " (CRITICAL!)";
        if (isNatural1) result += " (Critical Fail!)";
        if (hadAdvantage) result += $" (Advantage, dropped {discardedRoll})";
        if (hadDisadvantage) result += $" (Disadvantage, dropped {discardedRoll})";

        return result;
    }
}
