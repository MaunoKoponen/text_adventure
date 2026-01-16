using System;
using System.Collections.Generic;
using UnityEngine;

namespace WorldGen
{
    /// <summary>
    /// Defines a chapter with all its content references and unlock conditions.
    /// Saved as chapter_{N}.json in the generated story folder.
    /// </summary>
    [Serializable]
    public class ChapterData
    {
        [Header("Identity")]
        public string chapterId;
        public string chapterName;
        public int chapterNumber;           // 1-indexed, used for difficulty scaling
        [TextArea(2, 5)]
        public string chapterDescription;

        [Header("Unlock Conditions")]
        public string unlockQuestId;        // Main quest that must be completed to unlock
        public List<string> unlockFlags = new List<string>();  // Additional flags required

        [Header("Difficulty")]
        public int baseDifficulty;          // Derived from chapter number (chapterNumber * 2)
        public int minEnemyCR;              // Challenge Rating floor
        public int maxEnemyCR;              // Challenge Rating ceiling

        [Header("Content References")]
        public List<string> locationIds = new List<string>();
        public List<string> questIds = new List<string>();
        public List<string> mainQuestIds = new List<string>();  // Subset required for progression
        public List<string> enemyIds = new List<string>();
        public List<string> itemIds = new List<string>();
        public List<string> npcIds = new List<string>();

        [Header("Narrative")]
        [TextArea(3, 8)]
        public string chapterIntro;         // Opening narration when chapter starts
        [TextArea(3, 8)]
        public string chapterOutro;         // Completion text when main quest done
        [TextArea(2, 5)]
        public string mainQuestSummary;     // Brief summary of main storyline

        [Header("Map")]
        public string mapId;                // Reference to generated MapData
        public string hubLocationId;        // Main hub for this chapter
        public string entryLocationId;      // Where player enters from previous chapter
        public string exitLocationId;       // Transition to next chapter

        [Header("Generation Metadata")]
        public string generatedAt;
        public bool isGenerated;
        public bool isValidated;
        public List<string> validationErrors = new List<string>();

        /// <summary>
        /// Calculate difficulty based on chapter number.
        /// </summary>
        public void CalculateDifficulty()
        {
            baseDifficulty = chapterNumber * 2;
            minEnemyCR = Mathf.Max(1, chapterNumber - 1);
            maxEnemyCR = chapterNumber + 2;
        }

        /// <summary>
        /// Check if all main quests are in the quest list.
        /// </summary>
        public bool ValidateMainQuests()
        {
            foreach (var mainQuestId in mainQuestIds)
            {
                if (!questIds.Contains(mainQuestId))
                {
                    validationErrors.Add($"Main quest {mainQuestId} not in quest list");
                    return false;
                }
            }
            return true;
        }
    }

    /// <summary>
    /// Summary of a chapter for outline generation (used before full content generation).
    /// </summary>
    [Serializable]
    public class ChapterOutline
    {
        public string chapterId;
        public string chapterName;
        public string chapterDescription;
        public List<LocationSummary> locations = new List<LocationSummary>();
        public List<QuestSummary> mainQuests = new List<QuestSummary>();
        public List<QuestSummary> sideQuests = new List<QuestSummary>();
        public List<NPCSummary> keyNPCs = new List<NPCSummary>();
        public List<EnemySummary> enemies = new List<EnemySummary>();
    }

    [Serializable]
    public class LocationSummary
    {
        public string locationId;
        public string locationName;
        public string locationType;  // "hub", "exploration", "dungeon", "boss", "transition"
        public string description;
        public bool alwaysVisible;
        public List<string> connectedTo = new List<string>();
    }

    [Serializable]
    public class QuestSummary
    {
        public string questId;
        public string questName;
        public string description;
        public string questGiver;
        public string taskLocation;
        public int difficulty;
    }

    [Serializable]
    public class NPCSummary
    {
        public string npcId;
        public string npcName;
        public string role;          // "quest_giver", "merchant", "mentor", "antagonist"
        public string personality;
        public string locationId;
    }

    [Serializable]
    public class EnemySummary
    {
        public string enemyId;
        public string enemyName;
        public string enemyType;
        public int challengeRating;
        public string description;
    }
}
