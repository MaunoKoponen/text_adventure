using System;
using System.Collections.Generic;
using UnityEngine;

namespace WorldGen
{
    /// <summary>
    /// Validates generated JSON content against expected schemas.
    /// </summary>
    public static class JsonValidator
    {
        /// <summary>
        /// Validation result with errors and warnings.
        /// </summary>
        public class ValidationResult
        {
            public bool isValid => errors.Count == 0;
            public List<string> errors = new List<string>();
            public List<string> warnings = new List<string>();
            public object parsedObject;

            public void AddError(string error) => errors.Add(error);
            public void AddWarning(string warning) => warnings.Add(warning);
        }

        /// <summary>
        /// Validate room/location JSON.
        /// </summary>
        public static ValidationResult ValidateRoom(string json)
        {
            return ValidateRoom(json, null);
        }

        /// <summary>
        /// Validate room/location JSON with optional known room IDs for exit validation.
        /// </summary>
        public static ValidationResult ValidateRoom(string json, HashSet<string> knownRoomIds)
        {
            var result = new ValidationResult();

            if (string.IsNullOrEmpty(json))
            {
                result.AddError("JSON is empty");
                return result;
            }

            try
            {
                // Try to parse as Room structure
                var room = JsonUtility.FromJson<RoomValidation>(json);

                // Required fields
                if (string.IsNullOrEmpty(room.room_id))
                    result.AddError("room_id is required");

                if (string.IsNullOrEmpty(room.description))
                    result.AddError("description is required");

                // Validate room type
                if (!string.IsNullOrEmpty(room.room_type))
                {
                    var validTypes = new HashSet<string> { RoomTypes.Crossroad, RoomTypes.Interaction, RoomTypes.Combat };
                    if (!validTypes.Contains(room.room_type))
                    {
                        result.AddWarning($"Unknown room_type: '{room.room_type}' (expected: crossroad, interaction, combat)");
                    }
                }

                // Validate exits
                if (room.exits != null)
                {
                    foreach (var exit in room.exits)
                    {
                        if (string.IsNullOrEmpty(exit.leads_to))
                            result.AddError($"Exit '{exit.exit_name}' missing leads_to");
                        else if (knownRoomIds != null && !knownRoomIds.Contains(exit.leads_to))
                            result.AddError($"Exit '{exit.exit_name}' leads to unknown room: '{exit.leads_to}'");
                    }
                }
                else
                {
                    result.AddWarning("Room has no exits defined");
                }

                // Build sets for action_id / npc_name matching
                var actionIds = new HashSet<string>();
                var dialogueNpcNames = new HashSet<string>();

                // Collect action IDs
                if (room.actions != null)
                {
                    foreach (var action in room.actions)
                    {
                        if (!string.IsNullOrEmpty(action.action_id))
                        {
                            actionIds.Add(action.action_id);
                        }
                        else
                        {
                            result.AddWarning("Action missing action_id");
                        }
                    }
                }

                // Collect dialogue npc_names
                if (room.dialogues != null)
                {
                    foreach (var dialogue in room.dialogues)
                    {
                        if (!string.IsNullOrEmpty(dialogue.npc_name))
                        {
                            dialogueNpcNames.Add(dialogue.npc_name);
                        }
                        ValidateDialogue(dialogue, result);
                    }
                }

                // CRITICAL: Validate action_id / npc_name matching
                // For interaction rooms, every action_id should have a matching dialogue npc_name
                if (room.room_type == RoomTypes.Interaction ||
                    (string.IsNullOrEmpty(room.room_type) && actionIds.Count > 0 && dialogueNpcNames.Count > 0))
                {
                    // Check each action_id has a matching dialogue
                    foreach (var actionId in actionIds)
                    {
                        if (!dialogueNpcNames.Contains(actionId))
                        {
                            result.AddError($"action_id '{actionId}' has no matching dialogue npc_name. " +
                                "CRITICAL: action_id MUST exactly match a dialogue npc_name for the dialogue to trigger!");
                        }
                    }

                    // Check each dialogue has a matching action (optional but recommended)
                    foreach (var npcName in dialogueNpcNames)
                    {
                        if (!actionIds.Contains(npcName))
                        {
                            result.AddWarning($"dialogue npc_name '{npcName}' has no matching action_id. " +
                                "Player cannot trigger this dialogue!");
                        }
                    }
                }

                // Validate crossroad rooms should not have dialogues
                if (room.room_type == RoomTypes.Crossroad)
                {
                    if (room.dialogues != null && room.dialogues.Length > 0)
                    {
                        result.AddWarning("Crossroad room should not have dialogues");
                    }
                    if (room.actions != null && room.actions.Length > 0)
                    {
                        result.AddWarning("Crossroad room should not have actions");
                    }
                }

                result.parsedObject = room;
            }
            catch (Exception e)
            {
                result.AddError($"JSON parse error: {e.Message}");

                // Try to identify common issues
                if (json.Contains("```"))
                    result.AddError("JSON contains markdown code blocks - LLM should output raw JSON only");
            }

            return result;
        }

