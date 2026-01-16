using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WorldGen;

public class SettingsUI : MonoBehaviour
{
    public RoomManager roomManager;


    public Button newGameButton;
    public Button LoadGameButton;
    public Button ExitButton;

    [Header("Story Selection")]
    public TMP_Dropdown storyDropdown;
    public TMP_Text storyDescriptionText;

    // Available stories
    private List<StoryInfo> availableStories = new List<StoryInfo>();
    private int selectedStoryIndex = 0;

    private void Awake()
    {
        newGameButton.onClick.AddListener(NewGame);
        ExitButton.onClick.AddListener(CloseView);

        // Populate story dropdown
        PopulateStoryDropdown();

        if (storyDropdown != null)
        {
            storyDropdown.onValueChanged.AddListener(OnStorySelected);
        }
    }

    [System.Serializable]
    private class Flag
    {
        public string Key;
        public string Value;
    }

    [System.Serializable]
    private class GameParameters
    {
        public string currentRoom;
        public List<Flag> flags;
        public List<string> inventory;
    }
    
    
    public void NewGame()
    {
        // Wipe current progress
        PlayerPrefs.SetString("QuestLog", "");
        PlayerPrefs.SetString("QuestManagerData", "");
        PlayerPrefs.Save();

        if (RoomManager.Diary != null && RoomManager.Diary.questLog != null)
        {
            RoomManager.Diary.questLog.Entries.Clear();
        }

        // Reset QuestManager if it exists
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.ResetProgress();
        }

        // Check if using a story-based game or legacy
        var selectedStory = availableStories[selectedStoryIndex];

