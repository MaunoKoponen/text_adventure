using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WorldGen;

/// <summary>
/// Main map system controller.
/// Handles loading map data, spawning pins, drawing paths, and handling interactions.
/// </summary>
public class MapSystem : MonoBehaviour
{
    public static MapSystem Instance { get; private set; }

    [Header("Map Display")]
    public Image mapImage;
    public RectTransform pinContainer;
    public RectTransform pathContainer;

    [Header("Prefabs")]
    public GameObject mapPinPrefab;
    public GameObject pathLinePrefab;

    [Header("Configuration")]
    public float pinScale = 1f;
    public Color defaultPinColor = Color.white;
    public Color currentLocationColor = Color.yellow;
    public Color visitedPinColor = new Color(0.7f, 0.7f, 0.7f);
    public Color questPinColor = Color.yellow;

    [Header("Path Settings")]
    public Color defaultPathColor = new Color(0.6f, 0.5f, 0.3f);
    public float pathDotSpacing = 10f;

    // Runtime state
    private MapData currentMapData;
    private Dictionary<string, MapPinUI> activePins = new Dictionary<string, MapPinUI>();
    private List<GameObject> activePathObjects = new List<GameObject>();
    private string currentLocationId;

    // Events
    public event Action<string> OnLocationSelected;
    public event Action<string> OnTravelRequested;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Subscribe to chapter unlock events
        if (ChapterManager.Instance != null)
        {
            ChapterManager.Instance.OnChapterUnlocked += HandleChapterUnlocked;
        }
    }

    private void OnDestroy()
    {
        if (ChapterManager.Instance != null)
        {
            ChapterManager.Instance.OnChapterUnlocked -= HandleChapterUnlocked;
        }
    }

    /// <summary>
    /// Handle chapter unlock - refresh map visibility.
    /// </summary>
    private void HandleChapterUnlocked(ChapterData chapter)
    {
        Debug.Log($"[MapSystem] Chapter unlocked: {chapter.chapterName}, refreshing visibility");
        RefreshVisibility();
    }

    /// <summary>
    /// Load and display a map.
    /// </summary>
    public void LoadMap(string mapId)
    {
        // Clear existing content
        ClearMap();

        // Load map data
        currentMapData = MapDataLoader.LoadMap(mapId);
        if (currentMapData == null)
        {
            Debug.LogError($"Failed to load map: {mapId}");
            return;
        }

        // Set map image
        if (!string.IsNullOrEmpty(currentMapData.mapImagePath))
        {
            Sprite mapSprite = Resources.Load<Sprite>(currentMapData.mapImagePath);
            if (mapSprite != null)
            {
                mapImage.sprite = mapSprite;
            }
        }

        // Spawn pins
        foreach (var pinData in currentMapData.pins)
        {
            SpawnPin(pinData);
        }

        // Draw paths
        foreach (var pathData in currentMapData.paths)
        {
            DrawPath(pathData);
        }

        // Update visibility based on player state
        RefreshVisibility();
    }

    /// <summary>
    /// Load map from story-specific path.
    /// </summary>
    public void LoadMapFromStory(string storyPath, string mapId)
    {
        ClearMap();

        currentMapData = MapDataLoader.LoadMapFromStory(storyPath, mapId);
        if (currentMapData == null)
        {
            Debug.LogError($"Failed to load map: {storyPath}/Maps/{mapId}");
            return;
        }

        // Same setup as LoadMap...
        if (!string.IsNullOrEmpty(currentMapData.mapImagePath))
        {
            Sprite mapSprite = Resources.Load<Sprite>($"Stories/{storyPath}/{currentMapData.mapImagePath}");
            if (mapSprite == null)
            {
                mapSprite = Resources.Load<Sprite>(currentMapData.mapImagePath);
            }
            if (mapSprite != null)
            {
                mapImage.sprite = mapSprite;
            }
        }

        foreach (var pinData in currentMapData.pins)
        {
            SpawnPin(pinData);
        }

        foreach (var pathData in currentMapData.paths)
        {
            DrawPath(pathData);
        }

        RefreshVisibility();
    }

    /// <summary>
    /// Clear all map content.
    /// </summary>
    public void ClearMap()
    {
        foreach (var pin in activePins.Values)
        {
            if (pin != null)
                Destroy(pin.gameObject);
        }
        activePins.Clear();

        foreach (var pathObj in activePathObjects)
        {
            if (pathObj != null)
                Destroy(pathObj);
        }
        activePathObjects.Clear();

        currentMapData = null;
    }

    /// <summary>
    /// Spawn a pin on the map.
    /// </summary>
    private void SpawnPin(MapPinData pinData)
    {
        if (mapPinPrefab == null)
        {
            Debug.LogWarning("Map pin prefab not assigned!");
            return;
        }

        GameObject pinObj = Instantiate(mapPinPrefab, pinContainer);
        MapPinUI pinUI = pinObj.GetComponent<MapPinUI>();

        if (pinUI == null)
        {
            pinUI = pinObj.AddComponent<MapPinUI>();
        }

        // Position the pin
        RectTransform pinRect = pinObj.GetComponent<RectTransform>();
        pinRect.anchoredPosition = pinData.position;
        pinRect.localScale = Vector3.one * pinData.iconScale * pinScale;

        // Setup the pin
        pinUI.Setup(pinData, OnPinClicked);

        activePins[pinData.locationId] = pinUI;
    }

    /// <summary>
    /// Draw a path between two locations.
    /// </summary>
    private void DrawPath(MapPathData pathData)
    {
        MapPinData fromPin = currentMapData.GetPin(pathData.fromLocationId);
        MapPinData toPin = currentMapData.GetPin(pathData.toLocationId);

        if (fromPin == null || toPin == null)
        {
            Debug.LogWarning($"Path references missing pins: {pathData.fromLocationId} -> {pathData.toLocationId}");
            return;
        }

        // Get all points along the path
        List<Vector2> points = pathData.GetAllPoints(fromPin.position, toPin.position);

        // Create path visuals
        if (pathData.style == PathStyle.Dotted)
        {
            CreateDottedPath(points, pathData);
        }
        else if (pathData.style == PathStyle.Solid || pathData.style == PathStyle.Dashed)
        {
            CreateLinePath(points, pathData);
        }
    }

    /// <summary>
    /// Create a dotted path using individual dot images.
    /// </summary>
    private void CreateDottedPath(List<Vector2> points, MapPathData pathData)
    {
        Color pathColor = defaultPathColor;
        if (!string.IsNullOrEmpty(pathData.pathColor))
        {
            ColorUtility.TryParseHtmlString(pathData.pathColor, out pathColor);
        }

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 start = points[i];
            Vector2 end = points[i + 1];
            float distance = Vector2.Distance(start, end);
            int numDots = Mathf.Max(2, Mathf.FloorToInt(distance / pathDotSpacing));

            for (int j = 0; j <= numDots; j++)
            {
                float t = (float)j / numDots;
                Vector2 pos = Vector2.Lerp(start, end, t);

                GameObject dot = new GameObject("PathDot");
                dot.transform.SetParent(pathContainer);

                RectTransform dotRect = dot.AddComponent<RectTransform>();
                dotRect.anchoredPosition = pos;
                dotRect.sizeDelta = new Vector2(pathData.pathWidth * 2, pathData.pathWidth * 2);

                Image dotImage = dot.AddComponent<Image>();
                dotImage.color = pathColor;

                // Store reference for cleanup
                activePathObjects.Add(dot);
            }
        }
    }

    /// <summary>
    /// Create a solid/dashed line path.
    /// </summary>
    private void CreateLinePath(List<Vector2> points, MapPathData pathData)
    {
        // For solid lines, we'd use a LineRenderer or UI.Extensions
        // Simplified: create stretched images between points
        Color pathColor = defaultPathColor;
        if (!string.IsNullOrEmpty(pathData.pathColor))
        {
            ColorUtility.TryParseHtmlString(pathData.pathColor, out pathColor);
        }

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 start = points[i];
            Vector2 end = points[i + 1];

            GameObject line = new GameObject("PathLine");
            line.transform.SetParent(pathContainer);

            RectTransform lineRect = line.AddComponent<RectTransform>();

            // Position at midpoint
            lineRect.anchoredPosition = (start + end) / 2f;

            // Size to cover distance
            float distance = Vector2.Distance(start, end);
            lineRect.sizeDelta = new Vector2(distance, pathData.pathWidth);

            // Rotate to face end point
            float angle = Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg;
            lineRect.localRotation = Quaternion.Euler(0, 0, angle);

            Image lineImage = line.AddComponent<Image>();
            lineImage.color = pathColor;

            activePathObjects.Add(line);
        }
    }

    /// <summary>
    /// Refresh pin visibility based on current player state and chapter unlocks.
    /// </summary>
    public void RefreshVisibility()
    {
        var playerFlags = RoomManager.playerData?.Flags ?? new Dictionary<string, string>();

        foreach (var kvp in activePins)
        {
            MapPinUI pinUI = kvp.Value;
            if (pinUI == null) continue;

            // Check base visibility from pin data
            bool isVisible = pinUI.PinData.ShouldBeVisible(playerFlags);

            // Also check chapter-based visibility if ChapterManager exists
            if (isVisible && ChapterManager.Instance != null)
            {
                isVisible = IsLocationVisibleByChapter(kvp.Key);
            }

            pinUI.SetRevealed(isVisible);

            // Highlight current location
            bool isCurrent = kvp.Key == currentLocationId;
            pinUI.SetAsCurrent(isCurrent);
        }

        // Update path visibility
        RefreshPathVisibility(playerFlags);
    }

    /// <summary>
    /// Check if a location should be visible based on chapter unlock status.
    /// </summary>
    private bool IsLocationVisibleByChapter(string locationId)
    {
        if (ChapterManager.Instance == null) return true;

        // Check if location is in any unlocked chapter
        return ChapterManager.Instance.IsLocationInUnlockedChapter(locationId);
    }

    /// <summary>
    /// Refresh path visibility.
    /// </summary>
    private void RefreshPathVisibility(Dictionary<string, string> playerFlags)
    {
        // For now, paths use same visibility as their endpoints
        // Could be enhanced to fade/hide paths based on reveal state
    }

    /// <summary>
    /// Set the current location (highlighted on map).
    /// </summary>
    public void SetCurrentLocation(string locationId)
    {
        currentLocationId = locationId;

        foreach (var kvp in activePins)
        {
            kvp.Value.SetAsCurrent(kvp.Key == locationId);
        }
    }

    /// <summary>
    /// Mark a location as visited.
    /// </summary>
    public void MarkLocationVisited(string locationId)
    {
        if (activePins.TryGetValue(locationId, out MapPinUI pin))
        {
            pin.SetVisited(true);
        }
    }

    /// <summary>
    /// Show quest indicator on a location.
    /// </summary>
    public void SetQuestIndicator(string locationId, bool hasQuest)
    {
        if (activePins.TryGetValue(locationId, out MapPinUI pin))
        {
            pin.SetQuestIndicator(hasQuest);
        }
    }

    /// <summary>
    /// Handle pin click.
    /// </summary>
    private void OnPinClicked(MapPinData pinData)
    {
        OnLocationSelected?.Invoke(pinData.locationId);

        // Could show location info popup or trigger travel
        Debug.Log($"Map pin clicked: {pinData.displayName} ({pinData.locationId})");
    }

    /// <summary>
    /// Request travel to a location.
    /// </summary>
    public void TravelTo(string locationId)
    {
        OnTravelRequested?.Invoke(locationId);
    }

    /// <summary>
    /// Get all visible pins.
    /// </summary>
    public List<MapPinUI> GetVisiblePins()
    {
        var visible = new List<MapPinUI>();
        foreach (var pin in activePins.Values)
        {
            if (pin.IsRevealed)
                visible.Add(pin);
        }
        return visible;
    }
}