        /// <summary>
        /// Validate quest JSON.
        /// </summary>
        public static ValidationResult ValidateQuest(string json)
        {
            var result = new ValidationResult();

            if (string.IsNullOrEmpty(json))
            {
                result.AddError("JSON is empty");
                return result;
            }

            try
            {
                var quest = JsonUtility.FromJson<QuestData>(json);

                // Required fields
                if (string.IsNullOrEmpty(quest.questId))
                    result.AddError("questId is required");

                if (string.IsNullOrEmpty(quest.questName))
                    result.AddError("questName is required");

                if (quest.objectives == null || quest.objectives.Count == 0)
                    result.AddError("At least one objective is required");

                // Validate objectives
                if (quest.objectives != null)
                {
                    for (int i = 0; i < quest.objectives.Count; i++)
                    {
                        var obj = quest.objectives[i];

                        if (string.IsNullOrEmpty(obj.objectiveId))
                            result.AddError($"Objective {i} missing objectiveId");

                        if (string.IsNullOrEmpty(obj.description))
                            result.AddWarning($"Objective {i} missing description");

                        if (string.IsNullOrEmpty(obj.targetId))
                            result.AddError($"Objective {i} missing targetId");

                        // Validate objective type is valid
                        if (!Enum.IsDefined(typeof(ObjectiveType), obj.type))
                            result.AddError($"Objective {i} has invalid type: {obj.type}");
                    }
                }

                // Validate rewards
                if (quest.rewards == null)
                    result.AddWarning("Quest has no rewards defined");

                result.parsedObject = quest;
            }
            catch (Exception e)
            {
                result.AddError($"JSON parse error: {e.Message}");

                if (json.Contains("```"))
                    result.AddError("JSON contains markdown code blocks");
            }

            return result;
        }

        /// <summary>
        /// Validate enemy JSON.
        /// </summary>
        public static ValidationResult ValidateEnemy(string json)
        {
            var result = new ValidationResult();

            if (string.IsNullOrEmpty(json))
            {
                result.AddError("JSON is empty");
                return result;
            }

            try
            {
                var enemy = JsonUtility.FromJson<EnemyValidation>(json);

                // Required fields
                if (string.IsNullOrEmpty(enemy.enemyId))
                    result.AddError("enemyId is required");

                if (string.IsNullOrEmpty(enemy.enemyName))
                    result.AddError("enemyName is required");

                if (enemy.maxHitPoints <= 0)
                    result.AddError("maxHitPoints must be positive");

                if (enemy.armorClass < 0)
                    result.AddWarning("armorClass is negative");

                // Validate attacks
                if (enemy.attacks == null || enemy.attacks.Length == 0)
                    result.AddWarning("Enemy has no attacks defined");
                else
                {
                    foreach (var attack in enemy.attacks)
                    {
                        if (string.IsNullOrEmpty(attack.attackName))
                            result.AddWarning("Attack missing name");

                        if (attack.damageMax < attack.damageMin)
                            result.AddError($"Attack '{attack.attackName}' has damageMax < damageMin");
                    }
                }

                result.parsedObject = enemy;
            }
            catch (Exception e)
            {
                result.AddError($"JSON parse error: {e.Message}");
            }

            return result;
        }