        if (!selectedStory.isLegacy && selectedStory.config != null)
        {
            // Story-based new game
            StartStoryGame(selectedStory);
        }
        else
        {
            // Legacy new game
            StartLegacyGame();
        }
    }

    /// <summary>
    /// Start a new game using story system.
    /// </summary>
    private void StartStoryGame(StoryInfo story)
    {
        Debug.Log($"[SettingsUI] Starting story game: {story.storyId}");

        // Load the story
        if (StoryManager.Instance != null)
        {
            bool loaded = StoryManager.Instance.LoadStory(story.storyId);
            Debug.Log($"[SettingsUI] Story loaded: {loaded}, IsStoryLoaded: {StoryManager.Instance.IsStoryLoaded}");
        }
        else
        {
            Debug.LogError("[SettingsUI] StoryManager.Instance is NULL! Make sure StoryManager is in the scene.");
            // Fall back to legacy if StoryManager doesn't exist
            StartLegacyGame();
            return;
        }

        // Load chapters if ChapterManager exists
        if (ChapterManager.Instance != null)
        {
            ChapterManager.Instance.ResetProgress();
            ChapterManager.Instance.LoadChapters(story.storyId);
            Debug.Log($"[SettingsUI] Chapters loaded for story: {story.storyId}");
        }

        // Create new player data
        PlayerData tempData = new PlayerData();

        // Apply story starting conditions
        StoryManager.Instance.ApplyStartingConditions(tempData);

        // Set starting room from story config
        tempData.currentRoom = story.config.startingRoom;
        Debug.Log($"[SettingsUI] Starting room: {tempData.currentRoom}");

        SaveGameManager.SaveGame(tempData);
        SaveGameManager.LoadGame();

        // Load the room - StoryManager should now have the story loaded
        Debug.Log($"[SettingsUI] About to load room, StoryManager.IsStoryLoaded: {StoryManager.Instance.IsStoryLoaded}");
        roomManager.LoadRoomFromJson(RoomManager.playerData.currentRoom);

        Debug.Log($"Started new game with story: {story.storyName}");
    }

    /// <summary>
    /// Start a new game using legacy system.
    /// </summary>
    private void StartLegacyGame()
    {
        // Unload any story
        if (StoryManager.Instance != null)
        {
            StoryManager.Instance.UnloadStory();
        }

        // Load the parameters from the JSON file in the Resources folder
        TextAsset jsonFile = Resources.Load<TextAsset>("NewGameParameters");
        GameParameters parameters = JsonUtility.FromJson<GameParameters>(jsonFile.text);

        PlayerData tempData = new PlayerData
        {
            currentRoom = parameters.currentRoom
        };

        // Set flags from JSON
        foreach (var flag in parameters.flags)
        {
            tempData.SetFlag(flag.Key, flag.Value);
        }

        // Add inventory items from JSON
        foreach (var itemName in parameters.inventory)
        {
            tempData.AddItem(itemName);
        }

        SaveGameManager.SaveGame(tempData);
        SaveGameManager.LoadGame();
        roomManager.LoadRoomFromJson(RoomManager.playerData.currentRoom);

        Debug.Log("Started new legacy game");
    }
    
    void LoadGame()
    {
        if (SaveGameManager.SaveFileExists())
        {
            SaveGameManager.LoadGame();
        }
    }

    void CloseView()
    {
        this.gameObject.SetActive(false);
    }

    /// <summary>
    /// Populate the story dropdown with available stories.
    /// </summary>
    private void PopulateStoryDropdown()
    {
        availableStories.Clear();

        // Add legacy/default option (uses original NewGameParameters.json)
        availableStories.Add(new StoryInfo
        {
            storyId = "",
            storyName = "Default Story",
            description = "The original game story using legacy content.",
            isLegacy = true
        });

        // Dynamically discover all story folders by loading all config files
        var allConfigs = Resources.LoadAll<TextAsset>("Stories");
        var discoveredStories = new HashSet<string>();

        foreach (var asset in allConfigs)
        {
            // Config files are named "config" and are in story subfolders
            // The asset name will just be "config", so we need to find the folder
            if (asset.name == "config")
            {
                try
                {
                    var config = JsonUtility.FromJson<StoryConfig>(asset.text);
                    if (config != null && !string.IsNullOrEmpty(config.storyId) && !discoveredStories.Contains(config.storyId))
                    {
                        discoveredStories.Add(config.storyId);
                        availableStories.Add(new StoryInfo
                        {
                            storyId = config.storyId,
                            storyName = config.storyName ?? config.storyId,
                            description = config.storyDescription ?? "A generated adventure.",
                            isLegacy = false,
                            config = config
                        });
                        Debug.Log($"[SettingsUI] Discovered story: {config.storyId} - {config.storyName}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to parse story config: {e.Message}");
                }
            }
        }

        // Also try known story folders in case Resources.LoadAll missed them
        string[] knownStories = { "dev_world" };
        foreach (var storyId in knownStories)
        {
            if (discoveredStories.Contains(storyId)) continue;

            TextAsset configAsset = Resources.Load<TextAsset>($"Stories/{storyId}/config");
            if (configAsset != null)
            {
                try
                {
                    var config = JsonUtility.FromJson<StoryConfig>(configAsset.text);
                    availableStories.Add(new StoryInfo
                    {
                        storyId = storyId,
                        storyName = config.storyName ?? storyId,
                        description = config.storyDescription ?? "",
                        isLegacy = false,
                        config = config
                    });
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to load story config for {storyId}: {e.Message}");
                }
            }
        }

        // Populate dropdown
        if (storyDropdown != null)
        {
            storyDropdown.ClearOptions();
            var options = new List<string>();
            foreach (var story in availableStories)
            {
                options.Add(story.storyName);
            }
            storyDropdown.AddOptions(options);
        }

        // Update description
        OnStorySelected(0);

        Debug.Log($"[SettingsUI] Found {availableStories.Count} stories");
    }

    /// <summary>
    /// Handle story selection change.
    /// </summary>
    private void OnStorySelected(int index)
    {
        selectedStoryIndex = index;

        if (storyDescriptionText != null && index < availableStories.Count)
        {
            storyDescriptionText.text = availableStories[index].description;
        }
    }

    /// <summary>
    /// Info about an available story.
    /// </summary>
    private class StoryInfo
    {
        public string storyId;
        public string storyName;
        public string description;
        public bool isLegacy;
        public StoryConfig config;
    }
}
