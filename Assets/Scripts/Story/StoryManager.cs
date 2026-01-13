using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central manager for story/world loading and resource paths.
/// Handles folder-based story swapping with fallback to legacy paths.
/// </summary>
public class StoryManager : MonoBehaviour
{
    public static StoryManager Instance { get; private set; }

    // Current story configuration
    public StoryConfig CurrentStory { get; private set; }
    public string CurrentStoryId => CurrentStory?.storyId ?? "";
    public bool IsStoryLoaded => CurrentStory != null;

    // Events
    public event Action<StoryConfig> OnStoryLoaded;
    public event Action OnStoryUnloaded;

    // Cache for loaded resources
    private Dictionary<string, TextAsset> roomCache = new Dictionary<string, TextAsset>();
    private Dictionary<string, TextAsset> questCache = new Dictionary<string, TextAsset>();
    private Dictionary<string, TextAsset> mapCache = new Dictionary<string, TextAsset>();
    private Dictionary<string, TextAsset> enemyCache = new Dictionary<string, TextAsset>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Load a story from Resources/Stories/{storyId}/config.json
    /// </summary>
    public bool LoadStory(string storyId)
    {
        ClearCaches();

        string configPath = $"Stories/{storyId}/config";
        TextAsset configAsset = Resources.Load<TextAsset>(configPath);

        if (configAsset == null)
        {
            Debug.LogError($"Story config not found: {configPath}");
            return false;
        }

        try
        {
            CurrentStory = JsonUtility.FromJson<StoryConfig>(configAsset.text);
            CurrentStory.storyId = storyId; // Ensure ID matches folder name

            Debug.Log($"Story loaded: {CurrentStory.storyName} (v{CurrentStory.version})");
            OnStoryLoaded?.Invoke(CurrentStory);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to parse story config: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Unload the current story and clear caches.
    /// </summary>
    public void UnloadStory()
    {
        CurrentStory = null;
        ClearCaches();
        OnStoryUnloaded?.Invoke();
        Debug.Log("Story unloaded");
    }

    /// <summary>
    /// Get the resource path for story-specific content.
    /// </summary>
    private string GetStoryPath(string subfolder, string resourceId)
    {
        if (CurrentStory == null)
            return null;

        return $"Stories/{CurrentStory.storyId}/{subfolder}/{resourceId}";
    }

    /// <summary>
    /// Load room data, checking story folder first then falling back to legacy.
    /// </summary>
    public TextAsset LoadRoomData(string roomId)
    {
        // Check cache first
        if (roomCache.TryGetValue(roomId, out TextAsset cached))
        {
            return cached;
        }

        TextAsset asset = null;

        // Try story-specific path first
        if (CurrentStory != null)
        {
            string storyPath = GetStoryPath("Rooms", roomId);
            asset = Resources.Load<TextAsset>(storyPath);
        }

        // Fall back to legacy path
        if (asset == null)
        {
            asset = Resources.Load<TextAsset>($"Rooms/{roomId}");
        }

        // Cache the result (even if null to avoid repeated lookups)
        roomCache[roomId] = asset;
        return asset;
    }

    /// <summary>
    /// Load quest data, checking story folder first then falling back to legacy.
    /// </summary>
    public TextAsset LoadQuestData(string questId)
    {
        // Check cache first
        if (questCache.TryGetValue(questId, out TextAsset cached))
        {
            return cached;
        }

        TextAsset asset = null;

        // Try story-specific path first
        if (CurrentStory != null)
        {
            string storyPath = GetStoryPath("Quests", questId);
            asset = Resources.Load<TextAsset>(storyPath);
        }

        // Fall back to legacy path
        if (asset == null)
        {
            asset = Resources.Load<TextAsset>($"Quests/{questId}");
        }

        questCache[questId] = asset;
        return asset;
    }

    /// <summary>
    /// Load map data, checking story folder first then falling back to legacy.
    /// </summary>
    public TextAsset LoadMapData(string mapId)
    {
        // Check cache first
        if (mapCache.TryGetValue(mapId, out TextAsset cached))
        {
            return cached;
        }

        TextAsset asset = null;

        // Try story-specific path first
        if (CurrentStory != null)
        {
            string storyPath = GetStoryPath("Maps", mapId);
            asset = Resources.Load<TextAsset>(storyPath);
        }

        // Fall back to legacy path
        if (asset == null)
        {
            asset = Resources.Load<TextAsset>($"Maps/{mapId}");
        }

        mapCache[mapId] = asset;
        return asset;
    }

    /// <summary>
    /// Load enemy data, checking story folder first then falling back to legacy.
    /// </summary>
    public TextAsset LoadEnemyData(string enemyId)
    {
        // Check cache first
        if (enemyCache.TryGetValue(enemyId, out TextAsset cached))
        {
            return cached;
        }

        TextAsset asset = null;

        // Try story-specific path first
        if (CurrentStory != null)
        {
            string storyPath = GetStoryPath("Enemies", enemyId);
            asset = Resources.Load<TextAsset>(storyPath);
        }

        // Fall back to legacy path
        if (asset == null)
        {
            asset = Resources.Load<TextAsset>($"Enemies/{enemyId}");
        }

        enemyCache[enemyId] = asset;
        return asset;
    }

    /// <summary>
    /// Load a story-specific sprite/image.
    /// </summary>
    public Sprite LoadStorySprite(string spritePath)
    {
        Sprite sprite = null;

        // Try story-specific path first
        if (CurrentStory != null)
        {
            string storyPath = $"Stories/{CurrentStory.storyId}/Images/{spritePath}";
            sprite = Resources.Load<Sprite>(storyPath);
        }

        // Fall back to common Images folder
        if (sprite == null)
        {
            sprite = Resources.Load<Sprite>($"Images/{spritePath}");
        }

        return sprite;
    }

    /// <summary>
    /// Get all available stories.
    /// </summary>
    public List<StoryConfig> GetAvailableStories()
    {
        var stories = new List<StoryConfig>();

        // Load all config files from Stories folder
        var configFiles = Resources.LoadAll<TextAsset>("Stories");

        foreach (var folder in GetStoryFolders())
        {
            var configAsset = Resources.Load<TextAsset>($"Stories/{folder}/config");
            if (configAsset != null)
            {
                try
                {
                    var config = JsonUtility.FromJson<StoryConfig>(configAsset.text);
                    config.storyId = folder;
                    stories.Add(config);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to parse story config for {folder}: {e.Message}");
                }
            }
        }

        return stories;
    }

    /// <summary>
    /// Get story folder names (this is a workaround since Resources doesn't list folders).
    /// </summary>
    private List<string> GetStoryFolders()
    {
        var folders = new List<string>();

        // Try to load known story configs
        // In practice, you might want to maintain a manifest file
        string[] knownStories = { "dev_world", "default_story" };

        foreach (var story in knownStories)
        {
            var config = Resources.Load<TextAsset>($"Stories/{story}/config");
            if (config != null)
            {
                folders.Add(story);
            }
        }

        return folders;
    }

    /// <summary>
    /// Apply story starting conditions to player data.
    /// </summary>
    public void ApplyStartingConditions(PlayerData playerData)
    {
        if (CurrentStory == null || playerData == null)
        {
            Debug.LogWarning("Cannot apply starting conditions: no story or player data");
            return;
        }

        // Set starting gold
        playerData.coins = CurrentStory.startingGold;

        // Set starting health
        playerData.health = CurrentStory.startingHealth;

        // Add starting items
        foreach (var itemId in CurrentStory.startingItems)
        {
            playerData.AddItem(itemId);
        }

        // Set starting flags
        foreach (var flag in CurrentStory.startingFlags)
        {
            playerData.SetFlag(flag.flagName, flag.flagValue);
        }

        // Initialize enhanced stats if story uses them
        if (CurrentStory.useEnhancedCombat)
        {
            playerData.InitializeEnhancedStats();

            if (CurrentStory.startingAbilityScores != null && playerData.stats != null)
            {
                playerData.stats.abilities = CurrentStory.startingAbilityScores.ToAbilityScores();
                // Recalculate max HP with new CON modifier
                playerData.stats.maxHitPoints = 10 + playerData.stats.abilities.ConstitutionMod;
                playerData.stats.currentHitPoints = playerData.stats.maxHitPoints;
            }
        }

        Debug.Log($"Applied starting conditions for story: {CurrentStory.storyName}");
    }

    /// <summary>
    /// Clear all resource caches.
    /// </summary>
    public void ClearCaches()
    {
        roomCache.Clear();
        questCache.Clear();
        mapCache.Clear();
        enemyCache.Clear();
    }

    /// <summary>
    /// Get the starting room for the current story.
    /// </summary>
    public string GetStartingRoom()
    {
        return CurrentStory?.startingRoom ?? "start";
    }
}