        /// <summary>
        /// Validate item JSON.
        /// </summary>
        public static ValidationResult ValidateItem(string json)
        {
            var result = new ValidationResult();

            if (string.IsNullOrEmpty(json))
            {
                result.AddError("JSON is empty");
                return result;
            }

            try
            {
                var item = JsonUtility.FromJson<Item>(json);

                if (string.IsNullOrEmpty(item.itemId))
                    result.AddError("itemId is required");

                if (string.IsNullOrEmpty(item.shortDescription))
                    result.AddError("shortDescription is required");

                if (item.buyPrice < 0)
                    result.AddWarning("buyPrice is negative");

                if (item.sellPrice < 0)
                    result.AddWarning("sellPrice is negative");

                if (item.stacking && item.maxStack <= 0)
                    result.AddError("Stacking item must have maxStack > 0");

                result.parsedObject = item;
            }
            catch (Exception e)
            {
                result.AddError($"JSON parse error: {e.Message}");
            }

            return result;
        }

        /// <summary>
        /// Validate chapter outline JSON.
        /// </summary>
        public static ValidationResult ValidateChapterOutline(string json)
        {
            var result = new ValidationResult();

            if (string.IsNullOrEmpty(json))
            {
                result.AddError("JSON is empty");
                return result;
            }

            try
            {
                var outline = JsonUtility.FromJson<ChapterOutline>(json);

                if (string.IsNullOrEmpty(outline.chapterId))
                    result.AddError("chapterId is required");

                if (string.IsNullOrEmpty(outline.chapterName))
                    result.AddError("chapterName is required");

                if (outline.locations == null || outline.locations.Count == 0)
                    result.AddError("At least one location is required");

                if (outline.mainQuests == null || outline.mainQuests.Count == 0)
                    result.AddError("At least one main quest is required");

                // Validate location IDs are unique
                var locationIds = new HashSet<string>();
                foreach (var loc in outline.locations ?? new List<LocationSummary>())
                {
                    if (string.IsNullOrEmpty(loc.locationId))
                        result.AddError("Location missing locationId");
                    else if (!locationIds.Add(loc.locationId))
                        result.AddError($"Duplicate location ID: {loc.locationId}");
                }

                // Validate quest references exist
                foreach (var quest in outline.mainQuests ?? new List<QuestSummary>())
                {
                    if (!string.IsNullOrEmpty(quest.taskLocation) && !locationIds.Contains(quest.taskLocation))
                        result.AddWarning($"Quest '{quest.questId}' references unknown location: {quest.taskLocation}");
                }

                result.parsedObject = outline;
            }
            catch (Exception e)
            {
                result.AddError($"JSON parse error: {e.Message}");
            }

            return result;
        }

        /// <summary>
        /// Validate dialogue structure.
        /// </summary>
        private static void ValidateDialogue(DialogueValidation dialogue, ValidationResult result)
        {
            if (string.IsNullOrEmpty(dialogue.npc_name))
                result.AddWarning("Dialogue missing npc_name");

            if (dialogue.dialogues == null || dialogue.dialogues.Length == 0)
            {
                result.AddWarning($"NPC '{dialogue.npc_name}' has no dialogue steps");
                return;
            }

            for (int i = 0; i < dialogue.dialogues.Length; i++)
            {
                var step = dialogue.dialogues[i];

                if (string.IsNullOrEmpty(step.message))
                    result.AddWarning($"Dialogue step {i} has empty message");

                if (step.responses == null || step.responses.Length == 0)
                    result.AddWarning($"Dialogue step {i} has no responses");
                else
                {
                    foreach (var response in step.responses)
                    {
                        // Validate next_step references
                        if (response.next_step >= dialogue.dialogues.Length && response.next_step != -1)
                            result.AddError($"Invalid next_step {response.next_step} in dialogue (max: {dialogue.dialogues.Length - 1})");
                    }
                }
            }
        }

