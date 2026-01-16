using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WorldGen
{
    /// <summary>
    /// Validates cross-references between generated content.
    /// Ensures quests reference valid locations, NPCs, enemies, etc.
    /// </summary>
    public static class ContentIntegrityChecker
    {
        /// <summary>
        /// Run all integrity checks on generated content.
        /// </summary>
        public static List<string> CheckAllIntegrity(
            List<ChapterData> chapters,
            HashSet<string> roomIds,
            HashSet<string> questIds,
            HashSet<string> enemyIds)
        {
            var errors = new List<string>();

            errors.AddRange(CheckChapterIntegrity(chapters));
            errors.AddRange(CheckMainQuestChain(chapters, questIds));
            errors.AddRange(CheckLocationConnectivity(chapters, roomIds));

            return errors;
        }

        /// <summary>
        /// Verify chapter data is complete and consistent.
        /// </summary>
        public static List<string> CheckChapterIntegrity(List<ChapterData> chapters)
        {
            var errors = new List<string>();

            for (int i = 0; i < chapters.Count; i++)
            {
                var chapter = chapters[i];

                // Check chapter number sequence
                if (chapter.chapterNumber != i + 1)
                    errors.Add($"Chapter {chapter.chapterId} has incorrect number {chapter.chapterNumber}, expected {i + 1}");

                // Check required content
                if (chapter.locationIds.Count == 0)
                    errors.Add($"Chapter {chapter.chapterId} has no locations");

                if (chapter.questIds.Count == 0)
                    errors.Add($"Chapter {chapter.chapterId} has no quests");

                if (chapter.mainQuestIds.Count == 0)
                    errors.Add($"Chapter {chapter.chapterId} has no main quests (progression blocked)");

                // Verify main quests are in quest list
                foreach (var mainQuestId in chapter.mainQuestIds)
                {
                    if (!chapter.questIds.Contains(mainQuestId))
                        errors.Add($"Chapter {chapter.chapterId} main quest '{mainQuestId}' not in quest list");
                }

                // Check hub location exists
                if (!string.IsNullOrEmpty(chapter.hubLocationId) && !chapter.locationIds.Contains(chapter.hubLocationId))
                    errors.Add($"Chapter {chapter.chapterId} hub '{chapter.hubLocationId}' not in location list");
            }

            return errors;
        }

        /// <summary>
        /// Verify main quest chain allows progression through all chapters.
        /// </summary>
        public static List<string> CheckMainQuestChain(List<ChapterData> chapters, HashSet<string> allQuestIds)
        {
            var errors = new List<string>();

            for (int i = 1; i < chapters.Count; i++)
            {
                var chapter = chapters[i];
                var prevChapter = chapters[i - 1];

                // Check unlock quest exists
                if (string.IsNullOrEmpty(chapter.unlockQuestId))
                {
                    errors.Add($"Chapter {chapter.chapterId} has no unlock quest defined");
                    continue;
                }

                // Unlock quest should be from previous chapter
                if (!prevChapter.mainQuestIds.Contains(chapter.unlockQuestId))
                    errors.Add($"Chapter {chapter.chapterId} unlock quest '{chapter.unlockQuestId}' is not a main quest from previous chapter");

                // Verify quest exists
                if (!allQuestIds.Contains(chapter.unlockQuestId))
                    errors.Add($"Chapter {chapter.chapterId} unlock quest '{chapter.unlockQuestId}' not found");
            }

            return errors;
        }

        /// <summary>
        /// Verify all locations have valid connections (no orphaned locations).
        /// </summary>
        public static List<string> CheckLocationConnectivity(List<ChapterData> chapters, HashSet<string> allRoomIds)
        {
            var errors = new List<string>();

            foreach (var chapter in chapters)
            {
                // All chapter locations should exist
                foreach (var locationId in chapter.locationIds)
                {
                    if (!allRoomIds.Contains(locationId))
                        errors.Add($"Chapter {chapter.chapterId} references non-existent location: {locationId}");
                }

                // Entry/exit locations should exist
                if (!string.IsNullOrEmpty(chapter.entryLocationId) && !allRoomIds.Contains(chapter.entryLocationId))
                    errors.Add($"Chapter {chapter.chapterId} entry location '{chapter.entryLocationId}' not found");

                if (!string.IsNullOrEmpty(chapter.exitLocationId) && !allRoomIds.Contains(chapter.exitLocationId))
                    errors.Add($"Chapter {chapter.chapterId} exit location '{chapter.exitLocationId}' not found");
            }

            return errors;
        }

        /// <summary>
        /// Check quest objectives reference valid targets.
        /// </summary>
        public static List<string> CheckQuestReferences(
            List<QuestData> quests,
            HashSet<string> roomIds,
            HashSet<string> npcIds,
            HashSet<string> enemyIds,
            HashSet<string> itemIds)
        {
            var errors = new List<string>();

            foreach (var quest in quests)
            {
                // Check quest giver location
                if (!string.IsNullOrEmpty(quest.questGiverLocation) && !roomIds.Contains(quest.questGiverLocation))
                    errors.Add($"Quest '{quest.questId}' giver location '{quest.questGiverLocation}' not found");

                // Check objectives
                foreach (var obj in quest.objectives)
                {
                    switch (obj.type)
                    {
                        case ObjectiveType.GoToRoom:
                            if (!roomIds.Contains(obj.targetId))
                                errors.Add($"Quest '{quest.questId}' objective references invalid room: {obj.targetId}");
                            break;

                        case ObjectiveType.TalkToNPC:
                            if (npcIds.Count > 0 && !npcIds.Contains(obj.targetId))
                                errors.Add($"Quest '{quest.questId}' objective references unknown NPC: {obj.targetId}");
                            break;

                        case ObjectiveType.DefeatEnemy:
                        case ObjectiveType.DefeatCount:
                            if (!enemyIds.Contains(obj.targetId))
                                errors.Add($"Quest '{quest.questId}' objective references invalid enemy: {obj.targetId}");
                            break;

                        case ObjectiveType.CollectItem:
                        case ObjectiveType.DeliverItem:
                        case ObjectiveType.UseItem:
                            if (itemIds.Count > 0 && !itemIds.Contains(obj.targetId))
                                errors.Add($"Quest '{quest.questId}' objective references unknown item: {obj.targetId}");
                            break;
                    }
                }

                // Check reveal locations exist
                foreach (var locationId in quest.revealsOnAccept)
                {
                    if (!roomIds.Contains(locationId))
                        errors.Add($"Quest '{quest.questId}' revealsOnAccept references invalid location: {locationId}");
                }

                foreach (var locationId in quest.revealsOnComplete)
                {
                    if (!roomIds.Contains(locationId))
                        errors.Add($"Quest '{quest.questId}' revealsOnComplete references invalid location: {locationId}");
                }

                // Check prerequisite quests exist
                foreach (var prereqId in quest.prerequisiteQuests)
                {
                    if (!quests.Exists(q => q.questId == prereqId))
                        errors.Add($"Quest '{quest.questId}' prerequisite quest not found: {prereqId}");
                }
            }

            return errors;
        }

        /// <summary>
        /// Check that difficulty scaling is appropriate.
        /// </summary>
        public static List<string> CheckDifficultyScaling(List<ChapterData> chapters, List<QuestData> quests)
        {
            var warnings = new List<string>();

            foreach (var chapter in chapters)
            {
                var chapterQuests = quests.Where(q => q.chapterNumber == chapter.chapterNumber).ToList();

                foreach (var quest in chapterQuests)
                {
                    // Check difficulty is within expected range for chapter
                    int expectedMin = chapter.baseDifficulty - 2;
                    int expectedMax = chapter.baseDifficulty + 3; // Allow some variance for hard side quests

                    if (quest.difficulty < expectedMin)
                        warnings.Add($"Quest '{quest.questId}' difficulty {quest.difficulty} is below chapter {chapter.chapterNumber} minimum ({expectedMin})");

                    if (quest.difficulty > expectedMax && quest.questType == QuestType.Main)
                        warnings.Add($"Main quest '{quest.questId}' difficulty {quest.difficulty} exceeds chapter {chapter.chapterNumber} maximum ({expectedMax})");
                }
            }

            return warnings;
        }

        /// <summary>
        /// Check that player can always progress (non-restrictive locking).
        /// </summary>
        public static List<string> CheckProgressionPath(List<ChapterData> chapters, List<QuestData> quests)
        {
            var errors = new List<string>();

            foreach (var chapter in chapters)
            {
                // Get main quests for this chapter
                var mainQuests = quests.Where(q =>
                    chapter.mainQuestIds.Contains(q.questId)).ToList();

                if (mainQuests.Count == 0)
                {
                    errors.Add($"Chapter {chapter.chapterNumber} has no main quests - progression blocked");
                    continue;
                }

                // Check main quests don't have circular prerequisites
                var visited = new HashSet<string>();
                foreach (var quest in mainQuests)
                {
                    if (HasCircularPrerequisite(quest, quests, visited, new HashSet<string>()))
                        errors.Add($"Quest '{quest.questId}' has circular prerequisites");
                }

                // Check at least one main quest is accessible without completing other main quests
                bool hasAccessibleMainQuest = mainQuests.Any(q =>
                    q.prerequisiteQuests.Count == 0 ||
                    !q.prerequisiteQuests.Any(p => chapter.mainQuestIds.Contains(p)));

                if (!hasAccessibleMainQuest)
                    errors.Add($"Chapter {chapter.chapterNumber} has no initially accessible main quest");
            }

            return errors;
        }

        /// <summary>
        /// Check for circular prerequisite dependencies.
        /// </summary>
        private static bool HasCircularPrerequisite(
            QuestData quest,
            List<QuestData> allQuests,
            HashSet<string> globalVisited,
            HashSet<string> pathVisited)
        {
            if (pathVisited.Contains(quest.questId))
                return true; // Circular!

            if (globalVisited.Contains(quest.questId))
                return false; // Already checked, no cycle

            pathVisited.Add(quest.questId);
            globalVisited.Add(quest.questId);

            foreach (var prereqId in quest.prerequisiteQuests)
            {
                var prereqQuest = allQuests.Find(q => q.questId == prereqId);
                if (prereqQuest != null)
                {
                    if (HasCircularPrerequisite(prereqQuest, allQuests, globalVisited, new HashSet<string>(pathVisited)))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Generate a summary report of content.
        /// </summary>
        public static string GenerateContentReport(
            List<ChapterData> chapters,
            int roomCount,
            int questCount,
            int enemyCount,
            int itemCount)
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("=== GENERATED CONTENT REPORT ===");
            sb.AppendLine();
            sb.AppendLine($"Total Chapters: {chapters.Count}");
            sb.AppendLine($"Total Rooms: {roomCount}");
            sb.AppendLine($"Total Quests: {questCount}");
            sb.AppendLine($"Total Enemies: {enemyCount}");
            sb.AppendLine($"Total Items: {itemCount}");
            sb.AppendLine();

            foreach (var chapter in chapters)
            {
                sb.AppendLine($"--- Chapter {chapter.chapterNumber}: {chapter.chapterName} ---");
                sb.AppendLine($"  Locations: {chapter.locationIds.Count}");
                sb.AppendLine($"  Quests: {chapter.questIds.Count} ({chapter.mainQuestIds.Count} main)");
                sb.AppendLine($"  Enemies: {chapter.enemyIds.Count}");
                sb.AppendLine($"  Difficulty: {chapter.baseDifficulty}");

                if (chapter.validationErrors.Count > 0)
                {
                    sb.AppendLine($"  ERRORS: {chapter.validationErrors.Count}");
                    foreach (var error in chapter.validationErrors)
                        sb.AppendLine($"    - {error}");
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
