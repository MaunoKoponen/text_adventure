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
        private Dictionary<string, RoomGraph> generatedRoomGraphs = new Dictionary<string, RoomGraph>();  // chapterId -> RoomGraph
        private Dictionary<string, string> generatedRooms = new Dictionary<string, string>();      // roomId -> JSON
        private Dictionary<string, string> generatedQuests = new Dictionary<string, string>();    // questId -> JSON
        private Dictionary<string, string> generatedEnemies = new Dictionary<string, string>();   // enemyId -> JSON
        private Dictionary<string, string> generatedItems = new Dictionary<string, string>();     // itemId -> JSON

        // Cached outline data for cross-referencing during room generation
        private Dictionary<string, ChapterOutline> chapterOutlines = new Dictionary<string, ChapterOutline>();

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
                string cleanedOutline = JsonValidator.CleanJson(outlineResponse.content);
                outline = JsonUtility.FromJson<ChapterOutline>(cleanedOutline);
            }
            catch (Exception e)
            {
                OnError?.Invoke($"Failed to parse chapter outline: {e.Message}\nResponse: {outlineResponse.content}");
                isGenerating = false;
                yield break;
            }

            OnChapterOutlineGenerated?.Invoke(outline);

            // Cache outline for room generation
            chapterOutlines[outline.chapterId] = outline;

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

            // Step 2: Generate room connectivity graph FIRST
            UpdateStatus($"Chapter {chapterNumber}: Generating room graph...");

            RoomGraph roomGraph = null;
            yield return StartCoroutine(GenerateRoomGraphCoroutine(outline, chapter, systemPrompt, g => roomGraph = g));

            if (roomGraph == null)
            {
                OnError?.Invoke($"Failed to generate room graph for chapter {chapterNumber}");
                isGenerating = false;
                yield break;
            }

            // Validate room graph connectivity
            var graphErrors = roomGraph.ValidateConnections();
            if (graphErrors.Count > 0)
            {
                Debug.LogWarning($"[WorldGen] Room graph has {graphErrors.Count} connection errors:");
                foreach (var error in graphErrors)
                {
                    Debug.LogWarning($"  - {error}");
                }
            }

            generatedRoomGraphs[chapter.chapterId] = roomGraph;
            chapter.hubLocationId = roomGraph.hubRoomId;
            chapter.entryLocationId = roomGraph.entryRoomId;
            chapter.exitLocationId = roomGraph.exitRoomId;

            // Step 3: Generate individual rooms from the graph
            UpdateStatus($"Chapter {chapterNumber}: Generating {roomGraph.rooms.Count} rooms from graph...");

            int roomIndex = 0;
            foreach (var roomNode in roomGraph.rooms)
            {
                roomIndex++;
                UpdateStatus($"Chapter {chapterNumber}: Room {roomIndex}/{roomGraph.rooms.Count} - {roomNode.roomName} ({roomNode.roomType})");

                string roomPrompt = GetRoomPromptByType(roomNode, roomGraph, chapter, outline);

                LLMResponse roomResponse = null;
                yield return StartCoroutine(SendRequestAndWait(roomPrompt, systemPrompt, r => roomResponse = r));

                if (roomResponse.success)
                {
                    generatedRooms[roomNode.roomId] = roomResponse.content;
                    chapter.locationIds.Add(roomNode.roomId);
                }
                else
                {
                    Debug.LogWarning($"Failed to generate room {roomNode.roomId}: {roomResponse.error}");
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
        /// Generate the room connectivity graph for a chapter.
        /// This defines all rooms and their connections before content is generated.
        /// </summary>
        private IEnumerator GenerateRoomGraphCoroutine(ChapterOutline outline, ChapterData chapter,
            string systemPrompt, Action<RoomGraph> callback)
        {
            string graphPrompt = PromptTemplates.GetRoomGraphPrompt(outline, currentConfig.worldPrompt, currentConfig.settings);

            LLMResponse graphResponse = null;
            yield return StartCoroutine(SendRequestAndWait(graphPrompt, systemPrompt, r => graphResponse = r));

            if (!graphResponse.success)
            {
                Debug.LogError($"Failed to generate room graph: {graphResponse.error}");
                callback(null);
                yield break;
            }

            RoomGraph graph = null;
            try
            {
                string cleanedGraph = JsonValidator.CleanJson(graphResponse.content);
                graph = JsonUtility.FromJson<RoomGraph>(cleanedGraph);

                // Ensure bidirectional connections
                EnsureBidirectionalConnections(graph);

                Debug.Log($"[WorldGen] Room graph generated with {graph.rooms.Count} rooms");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse room graph: {e.Message}\nResponse: {graphResponse.content}");
                callback(null);
                yield break;
            }

            callback(graph);
        }

        /// <summary>
        /// Ensure all room connections are bidirectional.
        /// If room A connects to B, B must connect to A.
        /// </summary>
        private void EnsureBidirectionalConnections(RoomGraph graph)
        {
            foreach (var room in graph.rooms)
            {
                foreach (var targetId in room.connectsTo)
                {
                    var targetRoom = graph.GetRoom(targetId);
                    if (targetRoom != null && !targetRoom.connectsTo.Contains(room.roomId))
                    {
                        targetRoom.connectsTo.Add(room.roomId);
                        Debug.Log($"[WorldGen] Added bidirectional connection: {targetId} -> {room.roomId}");
                    }
                }
            }
        }

        /// <summary>
        /// Get the appropriate room prompt based on room type.
        /// </summary>
        private string GetRoomPromptByType(RoomNode roomNode, RoomGraph graph, ChapterData chapter, ChapterOutline outline)
        {
            switch (roomNode.roomType)
            {
                case RoomTypes.Crossroad:
                    return PromptTemplates.GetCrossroadRoomPrompt(roomNode, graph, chapter, currentConfig.worldPrompt);

                case RoomTypes.Interaction:
                    // Get NPCs for this room from the outline
                    var npcsInRoom = new List<NPCSummary>();
                    if (outline != null)
                    {
                        foreach (var npcId in roomNode.npcs)
                        {
                            var npc = outline.keyNPCs.Find(n => n.npcId == npcId);
                            if (npc != null)
                            {
                                npcsInRoom.Add(npc);
                            }
                        }
                    }
                    return PromptTemplates.GetInteractionRoomPrompt(roomNode, graph, chapter, currentConfig.worldPrompt, npcsInRoom);

                case RoomTypes.Combat:
                    // Get enemy for this room from the outline
                    EnemySummary enemy = null;
                    if (outline != null && !string.IsNullOrEmpty(roomNode.enemyId))
                    {
                        enemy = outline.enemies.Find(e => e.enemyId == roomNode.enemyId);
                    }
                    return PromptTemplates.GetCombatRoomPrompt(roomNode, graph, chapter, currentConfig.worldPrompt, enemy);

                default:
                    // Fall back to crossroad for unknown types
                    Debug.LogWarning($"Unknown room type '{roomNode.roomType}' for {roomNode.roomId}, defaulting to crossroad");
                    return PromptTemplates.GetCrossroadRoomPrompt(roomNode, graph, chapter, currentConfig.worldPrompt);
            }
        }

        /// <summary>
        /// Validate all generated content.
        /// </summary>
        private IEnumerator ValidateAllContent()
        {
            Debug.Log("[WorldGen] Starting validation...");
            var errors = new List<string>();

            // Build set of all known room IDs from room graphs
            var knownRoomIds = new HashSet<string>();
            foreach (var graph in generatedRoomGraphs.Values)
            {
                foreach (var roomId in graph.GetAllRoomIds())
                {
                    knownRoomIds.Add(roomId);
                }
            }
            Debug.Log($"[WorldGen] Known room IDs from graphs: {knownRoomIds.Count}");

            // Validate room graphs first
            Debug.Log($"[WorldGen] Validating {generatedRoomGraphs.Count} room graphs...");
            foreach (var kvp in generatedRoomGraphs)
            {
                try
                {
                    var result = JsonValidator.ValidateRoomGraph(kvp.Value);
                    if (!result.isValid)
                    {
                        errors.AddRange(result.errors.ConvertAll(e => $"RoomGraph {kvp.Key}: {e}"));
                    }
                    foreach (var warning in result.warnings)
                    {
                        Debug.LogWarning($"[WorldGen] RoomGraph {kvp.Key}: {warning}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[WorldGen] Room graph validation error for {kvp.Key}: {e.Message}");
                    errors.Add($"RoomGraph {kvp.Key}: validation exception - {e.Message}");
                }
            }

            yield return null;

            // Validate rooms with known room IDs for exit validation
            Debug.Log($"[WorldGen] Validating {generatedRooms.Count} rooms...");
            foreach (var kvp in generatedRooms)
            {
                try
                {
                    var result = JsonValidator.ValidateRoom(kvp.Value, knownRoomIds);
                    if (!result.isValid)
                    {
                        errors.AddRange(result.errors.ConvertAll(e => $"Room {kvp.Key}: {e}"));
                    }
                    foreach (var warning in result.warnings)
                    {
                        Debug.LogWarning($"[WorldGen] Room {kvp.Key}: {warning}");
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

            // Save room graphs
            Debug.Log($"[WorldGen] Saving {generatedRoomGraphs.Count} room graphs...");
            foreach (var kvp in generatedRoomGraphs)
            {
                try
                {
                    string graphJson = JsonUtility.ToJson(kvp.Value, true);
                    string graphPath = Path.Combine(basePath, "Chapters", $"{kvp.Key}_room_graph.json");
                    File.WriteAllText(graphPath, graphJson);
                    Debug.Log($"[WorldGen] Saved room graph: {kvp.Key}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[WorldGen] Failed to save room graph {kvp.Key}: {e}");
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
            generatedRoomGraphs.Clear();
            chapterOutlines.Clear();
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
