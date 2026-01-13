using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data for a single map view (typically one per region).
/// Stores pin positions and path connections for rendering.
/// </summary>
[Serializable]
public class MapData
{
    public string mapId;
    public string regionId;             // Reference to RegionData
    public string mapImagePath;         // Path to map background image
    public Vector2 mapSize;             // Image dimensions in pixels

    public List<MapPinData> pins = new List<MapPinData>();
    public List<MapPathData> paths = new List<MapPathData>();

    /// <summary>
    /// Get pin data by location ID.
    /// </summary>
    public MapPinData GetPin(string locationId)
    {
        return pins.Find(p => p.locationId == locationId);
    }

    /// <summary>
    /// Get all paths connected to a location.
    /// </summary>
    public List<MapPathData> GetPathsForLocation(string locationId)
    {
        return paths.FindAll(p => p.fromLocationId == locationId || p.toLocationId == locationId);
    }

    /// <summary>
    /// Check if two locations are connected.
    /// </summary>
    public bool AreLocationsConnected(string loc1, string loc2)
    {
        return paths.Exists(p =>
            (p.fromLocationId == loc1 && p.toLocationId == loc2) ||
            (p.fromLocationId == loc2 && p.toLocationId == loc1));
    }
}

/// <summary>
/// A single pin/marker on the map representing a location.
/// </summary>
[Serializable]
public class MapPinData
{
    public string locationId;           // Reference to LocationData
    public string displayName;          // Name shown on hover/click
    public Vector2 position;            // Pixel position on map image

    // Visual customization
    public string iconSpritePath;       // Custom pin icon (optional)
    public string iconColor;            // Hex color (e.g., "#FFCC00")
    public float iconScale = 1f;        // Scale multiplier

    // Visibility conditions (duplicated from LocationData for quick access)
    public bool alwaysVisible;
    public string revealFlag;
    public List<string> revealQuests = new List<string>();

    // Pin state flags
    public bool isCurrentLocation;      // Highlight if player is here
    public bool isVisited;              // Has player been here
    public bool hasActiveQuest;         // Quest marker indicator

    /// <summary>
    /// Check if this pin should be visible.
    /// </summary>
    public bool ShouldBeVisible(Dictionary<string, string> playerFlags)
    {
        if (alwaysVisible) return true;

        if (!string.IsNullOrEmpty(revealFlag))
        {
            if (playerFlags.TryGetValue(revealFlag, out string value) && value == "true")
                return true;
        }

        foreach (var questId in revealQuests)
        {
            if (playerFlags.TryGetValue(questId, out string value) && value != "false")
                return true;
        }

        return false;
    }

    /// <summary>
    /// Create pin data from location data.
    /// </summary>
    public static MapPinData FromLocation(LocationData location)
    {
        return new MapPinData
        {
            locationId = location.locationId,
            displayName = location.locationName,
            position = location.mapPosition,
            iconSpritePath = location.pinIconPath,
            iconColor = location.pinColor,
            alwaysVisible = location.alwaysVisible,
            revealFlag = location.revealFlag,
            revealQuests = new List<string>(location.revealQuests)
        };
    }
}

/// <summary>
/// A path connection between two map pins.
/// Used for visual path rendering (dotted lines, roads, etc.).
/// </summary>
[Serializable]
public class MapPathData
{
    public string pathId;
    public string fromLocationId;
    public string toLocationId;

    // Path rendering
    public PathStyle style = PathStyle.Dotted;
    public string pathColor;            // Hex color
    public float pathWidth = 2f;
    public List<Vector2> waypoints = new List<Vector2>();  // Intermediate points for curved paths

    // Visibility
    public bool visibleWhenBothRevealed = true;  // Only show when both endpoints visible
    public bool visibleWhenOneRevealed = false;   // Show if at least one endpoint visible

    /// <summary>
    /// Check if this path should be visible.
    /// </summary>
    public bool ShouldBeVisible(bool fromVisible, bool toVisible)
    {
        if (visibleWhenBothRevealed)
            return fromVisible && toVisible;

        if (visibleWhenOneRevealed)
            return fromVisible || toVisible;

        return false;
    }

    /// <summary>
    /// Get all points along the path (start, waypoints, end).
    /// </summary>
    public List<Vector2> GetAllPoints(Vector2 startPos, Vector2 endPos)
    {
        var points = new List<Vector2> { startPos };
        points.AddRange(waypoints);
        points.Add(endPos);
        return points;
    }
}

/// <summary>
/// Path line styles.
/// </summary>
public enum PathStyle
{
    Solid,
    Dotted,
    Dashed,
    Hidden      // Path exists but not drawn
}

/// <summary>
/// Helper class for loading/saving map data.
/// </summary>
public static class MapDataLoader
{
    /// <summary>
    /// Load map data from Resources.
    /// Uses StoryManager if available for story-specific maps.
    /// </summary>
    public static MapData LoadMap(string mapId)
    {
        TextAsset mapAsset = null;

        // Try story-specific path first (if StoryManager exists)
        if (StoryManager.Instance != null)
        {
            mapAsset = StoryManager.Instance.LoadMapData(mapId);
        }

        // Fall back to legacy path
        if (mapAsset == null)
        {
            mapAsset = Resources.Load<TextAsset>($"Maps/{mapId}");
        }

        if (mapAsset == null)
        {
            Debug.LogError($"Map not found: Maps/{mapId}");
            return null;
        }

        return JsonUtility.FromJson<MapData>(mapAsset.text);
    }

    /// <summary>
    /// Load map data from a story-specific path.
    /// </summary>
    public static MapData LoadMapFromStory(string storyPath, string mapId)
    {
        TextAsset mapAsset = Resources.Load<TextAsset>($"Stories/{storyPath}/Maps/{mapId}");
        if (mapAsset == null)
        {
            // Fallback to global maps
            return LoadMap(mapId);
        }

        return JsonUtility.FromJson<MapData>(mapAsset.text);
    }

    /// <summary>
    /// Save map data to JSON string (for editor export).
    /// </summary>
    public static string SaveToJson(MapData mapData)
    {
        return JsonUtility.ToJson(mapData, true);
    }
}
