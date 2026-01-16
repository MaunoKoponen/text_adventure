using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// World data containing all regions (top level of hierarchy).
/// </summary>
[Serializable]
public class WorldData
{
    public string worldId;
    public string worldName;
    public string description;
    public List<RegionData> regions = new List<RegionData>();

    public RegionData GetRegion(string regionId)
    {
        return regions.Find(r => r.regionId == regionId);
    }
}

/// <summary>
/// Region containing multiple areas (e.g., "Northern Lands", "Desert Wastes").
/// </summary>
[Serializable]
public class RegionData
{
    public string regionId;
    public string regionName;
    public string description;
    public string mapImagePath;         // Path to regional map PNG
    public List<AreaData> areas = new List<AreaData>();

    public AreaData GetArea(string areaId)
    {
        return areas.Find(a => a.areaId == areaId);
    }

    public LocationData GetLocation(string locationId)
    {
        foreach (var area in areas)
        {
            var location = area.GetLocation(locationId);
            if (location != null) return location;
        }
        return null;
    }
}

/// <summary>
/// Area containing multiple locations (e.g., "Thornwood Forest", "Merchant District").
/// </summary>
[Serializable]
public class AreaData
{
    public string areaId;
    public string areaName;
    public string description;
    public List<LocationData> locations = new List<LocationData>();

    public LocationData GetLocation(string locationId)
    {
        return locations.Find(l => l.locationId == locationId);
    }
}

/// <summary>
/// A specific location on the map (e.g., "Town Square", "Old Mill").
/// Maps to one or more rooms.
/// </summary>
[Serializable]
public class LocationData
{
    public string locationId;
    public string locationName;
    public string description;

    // Chapter association (for world generation)
    public int chapterNumber = 1;              // Chapter this location belongs to
    public LocationType locationType = LocationType.Exploration;  // Type of location
    public bool isChapterHub;                  // Main hub for its chapter
    public int difficulty = 1;                 // For combat encounters (1-10)

    // Generation metadata
    public bool isGenerated = false;           // True if AI-generated
    public string generationId;                // Batch ID for generated content

    // Map display properties
    public Vector2 mapPosition;         // Position on regional map (pixel coordinates)
    public string pinIconPath;          // Custom pin icon (optional, uses default if null)
    public string pinColor;             // Hex color for pin (e.g., "#FF0000")

    // Visibility and reveal conditions
    public bool isRevealed;             // Current revealed state (runtime)
    public bool alwaysVisible;          // Always shown regardless of conditions
    public string revealFlag;           // Flag that reveals this location when true
    public List<string> revealQuests = new List<string>();  // Quest IDs that reveal this

    // Connections to other locations
    public List<string> connectedLocations = new List<string>();  // IDs of connected locations

    // Room references
    public List<string> roomIds = new List<string>();  // Rooms within this location
    public string entryRoomId;          // Default entry point room

    // Local quests available at this location
    public List<string> localQuestIds = new List<string>();

    // Travel requirements
    public string travelRequiredFlag;   // Flag needed to travel here
    public string travelRequiredItem;   // Item needed to travel here

    /// <summary>
    /// Check if this location should be visible based on player flags.
    /// </summary>
    public bool ShouldBeVisible(Dictionary<string, string> playerFlags)
    {
        if (alwaysVisible) return true;

        // Check reveal flag
        if (!string.IsNullOrEmpty(revealFlag))
        {
            if (playerFlags.TryGetValue(revealFlag, out string value) && value == "true")
                return true;
        }

        // Check reveal quests
        foreach (var questId in revealQuests)
        {
            if (playerFlags.TryGetValue(questId, out string value) && value != "false")
                return true;
        }

        return isRevealed;
    }

    /// <summary>
    /// Check if player can travel to this location.
    /// </summary>
    public bool CanTravelTo(Dictionary<string, string> playerFlags, List<Item> inventory)
    {
        // Check flag requirement
        if (!string.IsNullOrEmpty(travelRequiredFlag))
        {
            if (!playerFlags.TryGetValue(travelRequiredFlag, out string value) || value != "true")
                return false;
        }

        // Check item requirement
        if (!string.IsNullOrEmpty(travelRequiredItem))
        {
            bool hasItem = inventory.Exists(i => i.shortDescription == travelRequiredItem);
            if (!hasItem) return false;
        }

        return true;
    }
}

/// <summary>
/// Type of location for gameplay purposes.
/// </summary>
public enum LocationType
{
    Hub,            // Safe area, shops, quest givers (always accessible)
    Exploration,    // Adventure area with encounters
    Dungeon,        // Multi-room challenge area
    Boss,           // Major encounter location
    Transition      // Connection between chapters
}
