using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central quest management system.
/// Handles quest tracking, objective updates, and rewards.
/// </summary>
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    // All loaded quest data (templates)
    private Dictionary<string, QuestData> allQuests = new Dictionary<string, QuestData>();

    // Active quests (player's current quests)
    private List<QuestData> activeQuests = new List<QuestData>();
    private List<QuestData> completedQuests = new List<QuestData>();

    // Events
    public event Action<QuestData> OnQuestStarted;
    public event Action<QuestData> OnQuestCompleted;
    public event Action<QuestData> OnMainQuestCompleted;  // Fired specifically for main quests
    public event Action<QuestData, QuestObjective> OnObjectiveUpdated;
    public event Action<List<string>> OnLocationsRevealed;

    // Reference to map system for location reveals
    private MapSystem mapSystem;

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
        mapSystem = FindObjectOfType<MapSystem>();
    }

    /// <summary>
    /// Load a quest template from Resources.
    /// </summary>
    public QuestData LoadQuestData(string questId)
    {
        // Check cache first
        if (allQuests.TryGetValue(questId, out QuestData cached))
        {
            return cached;
        }

        // Try story-specific path first (if StoryManager exists)
        TextAsset questAsset = null;

        if (StoryManager.Instance != null)
        {
            questAsset = StoryManager.Instance.LoadQuestData(questId);
        }

        // Fall back to legacy path
        if (questAsset == null)
        {
            questAsset = Resources.Load<TextAsset>($"Quests/{questId}");
        }

        if (questAsset == null)
        {
            Debug.LogError($"Quest not found: {questId}");
            return null;
        }

        QuestData quest = JsonUtility.FromJson<QuestData>(questAsset.text);

        // Handle legacy format (QuestLogEntry)
        if (string.IsNullOrEmpty(quest.questId) && questAsset.text.Contains("QuestID"))
        {
            var legacy = JsonUtility.FromJson<QuestLogEntry>(questAsset.text);
            quest = QuestData.FromLegacy(legacy);
        }

        allQuests[questId] = quest;
        return quest;
    }

    /// <summary>
    /// Check if a quest can be started.
    /// </summary>
    public bool CanStartQuest(string questId)
    {
        QuestData quest = LoadQuestData(questId);
        if (quest == null) return false;

        var playerFlags = RoomManager.playerData?.Flags;
        if (playerFlags == null) return false;

        // Check if already active or completed
        if (activeQuests.Exists(q => q.questId == questId)) return false;
        if (completedQuests.Exists(q => q.questId == questId)) return false;

        // Check prerequisite quests
        foreach (var prereqQuest in quest.prerequisiteQuests)
        {
            if (!playerFlags.TryGetValue(prereqQuest, out string value) || value != "concluded")
            {
                return false;
            }
        }

        // Check prerequisite flags
        foreach (var prereqFlag in quest.prerequisiteFlags)
        {
            if (!playerFlags.TryGetValue(prereqFlag, out string value) || value != "true")
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Start a quest.
    /// </summary>
    public bool StartQuest(string questId)
    {
        if (!CanStartQuest(questId))
        {
            Debug.Log($"Cannot start quest: {questId} - prerequisites not met");
            return false;
        }

        QuestData quest = LoadQuestData(questId);
        if (quest == null) return false;

        // Create instance copy for tracking
        var questInstance = CreateQuestInstance(quest);
        questInstance.state = QuestState.Active;
        activeQuests.Add(questInstance);

        // Set quest flag
        RoomManager.playerData?.SetFlag(questId, "active");

        // Reveal locations
        if (questInstance.revealsOnAccept?.Count > 0)
        {
            RevealLocations(questInstance.revealsOnAccept);
        }

        // Update legacy diary system for backward compatibility
        var diary = RoomManager.Diary;
        if (diary != null)
        {
            diary.OnQuestReceived(questId);
        }

        OnQuestStarted?.Invoke(questInstance);
        Debug.Log($"Quest started: {questInstance.questName}");

        SaveQuestProgress();
        return true;
    }

    /// <summary>
    /// Create a copy of quest data for active tracking.
    /// </summary>
    private QuestData CreateQuestInstance(QuestData template)
    {
        // Deep copy the quest
        string json = JsonUtility.ToJson(template);
        return JsonUtility.FromJson<QuestData>(json);
    }

    /// <summary>
    /// Update quest objective progress.
    /// </summary>
    public void UpdateObjective(ObjectiveType type, string targetId, int amount = 1)
    {
        foreach (var quest in activeQuests)
        {
            var objective = quest.GetCurrentObjective();
            if (objective == null) continue;

            if (objective.type == type && objective.targetId == targetId)
            {
                bool wasComplete = objective.isComplete;
                objective.AddProgress(amount);

                if (!wasComplete && objective.isComplete)
                {
                    Debug.Log($"Objective complete: {objective.description}");
                    OnObjectiveUpdated?.Invoke(quest, objective);

                    // Try to advance to next objective
                    if (!quest.AdvanceObjective())
                    {
                        // All objectives complete - mark ready for turn-in
                        if (quest.AreAllObjectivesComplete())
                        {
                            quest.state = QuestState.ReadyToTurnIn;
                            Debug.Log($"Quest ready for completion: {quest.questName}");
                        }
                    }
                }
            }
        }

        SaveQuestProgress();
    }

    /// <summary>
    /// Notify that a room was entered.
    /// </summary>
    public void OnRoomEntered(string roomId)
    {
        UpdateObjective(ObjectiveType.GoToRoom, roomId, 1);
    }

    /// <summary>
    /// Notify that an NPC was talked to.
    /// </summary>
    public void OnNPCInteracted(string npcId)
    {
        UpdateObjective(ObjectiveType.TalkToNPC, npcId, 1);
    }

    /// <summary>
    /// Notify that an enemy was defeated.
    /// </summary>
    public void OnEnemyDefeated(string enemyId)
    {
        UpdateObjective(ObjectiveType.DefeatEnemy, enemyId, 1);
        UpdateObjective(ObjectiveType.DefeatCount, enemyId, 1);
    }

    /// <summary>
    /// Notify that an item was collected.
    /// </summary>
    public void OnItemCollected(string itemId)
    {
        UpdateObjective(ObjectiveType.CollectItem, itemId, 1);
    }

    /// <summary>
    /// Notify that an item was delivered.
    /// </summary>
    public void OnItemDelivered(string itemId, string toNpcId)
    {
        UpdateObjective(ObjectiveType.DeliverItem, $"{itemId}_{toNpcId}", 1);
    }

    /// <summary>
    /// Complete a quest and grant rewards.
    /// </summary>
    public bool CompleteQuest(string questId)
    {
        var quest = activeQuests.Find(q => q.questId == questId);
        if (quest == null)
        {
            Debug.LogWarning($"Quest not active: {questId}");
            return false;
        }

        if (quest.state != QuestState.ReadyToTurnIn && !quest.AreAllObjectivesComplete())
        {
            Debug.LogWarning($"Quest not ready for completion: {questId}");
            return false;
        }

        // Mark complete
        quest.state = QuestState.Completed;
        activeQuests.Remove(quest);
        completedQuests.Add(quest);

        // Set flag
        RoomManager.playerData?.SetFlag(questId, "concluded");

        // Grant rewards
        if (quest.rewards != null && RoomManager.playerData != null)
        {
            quest.rewards.ApplyRewards(RoomManager.playerData);
        }

        // Reveal locations
        if (quest.revealsOnComplete?.Count > 0)
        {
            RevealLocations(quest.revealsOnComplete);
        }

        // Update legacy diary
        var diary = RoomManager.Diary;
        if (diary != null)
        {
            diary.OnQuestConcluded(questId);
        }

        OnQuestCompleted?.Invoke(quest);

        // Fire main quest event if applicable
        if (quest.questType == QuestType.Main)
        {
            OnMainQuestCompleted?.Invoke(quest);
            Debug.Log($"Main quest completed: {quest.questName} (Chapter {quest.chapterNumber})");
        }
        else
        {
            Debug.Log($"Quest completed: {quest.questName}");
        }

        SaveQuestProgress();
        return true;
    }

    /// <summary>
    /// Reveal locations on the map.
    /// </summary>
    private void RevealLocations(List<string> locationIds)
    {
        foreach (var locId in locationIds)
        {
            RoomManager.playerData?.SetFlag($"location_{locId}", "true");
        }

        if (mapSystem != null)
        {
            mapSystem.RefreshVisibility();
        }

        OnLocationsRevealed?.Invoke(locationIds);
    }

    /// <summary>
    /// Get all active quests.
    /// </summary>
    public List<QuestData> GetActiveQuests()
    {
        return new List<QuestData>(activeQuests);
    }

    /// <summary>
    /// Get all completed quests.
    /// </summary>
    public List<QuestData> GetCompletedQuests()
    {
        return new List<QuestData>(completedQuests);
    }

    /// <summary>
    /// Get active main quests.
    /// </summary>
    public List<QuestData> GetActiveMainQuests()
    {
        return activeQuests.FindAll(q => q.questType == QuestType.Main);
    }

    /// <summary>
    /// Get active side quests.
    /// </summary>
    public List<QuestData> GetActiveSideQuests()
    {
        return activeQuests.FindAll(q => q.questType == QuestType.Side);
    }

    /// <summary>
    /// Get active quests for a specific chapter.
    /// </summary>
    public List<QuestData> GetActiveQuestsForChapter(int chapterNumber)
    {
        return activeQuests.FindAll(q => q.chapterNumber == chapterNumber);
    }

    /// <summary>
    /// Check if the current main quest for a chapter is complete.
    /// </summary>
    public bool IsChapterMainQuestComplete(int chapterNumber)
    {
        // Check if any main quest from this chapter is in completed list
        return completedQuests.Exists(q =>
            q.questType == QuestType.Main && q.chapterNumber == chapterNumber);
    }

    /// <summary>
    /// Get quests available at a location.
    /// </summary>
    public List<QuestData> GetAvailableQuestsAtLocation(string locationId)
    {
        var available = new List<QuestData>();

        // Search all quest files for quests at this location
        var questFiles = Resources.LoadAll<TextAsset>("Quests");
        foreach (var file in questFiles)
        {
            var quest = LoadQuestData(file.name);
            if (quest != null && quest.questGiverLocation == locationId)
            {
                if (CanStartQuest(quest.questId))
                {
                    available.Add(quest);
                }
            }
        }

        return available;
    }

    /// <summary>
    /// Save quest progress to PlayerPrefs (alongside legacy diary).
    /// </summary>
    private void SaveQuestProgress()
    {
        var saveData = new QuestSaveData
        {
            activeQuestIds = new List<string>(),
            completedQuestIds = new List<string>()
        };

        foreach (var quest in activeQuests)
        {
            saveData.activeQuestIds.Add(quest.questId);
        }

        foreach (var quest in completedQuests)
        {
            saveData.completedQuestIds.Add(quest.questId);
        }

        string json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString("QuestManagerData", json);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Load quest progress from PlayerPrefs.
    /// </summary>
    public void LoadQuestProgress()
    {
        string json = PlayerPrefs.GetString("QuestManagerData", "");
        if (string.IsNullOrEmpty(json)) return;

        var saveData = JsonUtility.FromJson<QuestSaveData>(json);

        activeQuests.Clear();
        completedQuests.Clear();

        foreach (var questId in saveData.activeQuestIds)
        {
            var quest = LoadQuestData(questId);
            if (quest != null)
            {
                quest.state = QuestState.Active;
                activeQuests.Add(quest);
            }
        }

        foreach (var questId in saveData.completedQuestIds)
        {
            var quest = LoadQuestData(questId);
            if (quest != null)
            {
                quest.state = QuestState.Completed;
                completedQuests.Add(quest);
            }
        }
    }

    /// <summary>
    /// Reset all quest progress (for new game).
    /// </summary>
    public void ResetProgress()
    {
        activeQuests.Clear();
        completedQuests.Clear();
        PlayerPrefs.DeleteKey("QuestManagerData");
    }
}

/// <summary>
/// Save data for quest progress.
/// </summary>
[Serializable]
public class QuestSaveData
{
    public List<string> activeQuestIds;
    public List<string> completedQuestIds;
}
