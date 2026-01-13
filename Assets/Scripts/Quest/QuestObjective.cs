using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enhanced quest data with objectives, prerequisites, and rewards.
/// </summary>
[Serializable]
public class QuestData
{
    public string questId;
    public string questName;
    public string questDescription;
    public string questGiver;               // NPC who gives this quest
    public string questGiverLocation;       // Location where quest giver is found

    // Quest chain/prerequisites
    public List<string> prerequisiteQuests = new List<string>();   // Must be completed first
    public List<string> prerequisiteFlags = new List<string>();    // Flags that must be true

    // Objectives
    public List<QuestObjective> objectives = new List<QuestObjective>();
    public int currentObjectiveIndex = 0;

    // Location reveals
    public List<string> revealsOnAccept = new List<string>();      // Location IDs revealed when accepted
    public List<string> revealsOnComplete = new List<string>();    // Location IDs revealed when completed

    // Rewards
    public QuestReward rewards;

    // State
    public QuestState state = QuestState.NotStarted;

    /// <summary>
    /// Get the current objective.
    /// </summary>
    public QuestObjective GetCurrentObjective()
    {
        if (objectives == null || currentObjectiveIndex >= objectives.Count)
            return null;
        return objectives[currentObjectiveIndex];
    }

    /// <summary>
    /// Check if all objectives are complete.
    /// </summary>
    public bool AreAllObjectivesComplete()
    {
        foreach (var objective in objectives)
        {
            if (!objective.isComplete)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Advance to next objective.
    /// </summary>
    public bool AdvanceObjective()
    {
        if (currentObjectiveIndex < objectives.Count - 1)
        {
            currentObjectiveIndex++;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Create from legacy QuestLogEntry.
    /// </summary>
    public static QuestData FromLegacy(QuestLogEntry legacy)
    {
        return new QuestData
        {
            questId = legacy.QuestID,
            questName = legacy.QuestName,
            questDescription = legacy.QuestDescription,
            state = legacy.IsCompleted ? QuestState.Completed : QuestState.Active
        };
    }
}

/// <summary>
/// A single quest objective.
/// </summary>
[Serializable]
public class QuestObjective
{
    public string objectiveId;
    public string description;
    public ObjectiveType type;
    public string targetId;             // Room, NPC, item, or enemy ID
    public int targetCount = 1;         // For kill X or collect X
    public int currentCount = 0;
    public bool isComplete = false;
    public bool isOptional = false;     // Optional objectives don't block progression

    /// <summary>
    /// Check if this objective is satisfied.
    /// </summary>
    public bool CheckCompletion()
    {
        isComplete = currentCount >= targetCount;
        return isComplete;
    }

    /// <summary>
    /// Increment progress and check completion.
    /// </summary>
    public bool AddProgress(int amount = 1)
    {
        currentCount = Mathf.Min(currentCount + amount, targetCount);
        return CheckCompletion();
    }

    /// <summary>
    /// Get progress string (e.g., "2/5").
    /// </summary>
    public string GetProgressString()
    {
        if (type == ObjectiveType.GoToRoom || type == ObjectiveType.TalkToNPC)
        {
            return isComplete ? "(Complete)" : "";
        }
        return $"({currentCount}/{targetCount})";
    }
}

/// <summary>
/// Types of quest objectives.
/// </summary>
public enum ObjectiveType
{
    GoToRoom,           // Visit a specific room
    TalkToNPC,          // Interact with specific NPC
    CollectItem,        // Obtain item(s)
    DeliverItem,        // Bring item to NPC
    DefeatEnemy,        // Kill specific enemy
    DefeatCount,        // Kill X enemies of type
    SetFlag,            // Trigger a specific flag
    UseItem,            // Use an item (at location or on target)
    Custom              // Checked via custom logic
}

/// <summary>
/// Quest state.
/// </summary>
public enum QuestState
{
    NotStarted,
    Active,
    ReadyToTurnIn,
    Completed,
    Failed
}

/// <summary>
/// Rewards granted on quest completion.
/// </summary>
[Serializable]
public class QuestReward
{
    public int experiencePoints;
    public int gold;
    public List<string> itemIds = new List<string>();
    public List<FlagReward> flagsToSet = new List<FlagReward>();
    public List<string> skillPointAwards = new List<string>();  // Skill names to grant points in

    /// <summary>
    /// Apply rewards to player.
    /// </summary>
    public void ApplyRewards(PlayerData playerData)
    {
        // Gold
        playerData.coins += gold;

        // XP
        if (playerData.UsesEnhancedStats)
        {
            playerData.stats.experiencePoints += experiencePoints;
        }

        // Items
        foreach (var itemId in itemIds)
        {
            playerData.AddItem(itemId);
        }

        // Flags
        foreach (var flagReward in flagsToSet)
        {
            playerData.SetFlag(flagReward.flagName, flagReward.flagValue);
        }

        Debug.Log($"Quest rewards applied: {gold} gold, {experiencePoints} XP, {itemIds.Count} items");
    }
}

[Serializable]
public class FlagReward
{
    public string flagName;
    public string flagValue;
}
