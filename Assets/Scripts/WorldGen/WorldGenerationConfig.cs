using System;
using System.Collections.Generic;
using UnityEngine;

namespace WorldGen
{
    /// <summary>
    /// Configuration for world generation, including prompts, settings, and chapter registry.
    /// Saved as config.json in the generated story folder.
    /// </summary>
    [Serializable]
    public class WorldGenerationConfig
    {
        // Identity
        public string configId;
        public string configName;
        public string createdAt;
        public string generatedBy; // "openai" or "anthropic"

        // World Prompt (saved for reference and regeneration)
        public WorldPrompt worldPrompt = new WorldPrompt();

        // Generation Settings
        public GenerationSettings settings = new GenerationSettings();

        // Chapter Registry
        public List<string> chapterIds = new List<string>();

        // API Configuration (API key stored separately for security)
        public LLMApiConfig apiConfig = new LLMApiConfig();
    }

    /// <summary>
    /// Detailed world prompt that guides generation.
    /// Saved as world_prompt.json for easy reference and editing.
    /// </summary>
    [Serializable]
    public class WorldPrompt
    {
        [Header("Core Identity")]
        public string worldName;
        public string theme;              // e.g., "dark fantasy", "steampunk", "horror"
        public string tone;               // e.g., "gritty", "whimsical", "serious"
        public string era;                // e.g., "medieval", "futuristic"

        [Header("Setting")]
        [TextArea(3, 10)]
        public string settingDescription; // Detailed world description
        public List<string> keyLocations = new List<string>();   // Major landmarks to include
        public List<string> majorFactions = new List<string>();  // Political/social groups

        [Header("Narrative")]
        [TextArea(2, 5)]
        public string mainConflict;       // Central tension of the story
        public string protagonistRole;    // Player's role in the world
        public List<string> narrativeThemes = new List<string>(); // Recurring story themes

        [Header("Style")]
        public string writingStyle;       // Dialogue style preferences
        public string dialogueTone;       // How NPCs speak

        [Header("Custom")]
        public List<CustomParameter> customParameters = new List<CustomParameter>();
    }

    [Serializable]
    public class CustomParameter
    {
        public string key;
        public string value;
    }

    /// <summary>
    /// Settings that control generation quantities and balance.
    /// </summary>
    [Serializable]
    public class GenerationSettings
    {
        [Header("Scale")]
        public int totalChapters = 5;
        public int locationsPerChapter = 10;
        public int subLocationsPerMajor = 2;
        public int questsPerChapter = 7;
        public int mainQuestsPerChapter = 2;

        [Header("Content")]
        public int enemyTypesPerChapter = 5;
        public int itemsPerChapter = 10;
        public int npcsPerChapter = 8;

        [Header("Balance")]
        [Range(0f, 1f)]
        public float difficultyVariance = 0.3f; // Allow +/- 30% difficulty deviation
        public bool allowHardSideQuests = true;
        public int hardSideQuestChance = 20;    // Percentage chance for a hard side quest

        [Header("Location Distribution")]
        [Range(0f, 1f)]
        public float hubLocationRatio = 0.2f;      // ~20% always accessible
        [Range(0f, 1f)]
        public float questRevealedRatio = 0.6f;    // ~60% revealed by quests
        // Remaining ~20% are progression-gated
    }

    /// <summary>
    /// LLM API configuration (API key stored in EditorPrefs/PlayerPrefs for security).
    /// </summary>
    [Serializable]
    public class LLMApiConfig
    {
        public string provider = "openai"; // "openai" or "anthropic"
        public string model = "gpt-4";     // Model ID

        [Header("Generation Parameters")]
        [Range(0f, 2f)]
        public float temperature = 0.7f;
        public int maxTokensPerRequest = 4000;
        public int requestDelayMs = 1000;  // Rate limiting between requests

        [Header("Retry Settings")]
        public int maxRetries = 3;
        public int retryDelayMs = 2000;
    }
}