        /// <summary>
        /// Validate a room graph structure.
        /// </summary>
        public static ValidationResult ValidateRoomGraph(RoomGraph graph)
        {
            var result = new ValidationResult();

            if (graph == null)
            {
                result.AddError("Room graph is null");
                return result;
            }

            if (string.IsNullOrEmpty(graph.chapterId))
                result.AddError("chapterId is required");

            if (graph.rooms == null || graph.rooms.Count == 0)
            {
                result.AddError("Room graph has no rooms");
                return result;
            }

            // Validate hub, entry, exit room IDs exist
            var allRoomIds = new HashSet<string>(graph.GetAllRoomIds());

            if (!string.IsNullOrEmpty(graph.hubRoomId) && !allRoomIds.Contains(graph.hubRoomId))
                result.AddError($"hubRoomId '{graph.hubRoomId}' not found in room list");

            if (!string.IsNullOrEmpty(graph.entryRoomId) && !allRoomIds.Contains(graph.entryRoomId))
                result.AddError($"entryRoomId '{graph.entryRoomId}' not found in room list");

            if (!string.IsNullOrEmpty(graph.exitRoomId) && !allRoomIds.Contains(graph.exitRoomId))
                result.AddError($"exitRoomId '{graph.exitRoomId}' not found in room list");

            // Validate each room
            var roomIds = new HashSet<string>();
            foreach (var room in graph.rooms)
            {
                if (string.IsNullOrEmpty(room.roomId))
                {
                    result.AddError("Room has empty roomId");
                    continue;
                }

                if (!roomIds.Add(room.roomId))
                {
                    result.AddError($"Duplicate room ID: {room.roomId}");
                }

                if (string.IsNullOrEmpty(room.roomName))
                    result.AddWarning($"Room '{room.roomId}' missing roomName");

                // Validate room type
                var validTypes = new HashSet<string> { RoomTypes.Crossroad, RoomTypes.Interaction, RoomTypes.Combat };
                if (!string.IsNullOrEmpty(room.roomType) && !validTypes.Contains(room.roomType))
                {
                    result.AddWarning($"Room '{room.roomId}' has unknown type: '{room.roomType}'");
                }

                // Validate connections
                if (room.connectsTo == null || room.connectsTo.Count == 0)
                {
                    result.AddWarning($"Room '{room.roomId}' has no connections (isolated room)");
                }
                else
                {
                    foreach (var targetId in room.connectsTo)
                    {
                        if (!allRoomIds.Contains(targetId))
                        {
                            result.AddError($"Room '{room.roomId}' connects to non-existent room '{targetId}'");
                        }
                    }
                }

                // Validate interaction rooms have NPCs
                if (room.roomType == RoomTypes.Interaction)
                {
                    if (room.npcs == null || room.npcs.Count == 0)
                    {
                        result.AddWarning($"Interaction room '{room.roomId}' has no NPCs defined");
                    }
                }

                // Validate combat rooms have enemy
                if (room.roomType == RoomTypes.Combat)
                {
                    if (string.IsNullOrEmpty(room.enemyId))
                    {
                        result.AddWarning($"Combat room '{room.roomId}' has no enemyId defined");
                    }
                }
            }

            // Check bidirectionality
            var connectionErrors = graph.ValidateConnections();
            foreach (var error in connectionErrors)
            {
                result.AddError(error);
            }

            result.parsedObject = graph;
            return result;
        }

        /// <summary>
        /// Extract clean JSON from LLM response (removes markdown if present).
        /// </summary>
        public static string CleanJson(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            string result = input.Trim();

            // Remove markdown code blocks
            if (result.StartsWith("```json"))
                result = result.Substring(7);
            else if (result.StartsWith("```"))
                result = result.Substring(3);

            if (result.EndsWith("```"))
                result = result.Substring(0, result.Length - 3);

            return result.Trim();
        }
    }

    // Validation-only structures (minimal for parsing)

    [Serializable]
    internal class RoomValidation
    {
        public string room_id;
        public string room_type;  // crossroad, interaction, combat
        public string description;
        public string[] npcs;
        public ActionValidation[] actions;
        public ExitValidation[] exits;
        public DialogueValidation[] dialogues;
    }

    [Serializable]
    internal class ActionValidation
    {
        public string action_id;
        public string action_description;
    }

    [Serializable]
    internal class ExitValidation
    {
        public string exit_name;
        public string leads_to;
    }

    [Serializable]
    internal class DialogueValidation
    {
        public string npc_name;
        public string dialogue_image;
        public DialogueStepValidation[] dialogues;
    }

    [Serializable]
    internal class DialogueStepValidation
    {
        public string message;
        public ResponseValidation[] responses;
    }

    [Serializable]
    internal class ResponseValidation
    {
        public string text;
        public int next_step;
    }

    [Serializable]
    internal class EnemyValidation
    {
        public string enemyId;
        public string enemyName;
        public string description;
        public int maxHitPoints;
        public int armorClass;
        public AttackValidation[] attacks;
    }

    [Serializable]
    internal class AttackValidation
    {
        public string attackName;
        public int damageMin;
        public int damageMax;
    }
}
