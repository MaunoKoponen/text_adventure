using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Configuration data for a story/world.
/// Loaded from config.json in the story folder.
/// </summary>
[Serializable]
public class StoryConfig
{
    public string storyId;
    public string storyName;
    public string storyDescription;
    public string author;
    public string version;

    // Starting conditions
    public string startingRoom;
    public int startingGold = 100;
    public int startingHealth = 100;
    public List<string> startingItems = new List<string>();
    public List<StartingFlag> startingFlags = new List<StartingFlag>();

    // Whether to use enhanced combat system
    public bool useEnhancedCombat = false;

    // Starting ability scores (if using enhanced combat)
    public StartingAbilityScores startingAbilityScores;

    // Map configuration
    public string defaultMapId;
    public string defaultRegionId;

    // Story-specific settings
    public bool allowFastTravel = true;
    public bool showMinimap = true;
    public bool enableDebugCommands = false;

    /// <summary>
    /// Create a default dev world configuration.
    /// </summary>
    public static StoryConfig CreateDevWorld()
    {
        return new StoryConfig
        {
            storyId = "dev_world",
            storyName = "Development Test World",
            storyDescription = "A test world for developing and testing game systems.",
            author = "Developer",
            version = "1.0",
            startingRoom = "dev_hub",
            startingGold = 1000,
            startingHealth = 100,
            useEnhancedCombat = true,
            startingAbilityScores = StartingAbilityScores.CreateBalanced(),
            defaultMapId = "dev_region",
            defaultRegionId = "dev_region",
            enableDebugCommands = true,
            startingItems = new List<string> { "health_potion", "sword" }
        };
    }
}

/// <summary>
/// A flag to set at game start.
/// </summary>
[Serializable]
public class StartingFlag
{
    public string flagName;
    public string flagValue;
}

/// <summary>
/// Starting ability scores for enhanced combat.
/// </summary>
[Serializable]
public class StartingAbilityScores
{
    public int strength = 10;
    public int dexterity = 10;
    public int constitution = 10;
    public int intelligence = 10;
    public int wisdom = 10;
    public int charisma = 10;

    /// <summary>
    /// Create balanced starting scores (all 10s).
    /// </summary>
    public static StartingAbilityScores CreateBalanced()
    {
        return new StartingAbilityScores();
    }

    /// <summary>
    /// Create fighter-style scores.
    /// </summary>
    public static StartingAbilityScores CreateFighter()
    {
        return new StartingAbilityScores
        {
            strength = 15,
            dexterity = 13,
            constitution = 14,
            intelligence = 8,
            wisdom = 10,
            charisma = 10
        };
    }

    /// <summary>
    /// Create rogue-style scores.
    /// </summary>
    public static StartingAbilityScores CreateRogue()
    {
        return new StartingAbilityScores
        {
            strength = 10,
            dexterity = 15,
            constitution = 12,
            intelligence = 13,
            wisdom = 10,
            charisma = 14
        };
    }

    /// <summary>
    /// Create mage-style scores.
    /// </summary>
    public static StartingAbilityScores CreateMage()
    {
        return new StartingAbilityScores
        {
            strength = 8,
            dexterity = 12,
            constitution = 12,
            intelligence = 15,
            wisdom = 14,
            charisma = 10
        };
    }

    /// <summary>
    /// Convert to AbilityScores.
    /// </summary>
    public AbilityScores ToAbilityScores()
    {
        return AbilityScores.Create(strength, dexterity, constitution, intelligence, wisdom, charisma);
    }
}
