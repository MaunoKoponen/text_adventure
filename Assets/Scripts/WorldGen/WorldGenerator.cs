using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace WorldGen
{
    /// <summary>
    /// Orchestrates world generation using LLM API.
    /// Generates chapters with locations, quests, NPCs, enemies, and items.
    /// </summary>
    public class WorldGenerator : MonoBehaviour
    {
        public static WorldGenerator Instance { get; private set; }

        [Header("Configuration")]
        public WorldGenerationConfig currentConfig;
        public string outputStoryId = "generated_world";

        [Header("References")]
        [SerializeField] private LLMService llmService;

        // Generation state
        private bool isGenerating = false;
        private bool wasCancelled = false;
        private float progress = 0f;
        private string currentStatus = "";

        // Generated content (held in memory until saved)
        private List<ChapterData> generatedChapters = new List<ChapterData>();
        private Dictionary<string, string> generatedRooms = new Dictionary<string, string>();      // roomId -> JSON
        private Dictionary<string, string> generatedQuests = new Dictionary<string, string>();    // questId -> JSON
        private Dictionary<string, string> generatedEnemies = new Dictionary<string, string>();   // enemyId -> JSON
        private Dictionary<string, string> generatedItems = new Dictionary<string, string>();     // itemId -> JSON

        // Events
        public event Action<string> OnStatusUpdate;
        public event Action<float> OnProgressUpdate;
        public event Action<ChapterData> OnChapterGenerated;
        public event Action<ChapterOutline> OnChapterOutlineGenerated;
        public event Action<WorldGenerationConfig> OnGenerationComplete;
        public event Action<string> OnError;
        public event Action<List<string>> OnValidationErrors;

        // Properties
        public bool IsGenerating => isGenerating;
        public float Progress => progress;
        public string CurrentStatus => currentStatus;

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

        private void Start()
        {
            // Find or create LLMService
            if (llmService == null)
            {
                llmService = FindObjectOfType<LLMService>();
                if (llmService == null)
                {
                    var go = new GameObject("LLMService");
                    go.transform.SetParent(transform);
                    llmService = go.AddComponent<LLMService>();
                }
            }
        }

        /// <summary>
        /// Start generating a new world from config.
        /// </summary>
        public void StartGeneration(WorldGenerationConfig config, string apiKey)
        {
            if (isGenerating)
            {
                OnError?.Invoke("Generation already in progress");
                return;
            }

            currentConfig = config;
            llmService.Initialize(config.apiConfig, apiKey);

            StartCoroutine(GenerateWorldCoroutine());
        }

        /// <summary>
        /// Generate a single chapter (for incremental generation during playtesting).
        /// </summary>
        public void GenerateNextChapter(string apiKey)
        {
            if (isGenerating)
            {
                OnError?.Invoke("Generation already in progress");
                return;
            }

            if (currentConfig == null)
            {
                OnError?.Invoke("No world config loaded. Start a new world first.");
                return;
            }

            int nextChapterNumber = currentConfig.chapterIds.Count + 1;
            llmService.Initialize(currentConfig.apiConfig, apiKey);

            StartCoroutine(GenerateChapterCoroutine(nextChapterNumber));
        }

        /// <summary>
        /// Main generation coroutine for full world.
        /// </summary>
        private IEnumerator GenerateWorldCoroutine()
        {
            isGenerating = true;
            wasCancelled = false;
            progress = 0f;
            ClearGeneratedContent();

            UpdateStatus("Starting world generation...");

            int totalChapters = currentConfig.settings.totalChapters;

            for (int i = 1; i <= totalChapters; i++)
            {
                UpdateStatus($"Generating Chapter {i}/{totalChapters}...");

                yield return StartCoroutine(GenerateChapterCoroutine(i));

                progress = (float)i / totalChapters;
                OnProgressUpdate?.Invoke(progress);

                if (wasCancelled)
                {
                    Debug.Log("[WorldGen] Generation was cancelled");
                    yield break;
                }
            }

            // Validate all content
            UpdateStatus("Validating generated content...");
            yield return StartCoroutine(ValidateAllContent());

            // Save to files
            UpdateStatus("Saving to JSON files...");
            yield return StartCoroutine(SaveAllContent());

            isGenerating = false;
            progress = 1f;
            UpdateStatus("Generation complete!");

            OnGenerationComplete?.Invoke(currentConfig);
        }

        /// <summary>
        /// Generate a single chapter.
        /// </summary>
        private IEnumerator GenerateChapterCoroutine(int chapterNumber)
        {
            isGenerating = true;

            // Step 1: Generate chapter outline
            UpdateStatus($"Chapter {chapterNumber}: Generating outline...");

            ChapterData previousChapter = chapterNumber > 1 && generatedChapters.Count >= chapterNumber - 1
                ? generatedChapters[chapterNumber - 2]
                : null;

            string outlinePrompt = PromptTemplates.GetChapterOutlinePrompt(
                chapterNumber, previousChapter, currentConfig.worldPrompt, currentConfig.settings);

            string systemPrompt = PromptTemplates.GetSystemPrompt(currentConfig.worldPrompt);

            LLMResponse outlineResponse = null;
            yield return StartCoroutine(SendRequestAndWait(outlinePrompt, systemPrompt, r => outlineResponse = r));

            if (!outlineResponse.success)
            {
                OnError?.Invoke($"Failed to generate chapter outline: {outlineResponse.error}");
                isGenerating = false;
                yield break;
            }

            // Parse outline
            ChapterOutline outline = null;
            try
            {
                outline = JsonUtility.FromJson<ChapterOutline>(outlineResponse.content);
            }
            catch (Exception e)
            {
                OnError?.Invoke($"Failed to parse chapter outline: {e.Message}\nResponse: {outlineResponse.content}");
                isGenerating = false;
                yield break;
            }

            OnChapterOutlineGenerated?.Invoke(outline);

            // Create ChapterData from outline
            var chapter = new ChapterData
            {
                chapterId = outline.chapterId,
                chapterName = outline.chapterName,
                chapterNumber = chapterNumber,
                chapterDescription = outline.chapterDescription,
                isGenerated = true,
                generatedAt = DateTime.Now.ToString("o")
            };
            chapter.CalculateDifficulty();

            // Step 2: Generate locations/rooms
            UpdateStatus($"Chapter {chapterNumber}: Generating {outline.locations.Count} locations...");

            int locationIndex = 0;
            foreach (var locationSummary in outline.locations)
            {
                locationIndex++;
                UpdateStatus($"Chapter {chapterNumber}: Location {locationIndex}/{outline.locations.Count} - {locationSummary.locationName}");

                string roomPrompt = PromptTemplates.GetRoomPrompt(locationSummary, chapter, currentConfig.worldPrompt);

                LLMResponse roomResponse = null;
                yield return StartCoroutine(SendRequestAndWait(roomPrompt, systemPrompt, r => roomResponse = r));

                if (roomResponse.success)
                {
                    generatedRooms[locationSummary.locationId] = roomResponse.content;
                    chapter.locationIds.Add(locationSummary.locationId);
                }
                else
                {
                    Debug.LogWarning($"Failed to generate room {locationSummary.locationId}: {roomResponse.error}");
                }

                yield return null; // Yield frame to prevent freezing
            }

            // Step 3: Generate quests
            UpdateStatus($"Chapter {chapterNumber}: Generating quests...");

            // Main quests
            foreach (var questSummary in outline.mainQuests)
            {
                string questPrompt = PromptTemplates.GetQuestPrompt(questSummary, chapter, currentConfig.worldPrompt, true);

                LLMResponse questResponse = null;
                yield return StartCoroutine(SendRequestAndWait(questPrompt, systemPrompt, r => questResponse = r));

                if (questResponse.success)
                {
                    generatedQuests[questSummary.questId] = questResponse.content;
                    chapter.questIds.Add(questSummary.questId);
                    chapter.mainQuestIds.Add(questSummary.questId);
                }

                yield return null;
            }

            // Side quests
            foreach (var questSummary in outline.sideQuests)
            {
                string questPrompt = PromptTemplates.GetQuestPrompt(questSummary, chapter, currentConfig.worldPrompt, false);

                LLMResponse questResponse = null;
                yield return StartCoroutine(SendRequestAndWait(questPrompt, systemPrompt, r => questResponse = r));

                if (questResponse.success)
                {
                    generatedQuests[questSummary.questId] = questResponse.content;
                    chapter.questIds.Add(questSummary.questId);
                }

                yield return null;
            }

            // Step 4: Generate enemies
            UpdateStatus($"Chapter {chapterNumber}: Generating enemies...");

            foreach (var enemySummary in outline.enemies)
            {
                string enemyPrompt = PromptTemplates.GetEnemyPrompt(enemySummary, chapter, currentConfig.worldPrompt);

                LLMResponse enemyResponse = null;
                yield return StartCoroutine(SendRequestAndWait(enemyPrompt, systemPrompt, r => enemyResponse = r));

                if (enemyResponse.success)
                {
                    generatedEnemies[enemySummary.enemyId] = enemyResponse.content;
                    chapter.enemyIds.Add(enemySummary.enemyId);
                }

                yield return null;
            }

            // Set chapter unlock quest (last main quest of this chapter unlocks next)
            if (chapter.mainQuestIds.Count > 0)
            {
                chapter.unlockQuestId = chapter.mainQuestIds[chapter.mainQuestIds.Count - 1];
            }

            // Store chapter
            generatedChapters.Add(chapter);
            currentConfig.chapterIds.Add(chapter.chapterId);

            OnChapterGenerated?.Invoke(chapter);
            Debug.Log($"[WorldGen] Chapter {chapterNumber} generation complete");
        }

        /// <summary>
        /// Helper to send request and wait for response.
        /// </summary>
        private IEnumerator SendRequestAndWait(string prompt, string systemPrompt, Action<LLMResponse> callback)
        {
            bool completed = false;
            LLMResponse response = null;

            var request = new LLMRequest
            {
                requestId = Guid.NewGuid().ToString(),
                prompt = prompt,
                systemPrompt = systemPrompt,
                callback = r =>
                {
                    response = r;
                    completed = true;
                }
            };

            llmService.QueueRequest(request);

            while (!completed)
            {
                yield return null;
            }

            callback(response);
        }

        /// <summary>
        /// Validate all generated content.
        /// </summary>
        private IEnumerator ValidateAllContent()
        {
            Debug.Log("[WorldGen] Starting validation...");
            var errors = new List<string>();

            // Validate rooms
            Debug.Log($"[WorldGen] Validating {generatedRooms.Count} rooms...");
            foreach (var kvp in generatedRooms)
            {
                try
                {
                    var result = JsonValidator.ValidateRoom(kvp.Value);
                    if (!result.isValid)
                    {
                        errors.AddRange(result.errors.ConvertAll(e => $"Room {kvp.Key}: {e}"));
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[WorldGen] Room validation error for {kvp.Key}: {e.Message}");
                    errors.Add($"Room {kvp.Key}: validation exception - {e.Message}");
                }
            }

            yield return null;

            // Validate quests
            Debug.Log($"[WorldGen] Validating {generatedQuests.Count} quests...");
            foreach (var kvp in generatedQuests)
            {
                try
                {
                    var result = JsonValidator.ValidateQuest(kvp.Value);
                    if (!result.isValid)
                    {
                        errors.AddRange(result.errors.ConvertAll(e => $"Quest {kvp.Key}: {e}"));
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[WorldGen] Quest validation error for {kvp.Key}: {e.Message}");
                    errors.Add($"Quest {kvp.Key}: validation exception - {e.Message}");
                }
            }

            yield return null;

            // Validate enemies
            Debug.Log($"[WorldGen] Validating {generatedEnemies.Count} enemies...");
            foreach (var kvp in generatedEnemies)
            {
                try
                {
                    var result = JsonValidator.ValidateEnemy(kvp.Value);
                    if (!result.isValid)
                    {
                        errors.AddRange(result.errors.ConvertAll(e => $"Enemy {kvp.Key}: {e}"));
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[WorldGen] Enemy validation error for {kvp.Key}: {e.Message}");
                    errors.Add($"Enemy {kvp.Key}: validation exception - {e.Message}");
                }
            }

            yield return null;

            // Cross-reference validation
            Debug.Log("[WorldGen] Checking cross-references...");
            try
            {
                var roomIds = new HashSet<string>(generatedRooms.Keys);
                var questIds = new HashSet<string>(generatedQuests.Keys);
                var enemyIds = new HashSet<string>(generatedEnemies.Keys);

                var integrityErrors = ContentIntegrityChecker.CheckAllIntegrity(
                    generatedChapters, roomIds, questIds, enemyIds);
                errors.AddRange(integrityErrors);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldGen] Cross-reference check exception: {e}");
                errors.Add($"Cross-reference check exception: {e.Message}");
            }

            if (errors.Count > 0)
            {
                Debug.LogWarning($"[WorldGen] Validation found {errors.Count} issues");
                OnValidationErrors?.Invoke(errors);
                foreach (var error in errors)
                {
                    Debug.LogWarning($"[WorldGen] Validation: {error}");
                }
            }
            else
            {
                Debug.Log("[WorldGen] Validation passed with no errors!");
                UpdateStatus("Validation passed!");
            }
        }

        /// <summary>
        /// Save all generated content to JSON files.
        /// </summary>
        private IEnumerator SaveAllContent()
        {
            string basePath = Path.Combine(Application.dataPath, "Resources", "Stories", outputStoryId);
            Debug.Log($"[WorldGen] Saving to path: {basePath}");
            bool directoryCreated = false;

            // Create directories
            try
            {
                Directory.CreateDirectory(basePath);
                Directory.CreateDirectory(Path.Combine(basePath, "Rooms"));
                Directory.CreateDirectory(Path.Combine(basePath, "Quests"));
                Directory.CreateDirectory(Path.Combine(basePath, "Enemies"));
                Directory.CreateDirectory(Path.Combine(basePath, "Items"));
                Directory.CreateDirectory(Path.Combine(basePath, "Maps"));
                Directory.CreateDirectory(Path.Combine(basePath, "Chapters"));
                Debug.Log($"[WorldGen] Created directories at: {basePath}");
                directoryCreated = true;
            }
            catch (Exception e)
            {
                OnError?.Invoke($"Failed to create directories: {e.Message}");
                Debug.LogError($"[WorldGen] Failed to create directories: {e}");
            }

            if (!directoryCreated)
            {
                yield break;
            }

            yield return null;

            // Save config
            bool configSaved = false;
            try
            {
                string configJson = JsonUtility.ToJson(currentConfig, true);
                File.WriteAllText(Path.Combine(basePath, "generation_config.json"), configJson);
                Debug.Log("[WorldGen] Saved generation_config.json");

                // Save world prompt
                string promptJson = JsonUtility.ToJson(currentConfig.worldPrompt, true);
                File.WriteAllText(Path.Combine(basePath, "world_prompt.json"), promptJson);
                Debug.Log("[WorldGen] Saved world_prompt.json");
                configSaved = true;
            }
            catch (Exception e)
            {
                OnError?.Invoke($"Failed to save config: {e.Message}");
                Debug.LogError($"[WorldGen] Failed to save config: {e}");
            }

            if (!configSaved)
            {
                yield break;
            }

            yield return null;

            // Save chapters
            Debug.Log($"[WorldGen] Saving {generatedChapters.Count} chapters...");
            foreach (var chapter in generatedChapters)
            {
                try
                {
                    string chapterJson = JsonUtility.ToJson(chapter, true);
                    string chapterPath = Path.Combine(basePath, "Chapters", $"{chapter.chapterId}.json");
                    File.WriteAllText(chapterPath, chapterJson);
                    Debug.Log($"[WorldGen] Saved chapter: {chapter.chapterId}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[WorldGen] Failed to save chapter {chapter.chapterId}: {e}");
                }
            }

            yield return null;

            // Save rooms
            Debug.Log($"[WorldGen] Saving {generatedRooms.Count} rooms...");
            foreach (var kvp in generatedRooms)
            {
                try
                {
                    string roomPath = Path.Combine(basePath, "Rooms", $"{kvp.Key}.json");
                    File.WriteAllText(roomPath, kvp.Value);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[WorldGen] Failed to save room {kvp.Key}: {e}");
                }
            }

            yield return null;

            // Save quests
            Debug.Log($"[WorldGen] Saving {generatedQuests.Count} quests...");
            foreach (var kvp in generatedQuests)
            {
                try
                {
                    string questPath = Path.Combine(basePath, "Quests", $"{kvp.Key}.json");
                    File.WriteAllText(questPath, kvp.Value);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[WorldGen] Failed to save quest {kvp.Key}: {e}");
                }
            }

            yield return null;

            // Save enemies
            Debug.Log($"[WorldGen] Saving {generatedEnemies.Count} enemies...");
            foreach (var kvp in generatedEnemies)
            {
                try
                {
                    string enemyPath = Path.Combine(basePath, "Enemies", $"{kvp.Key}.json");
                    File.WriteAllText(enemyPath, kvp.Value);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[WorldGen] Failed to save enemy {kvp.Key}: {e}");
                }
            }

            yield return null;

            // Save items
            Debug.Log($"[WorldGen] Saving {generatedItems.Count} items...");
            foreach (var kvp in generatedItems)
            {
                try
                {
                    string itemPath = Path.Combine(basePath, "Items", $"{kvp.Key}.json");
                    File.WriteAllText(itemPath, kvp.Value);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[WorldGen] Failed to save item {kvp.Key}: {e}");
                }
            }

            yield return null;

            // Create story config
            try
            {
                var storyConfig = CreateStoryConfig();
                string storyConfigJson = JsonUtility.ToJson(storyConfig, true);
                File.WriteAllText(Path.Combine(basePath, "config.json"), storyConfigJson);
                Debug.Log("[WorldGen] Saved config.json (story config)");
            }
            catch (Exception e)
            {
                OnError?.Invoke($"Failed to save story config: {e.Message}");
                Debug.LogError($"[WorldGen] Failed to save story config: {e}");
            }

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif

            UpdateStatus($"Saved to: Stories/{outputStoryId}/");
            Debug.Log($"[WorldGen] Save complete! Path: {basePath}");
        }

        /// <summary>
        /// Create StoryConfig from generated content.
        /// </summary>
        private StoryConfig CreateStoryConfig()
        {
            // Use worldName if set, otherwise fall back to outputStoryId
            string name = currentConfig.worldPrompt.worldName;
            if (string.IsNullOrEmpty(name))
            {
                name = outputStoryId;
            }

            var config = new StoryConfig
            {
                storyId = outputStoryId,
                storyName = name,
                storyDescription = currentConfig.worldPrompt.settingDescription ?? "A generated adventure.",
                startingGold = 50,
                startingHealth = 100
            };

            // Set starting room to first chapter's hub
            if (generatedChapters.Count > 0 && generatedChapters[0].hubLocationId != null)
            {
                config.startingRoom = generatedChapters[0].hubLocationId;
            }
            else if (generatedChapters.Count > 0 && generatedChapters[0].locationIds.Count > 0)
            {
                config.startingRoom = generatedChapters[0].locationIds[0];
            }

            return config;
        }

        /// <summary>
        /// Clear all generated content.
        /// </summary>
        private void ClearGeneratedContent()
        {
            generatedChapters.Clear();
            generatedRooms.Clear();
            generatedQuests.Clear();
            generatedEnemies.Clear();
            generatedItems.Clear();
        }

        /// <summary>
        /// Cancel ongoing generation.
        /// </summary>
        public void CancelGeneration()
        {
            wasCancelled = true;
            isGenerating = false;
            UpdateStatus("Generation cancelled.");
            Debug.Log("[WorldGen] Generation cancelled by user");
        }

        /// <summary>
        /// Update status and fire event.
        /// </summary>
        private void UpdateStatus(string status)
        {
            currentStatus = status;
            OnStatusUpdate?.Invoke(status);
            Debug.Log($"[WorldGen] {status}");
        }

        /// <summary>
        /// Load existing config from file.
        /// </summary>
        public bool LoadConfig(string storyId)
        {
            string path = Path.Combine(Application.dataPath, "Resources", "Stories", storyId, "generation_config.json");

            if (!File.Exists(path))
            {
                OnError?.Invoke($"Config not found: {path}");
                return false;
            }

            try
            {
                string json = File.ReadAllText(path);
                currentConfig = JsonUtility.FromJson<WorldGenerationConfig>(json);
                outputStoryId = storyId;
                return true;
            }
            catch (Exception e)
            {
                OnError?.Invoke($"Failed to load config: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get generation summary.
        /// </summary>
        public string GetGenerationSummary()
        {
            return $"Chapters: {generatedChapters.Count}\n" +
                   $"Rooms: {generatedRooms.Count}\n" +
                   $"Quests: {generatedQuests.Count}\n" +
                   $"Enemies: {generatedEnemies.Count}\n" +
                   $"Items: {generatedItems.Count}";
        }
    }
}
