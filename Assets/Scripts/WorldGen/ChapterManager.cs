using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace WorldGen
{
    /// <summary>
    /// Manages chapter progression, unlocking, and state tracking.
    /// Integrates with QuestManager to unlock chapters when main quests complete.
    /// </summary>
    public class ChapterManager : MonoBehaviour
    {
        public static ChapterManager Instance { get; private set; }

        [Header("Configuration")]
        [Tooltip("The story folder to load chapters from")]
        public string currentStoryId = "generated_world";

        // Loaded chapter data
        private Dictionary<string, ChapterData> allChapters = new Dictionary<string, ChapterData>();
        private List<ChapterData> chapterOrder = new List<ChapterData>(); // Ordered by chapter number

        // Chapter state
        private HashSet<string> unlockedChapterIds = new HashSet<string>();
        private string currentChapterId;

        // Events
        public event Action<ChapterData> OnChapterUnlocked;
        public event Action<ChapterData> OnChapterChanged;
        public event Action OnChaptersLoaded;

        // Properties
        public ChapterData CurrentChapter => GetChapter(currentChapterId);
        public int CurrentChapterNumber => CurrentChapter?.chapterNumber ?? 1;
        public IReadOnlyList<ChapterData> AllChapters => chapterOrder;

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
            // Subscribe to quest completion events
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnMainQuestCompleted += HandleMainQuestCompleted;
                QuestManager.Instance.OnQuestCompleted += HandleQuestCompleted;
            }
        }

        private void OnDestroy()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnMainQuestCompleted -= HandleMainQuestCompleted;
                QuestManager.Instance.OnQuestCompleted -= HandleQuestCompleted;
            }
        }

        /// <summary>
        /// Handle main quest completion - this is the primary trigger for chapter unlocks.
        /// </summary>
        private void HandleMainQuestCompleted(QuestData quest)
        {
            Debug.Log($"[ChapterManager] Main quest completed: {quest.questName} (Chapter {quest.chapterNumber})");
            CheckChapterUnlock(quest);
        }

        /// <summary>
        /// Load all chapters for a story.
        /// </summary>
        public void LoadChapters(string storyId)
        {
            currentStoryId = storyId;
            allChapters.Clear();
            chapterOrder.Clear();

            string chaptersPath = $"Stories/{storyId}/Chapters";
            var chapterFiles = Resources.LoadAll<TextAsset>(chaptersPath);

            if (chapterFiles.Length == 0)
            {
                Debug.LogWarning($"No chapters found at: {chaptersPath}");
                return;
            }

            foreach (var file in chapterFiles)
            {
                try
                {
                    var chapter = JsonUtility.FromJson<ChapterData>(file.text);
                    if (chapter != null && !string.IsNullOrEmpty(chapter.chapterId))
                    {
                        allChapters[chapter.chapterId] = chapter;
                        chapterOrder.Add(chapter);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to parse chapter {file.name}: {e.Message}");
                }
            }

            // Sort by chapter number
            chapterOrder.Sort((a, b) => a.chapterNumber.CompareTo(b.chapterNumber));

            // Unlock first chapter by default
            if (chapterOrder.Count > 0)
            {
                UnlockChapter(chapterOrder[0].chapterId);
                currentChapterId = chapterOrder[0].chapterId;
            }

            Debug.Log($"[ChapterManager] Loaded {allChapters.Count} chapters from {storyId}");
            OnChaptersLoaded?.Invoke();
        }

        /// <summary>
        /// Get chapter data by ID.
        /// </summary>
        public ChapterData GetChapter(string chapterId)
        {
            if (string.IsNullOrEmpty(chapterId)) return null;
            allChapters.TryGetValue(chapterId, out ChapterData chapter);
            return chapter;
        }

        /// <summary>
        /// Get chapter by number (1-indexed).
        /// </summary>
        public ChapterData GetChapterByNumber(int chapterNumber)
        {
            return chapterOrder.Find(c => c.chapterNumber == chapterNumber);
        }

        /// <summary>
        /// Check if a chapter is unlocked.
        /// </summary>
        public bool IsChapterUnlocked(string chapterId)
        {
            return unlockedChapterIds.Contains(chapterId);
        }

        /// <summary>
        /// Check if a chapter is unlocked by number.
        /// </summary>
        public bool IsChapterUnlocked(int chapterNumber)
        {
            var chapter = GetChapterByNumber(chapterNumber);
            return chapter != null && IsChapterUnlocked(chapter.chapterId);
        }

        /// <summary>
        /// Unlock a chapter.
        /// </summary>
        public void UnlockChapter(string chapterId)
        {
            if (unlockedChapterIds.Contains(chapterId)) return;

            var chapter = GetChapter(chapterId);
            if (chapter == null)
            {
                Debug.LogWarning($"Cannot unlock unknown chapter: {chapterId}");
                return;
            }

            unlockedChapterIds.Add(chapterId);
            Debug.Log($"[ChapterManager] Chapter unlocked: {chapter.chapterName}");

            // Set player flag for chapter unlock
            RoomManager.playerData?.SetFlag($"chapter_{chapterId}_unlocked", "true");
            RoomManager.playerData?.SetFlag($"chapter_{chapter.chapterNumber}_unlocked", "true");

            OnChapterUnlocked?.Invoke(chapter);
            SaveProgress();
        }

        /// <summary>
        /// Set the current active chapter.
        /// </summary>
        public void SetCurrentChapter(string chapterId)
        {
            if (!IsChapterUnlocked(chapterId))
            {
                Debug.LogWarning($"Cannot set locked chapter as current: {chapterId}");
                return;
            }

            if (currentChapterId == chapterId) return;

            currentChapterId = chapterId;
            var chapter = GetChapter(chapterId);

            Debug.Log($"[ChapterManager] Current chapter changed to: {chapter?.chapterName}");
            OnChapterChanged?.Invoke(chapter);
            SaveProgress();
        }

        /// <summary>
        /// Handle any quest completion (for general tracking).
        /// </summary>
        private void HandleQuestCompleted(QuestData quest)
        {
            // General quest completion handling (if needed)
            // Main quest unlocks are handled by HandleMainQuestCompleted
        }

        /// <summary>
        /// Check if a quest completion should unlock a new chapter.
        /// </summary>
        private void CheckChapterUnlock(QuestData quest)
        {
            if (quest == null) return;

            // Check if this quest unlocks any chapter
            foreach (var chapter in chapterOrder)
            {
                if (chapter.unlockQuestId == quest.questId)
                {
                    // Find the next chapter to unlock
                    int nextChapterNum = chapter.chapterNumber + 1;
                    var nextChapter = GetChapterByNumber(nextChapterNum);

                    if (nextChapter != null && !IsChapterUnlocked(nextChapter.chapterId))
                    {
                        UnlockChapter(nextChapter.chapterId);
                        Debug.Log($"[ChapterManager] Quest '{quest.questName}' unlocked chapter: {nextChapter.chapterName}");
                    }
                    break;
                }
            }

            // Also check by chapter number - if all main quests in a chapter are done, unlock next
            var currentChapter = GetChapterByNumber(quest.chapterNumber);
            if (currentChapter != null)
            {
                bool allMainQuestsDone = true;
                foreach (var mainQuestId in currentChapter.mainQuestIds)
                {
                    var flags = RoomManager.playerData?.Flags;
                    if (flags == null || !flags.TryGetValue(mainQuestId, out string state) || state != "concluded")
                    {
                        allMainQuestsDone = false;
                        break;
                    }
                }

                if (allMainQuestsDone)
                {
                    int nextChapterNum = currentChapter.chapterNumber + 1;
                    var nextChapter = GetChapterByNumber(nextChapterNum);
                    if (nextChapter != null && !IsChapterUnlocked(nextChapter.chapterId))
                    {
                        UnlockChapter(nextChapter.chapterId);
                        Debug.Log($"[ChapterManager] All main quests complete in {currentChapter.chapterName}, unlocked: {nextChapter.chapterName}");
                    }
                }
            }
        }

        /// <summary>
        /// Get all locations for unlocked chapters.
        /// </summary>
        public List<string> GetUnlockedLocationIds()
        {
            var locations = new List<string>();

            foreach (var chapterId in unlockedChapterIds)
            {
                var chapter = GetChapter(chapterId);
                if (chapter != null)
                {
                    locations.AddRange(chapter.locationIds);
                }
            }

            return locations;
        }

        /// <summary>
        /// Get all locations for a specific chapter.
        /// </summary>
        public List<string> GetChapterLocationIds(string chapterId)
        {
            var chapter = GetChapter(chapterId);
            return chapter?.locationIds ?? new List<string>();
        }

        /// <summary>
        /// Check if a location belongs to an unlocked chapter.
        /// </summary>
        public bool IsLocationInUnlockedChapter(string locationId)
        {
            foreach (var chapterId in unlockedChapterIds)
            {
                var chapter = GetChapter(chapterId);
                if (chapter != null && chapter.locationIds.Contains(locationId))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Get the chapter a location belongs to.
        /// </summary>
        public ChapterData GetChapterForLocation(string locationId)
        {
            foreach (var chapter in chapterOrder)
            {
                if (chapter.locationIds.Contains(locationId))
                {
                    return chapter;
                }
            }
            return null;
        }

        /// <summary>
        /// Get all main quests for a chapter.
        /// </summary>
        public List<string> GetMainQuestIds(string chapterId)
        {
            var chapter = GetChapter(chapterId);
            return chapter?.mainQuestIds ?? new List<string>();
        }

        /// <summary>
        /// Get all quests for a chapter.
        /// </summary>
        public List<string> GetAllQuestIds(string chapterId)
        {
            var chapter = GetChapter(chapterId);
            return chapter?.questIds ?? new List<string>();
        }

        /// <summary>
        /// Get all unlocked chapters.
        /// </summary>
        public List<ChapterData> GetUnlockedChapters()
        {
            var unlocked = new List<ChapterData>();
            foreach (var chapterId in unlockedChapterIds)
            {
                var chapter = GetChapter(chapterId);
                if (chapter != null)
                {
                    unlocked.Add(chapter);
                }
            }
            unlocked.Sort((a, b) => a.chapterNumber.CompareTo(b.chapterNumber));
            return unlocked;
        }

        /// <summary>
        /// Get chapter completion percentage.
        /// </summary>
        public float GetChapterProgress(string chapterId)
        {
            var chapter = GetChapter(chapterId);
            if (chapter == null) return 0f;

            if (chapter.mainQuestIds.Count == 0) return 1f;

            int completed = 0;
            foreach (var questId in chapter.mainQuestIds)
            {
                var flags = RoomManager.playerData?.Flags;
                if (flags != null && flags.TryGetValue(questId, out string state) && state == "concluded")
                {
                    completed++;
                }
            }

            return (float)completed / chapter.mainQuestIds.Count;
        }

        /// <summary>
        /// Save chapter progress.
        /// </summary>
        private void SaveProgress()
        {
            var saveData = new ChapterSaveData
            {
                storyId = currentStoryId,
                currentChapterId = currentChapterId,
                unlockedChapterIds = new List<string>(unlockedChapterIds)
            };

            string json = JsonUtility.ToJson(saveData);
            PlayerPrefs.SetString("ChapterManagerData", json);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Load chapter progress.
        /// </summary>
        public void LoadProgress()
        {
            string json = PlayerPrefs.GetString("ChapterManagerData", "");
            if (string.IsNullOrEmpty(json)) return;

            try
            {
                var saveData = JsonUtility.FromJson<ChapterSaveData>(json);

                if (saveData.storyId == currentStoryId)
                {
                    currentChapterId = saveData.currentChapterId;
                    unlockedChapterIds = new HashSet<string>(saveData.unlockedChapterIds);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to load chapter progress: {e.Message}");
            }
        }

        /// <summary>
        /// Reset progress for new game.
        /// </summary>
        public void ResetProgress()
        {
            unlockedChapterIds.Clear();
            currentChapterId = null;

            // Unlock first chapter
            if (chapterOrder.Count > 0)
            {
                UnlockChapter(chapterOrder[0].chapterId);
                currentChapterId = chapterOrder[0].chapterId;
            }

            PlayerPrefs.DeleteKey("ChapterManagerData");
        }
    }

    /// <summary>
    /// Save data for chapter progress.
    /// </summary>
    [Serializable]
    public class ChapterSaveData
    {
        public string storyId;
        public string currentChapterId;
        public List<string> unlockedChapterIds;
    }
}
