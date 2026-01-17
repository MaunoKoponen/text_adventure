using System.Text;

namespace WorldGen
{
    /// <summary>
    /// Prompt templates for LLM world generation.
    /// Each template produces JSON matching the game's existing data formats.
    /// </summary>
    public static class PromptTemplates
    {
        /// <summary>
        /// System prompt that establishes world context for all generation.
        /// </summary>
        public static string GetSystemPrompt(WorldPrompt worldPrompt)
        {
            var sb = new StringBuilder();

            sb.AppendLine("You are a world-building assistant for a text adventure game.");
            sb.AppendLine("Your task is to generate game content in valid JSON format.");
            sb.AppendLine();
            sb.AppendLine("=== WORLD CONTEXT ===");
            sb.AppendLine($"World Name: {worldPrompt.worldName}");
            sb.AppendLine($"Theme: {worldPrompt.theme}");
            sb.AppendLine($"Tone: {worldPrompt.tone}");
            sb.AppendLine($"Era: {worldPrompt.era}");
            sb.AppendLine($"Setting: {worldPrompt.settingDescription}");
            sb.AppendLine($"Main Conflict: {worldPrompt.mainConflict}");
            sb.AppendLine($"Player Role: {worldPrompt.protagonistRole}");
            sb.AppendLine($"Writing Style: {worldPrompt.writingStyle}");
            sb.AppendLine($"Dialogue Tone: {worldPrompt.dialogueTone}");
            sb.AppendLine();
            sb.AppendLine("=== CRITICAL RULES ===");
            sb.AppendLine("1. Output ONLY valid JSON - no markdown, no explanations, no code blocks");
            sb.AppendLine("2. Use snake_case for all IDs (e.g., 'haunted_mill', 'guard_captain')");
            sb.AppendLine("3. Descriptions should be atmospheric and match the tone");
            sb.AppendLine("4. NPC dialogue must reflect personality and world lore");
            sb.AppendLine("5. All location/quest/NPC references must use consistent IDs");
            sb.AppendLine("6. Combat encounters must match specified difficulty level");

            return sb.ToString();
        }

        /// <summary>
        /// Prompt for generating chapter outline (high-level structure).
        /// </summary>
        public static string GetChapterOutlinePrompt(int chapterNumber, ChapterData previousChapter,
            WorldPrompt worldPrompt, GenerationSettings settings)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Generate a detailed outline for Chapter {chapterNumber}.");
            sb.AppendLine();
            sb.AppendLine("=== CHAPTER REQUIREMENTS ===");
            sb.AppendLine($"- {settings.locationsPerChapter} major locations");
            sb.AppendLine($"- {settings.mainQuestsPerChapter} main quests (required for progression)");
            sb.AppendLine($"- {settings.questsPerChapter - settings.mainQuestsPerChapter} side quests (optional)");
            sb.AppendLine($"- Base difficulty: {chapterNumber * 2} (scale 1-10)");
            sb.AppendLine($"- {settings.enemyTypesPerChapter} enemy types");
            sb.AppendLine($"- {settings.npcsPerChapter} key NPCs");
            sb.AppendLine();
            sb.AppendLine("=== LOCATION DISTRIBUTION ===");
            int hubCount = (int)(settings.locationsPerChapter * settings.hubLocationRatio);
            int questRevealCount = (int)(settings.locationsPerChapter * settings.questRevealedRatio);
            int gatedCount = settings.locationsPerChapter - hubCount - questRevealCount;
            sb.AppendLine($"- {hubCount} hub locations (always accessible: towns, shops)");
            sb.AppendLine($"- {questRevealCount} quest-revealed locations (discovered through quests)");
            sb.AppendLine($"- {gatedCount} progression-gated locations (require main quest completion)");

            if (previousChapter != null)
            {
                sb.AppendLine();
                sb.AppendLine("=== PREVIOUS CHAPTER CONTEXT ===");
                sb.AppendLine($"Previous Chapter: {previousChapter.chapterName}");
                sb.AppendLine($"Summary: {previousChapter.chapterDescription}");
                sb.AppendLine($"Exit Location: {previousChapter.exitLocationId}");
                sb.AppendLine("The new chapter should continue naturally from this point.");
            }

            sb.AppendLine();
            sb.AppendLine("=== OUTPUT FORMAT ===");
            sb.AppendLine(@"{
  ""chapterId"": ""chapter_" + chapterNumber + @""",
  ""chapterName"": ""<evocative chapter name>"",
  ""chapterDescription"": ""<2-3 sentence summary>"",
  ""chapterIntro"": ""<opening narration when chapter starts>"",
  ""hubLocationId"": ""<main_hub_id>"",
  ""entryLocationId"": ""<entry_from_previous_chapter>"",
  ""exitLocationId"": ""<exit_to_next_chapter>"",
  ""locations"": [
    {
      ""locationId"": ""<snake_case_id>"",
      ""locationName"": ""<display name>"",
      ""locationType"": ""hub|exploration|dungeon|boss|transition"",
      ""description"": ""<brief description>"",
      ""alwaysVisible"": true|false,
      ""connectedTo"": [""<other_location_ids>""]
    }
  ],
  ""mainQuests"": [
    {
      ""questId"": ""<snake_case_id>"",
      ""questName"": ""<quest name>"",
      ""description"": ""<quest summary>"",
      ""questGiver"": ""<npc_id>"",
      ""taskLocation"": ""<location_id>"",
      ""difficulty"": <1-10>
    }
  ],
  ""sideQuests"": [<same format as mainQuests>],
  ""keyNPCs"": [
    {
      ""npcId"": ""<snake_case_id>"",
      ""npcName"": ""<display name>"",
      ""role"": ""quest_giver|merchant|mentor|antagonist|citizen"",
      ""personality"": ""<brief personality>"",
      ""locationId"": ""<where they are found>""
    }
  ],
  ""enemies"": [
    {
      ""enemyId"": ""<snake_case_id>"",
      ""enemyName"": ""<display name>"",
      ""enemyType"": ""<creature type>"",
      ""challengeRating"": <1-10>,
      ""description"": ""<brief description>""
    }
  ]
}");

            return sb.ToString();
        }

        /// <summary>
        /// Prompt for generating the room connectivity graph for a chapter.
        /// This is generated FIRST before individual room content.
        /// </summary>
        public static string GetRoomGraphPrompt(ChapterOutline chapterOutline, WorldPrompt worldPrompt,
            GenerationSettings settings)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Generate a room connectivity graph for chapter: {chapterOutline.chapterName}");
            sb.AppendLine();
            sb.AppendLine("=== CHAPTER LOCATIONS ===");
            foreach (var loc in chapterOutline.locations)
            {
                sb.AppendLine($"- {loc.locationId}: {loc.locationName} ({loc.locationType})");
            }
            sb.AppendLine();
            sb.AppendLine("=== KEY NPCs ===");
            foreach (var npc in chapterOutline.keyNPCs)
            {
                sb.AppendLine($"- {npc.npcId}: {npc.npcName} ({npc.role}) at {npc.locationId}");
            }
            sb.AppendLine();
            sb.AppendLine("=== ROOM TYPE DEFINITIONS ===");
            sb.AppendLine("1. 'crossroad' - Navigation hub with 2-4 exits. NO NPC dialogues, NO combat.");
            sb.AppendLine("2. 'interaction' - NPC dialogue room with 1-2 exits. Contains NPCs to talk to.");
            sb.AppendLine("3. 'combat' - Combat encounter room with 1-2 exits. Contains enemy.");
            sb.AppendLine();
            sb.AppendLine("=== REQUIREMENTS ===");
            sb.AppendLine("- Every location must have at least one room");
            sb.AppendLine("- Hub locations should have mostly crossroad rooms");
            sb.AppendLine("- Each NPC needs an interaction room where they can be found");
            sb.AppendLine("- All connections must be bidirectional (if A connects to B, B must connect to A)");
            sb.AppendLine("- Use snake_case for all room IDs");
            sb.AppendLine("- Room IDs should follow pattern: locationId_descriptiveName (e.g., 'town_square_fountain', 'tavern_interior')");
            sb.AppendLine();
            sb.AppendLine("=== OUTPUT FORMAT ===");
            sb.AppendLine(@"{
  ""chapterId"": """ + chapterOutline.chapterId + @""",
  ""hubRoomId"": ""<main_hub_room_id>"",
  ""entryRoomId"": ""<entry_point_room_id>"",
  ""exitRoomId"": ""<exit_to_next_chapter_room_id>"",
  ""rooms"": [
    {
      ""roomId"": ""<snake_case_room_id>"",
      ""roomName"": ""<Display Name>"",
      ""roomType"": ""crossroad|interaction|combat"",
      ""description"": ""<brief 1-sentence description for context>"",
      ""connectsTo"": [""<other_room_ids>""],
      ""npcs"": [""<npc_ids_in_this_room>""],
      ""enemyId"": ""<enemy_id_or_null>"",
      ""isHub"": true|false
    }
  ]
}");

            return sb.ToString();
        }

        /// <summary>
        /// Prompt for generating a CROSSROAD room (navigation only, no NPCs).
        /// </summary>
        public static string GetCrossroadRoomPrompt(RoomNode roomNode, RoomGraph graph, ChapterData chapter,
            WorldPrompt worldPrompt)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Generate a CROSSROAD (navigation) room JSON for: {roomNode.roomName}");
            sb.AppendLine();
            sb.AppendLine("=== THIS IS A NAVIGATION ROOM ===");
            sb.AppendLine("- NO NPC dialogues");
            sb.AppendLine("- NO combat");
            sb.AppendLine("- Focus on atmospheric description and navigation options");
            sb.AppendLine();
            sb.AppendLine("=== ROOM DETAILS ===");
            sb.AppendLine($"Room ID: {roomNode.roomId}");
            sb.AppendLine($"Room Name: {roomNode.roomName}");
            sb.AppendLine($"Brief Context: {roomNode.description}");
            sb.AppendLine($"Chapter: {chapter.chapterNumber} - {chapter.chapterName}");
            sb.AppendLine();
            sb.AppendLine("=== VALID EXIT DESTINATIONS (use ONLY these room IDs) ===");
            foreach (var connectedId in roomNode.connectsTo)
            {
                var connected = graph.GetRoom(connectedId);
                if (connected != null)
                {
                    sb.AppendLine($"- {connectedId}: {connected.roomName}");
                }
            }
            sb.AppendLine();
            sb.AppendLine("=== REQUIREMENTS ===");
            sb.AppendLine("- Rich atmospheric description (2-3 paragraphs)");
            sb.AppendLine("- actions array should be EMPTY []");
            sb.AppendLine("- dialogues array should be EMPTY []");
            sb.AppendLine("- npcs array should be EMPTY []");
            sb.AppendLine($"- Exactly {roomNode.connectsTo.Count} exits using ONLY the room IDs listed above");
            sb.AppendLine();
            sb.AppendLine("=== OUTPUT FORMAT ===");
            sb.AppendLine(@"{
  ""room_id"": """ + roomNode.roomId + @""",
  ""room_type"": ""crossroad"",
  ""description"": ""<rich atmospheric description with sensory details - what the player sees, hears, smells>"",
  ""npcs"": [],
  ""items"": [],
  ""actions"": [],
  ""dialogues"": [],
  ""exits"": [");

            // Generate exit stubs for the exact connections
            for (int i = 0; i < roomNode.connectsTo.Count; i++)
            {
                var targetId = roomNode.connectsTo[i];
                var targetRoom = graph.GetRoom(targetId);
                string targetName = targetRoom?.roomName ?? targetId;
                string comma = i < roomNode.connectsTo.Count - 1 ? "," : "";
                sb.AppendLine($@"    {{
      ""exit_name"": ""<direction to {targetName}>"",
      ""leads_to"": ""{targetId}"",
      ""conditions"": [],
      ""conditions_not"": []
    }}{comma}");
            }

            sb.AppendLine(@"  ],
  ""combat"": null,
  ""events"": []
}");

            return sb.ToString();
        }

        /// <summary>
        /// Prompt for generating an INTERACTION room (NPC dialogue room).
        /// CRITICAL: action_id MUST exactly match npc_name.
        /// </summary>
        public static string GetInteractionRoomPrompt(RoomNode roomNode, RoomGraph graph, ChapterData chapter,
            WorldPrompt worldPrompt, System.Collections.Generic.List<NPCSummary> npcsInRoom)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Generate an INTERACTION (NPC dialogue) room JSON for: {roomNode.roomName}");
            sb.AppendLine();
            sb.AppendLine("!!! CRITICAL RULE - READ CAREFULLY !!!");
            sb.AppendLine("action_id MUST EXACTLY EQUAL npc_name (case-sensitive)");
            sb.AppendLine("Example:");
            sb.AppendLine("  actions: [{ \"action_id\": \"tavernkeeper_martha\", ... }]");
            sb.AppendLine("  dialogues: [{ \"npc_name\": \"tavernkeeper_martha\", ... }]");
            sb.AppendLine();
            sb.AppendLine("=== ROOM DETAILS ===");
            sb.AppendLine($"Room ID: {roomNode.roomId}");
            sb.AppendLine($"Room Name: {roomNode.roomName}");
            sb.AppendLine($"Brief Context: {roomNode.description}");
            sb.AppendLine($"Chapter: {chapter.chapterNumber} - {chapter.chapterName}");
            sb.AppendLine();
            sb.AppendLine("=== NPCs IN THIS ROOM (create dialogue for each) ===");
            foreach (var npcId in roomNode.npcs)
            {
                var npc = npcsInRoom?.Find(n => n.npcId == npcId);
                if (npc != null)
                {
                    sb.AppendLine($"- {npc.npcId}: {npc.npcName} ({npc.role}) - {npc.personality}");
                }
                else
                {
                    sb.AppendLine($"- {npcId}");
                }
            }
            sb.AppendLine();
            sb.AppendLine("=== VALID EXIT DESTINATIONS (use ONLY these room IDs) ===");
            foreach (var connectedId in roomNode.connectsTo)
            {
                var connected = graph.GetRoom(connectedId);
                if (connected != null)
                {
                    sb.AppendLine($"- {connectedId}: {connected.roomName}");
                }
            }
            sb.AppendLine();
            sb.AppendLine("=== REQUIREMENTS ===");
            sb.AppendLine("- Rich atmospheric description (2-3 paragraphs)");
            sb.AppendLine($"- Exactly {roomNode.npcs.Count} action(s), one per NPC");
            sb.AppendLine("- EACH action_id MUST match a dialogue npc_name EXACTLY");
            sb.AppendLine("- Each dialogue should have 3-5 steps with branching responses");
            sb.AppendLine($"- Exactly {roomNode.connectsTo.Count} exits using ONLY the room IDs listed above");
            sb.AppendLine();
            sb.AppendLine("=== OUTPUT FORMAT ===");
            sb.AppendLine(@"{
  ""room_id"": """ + roomNode.roomId + @""",
  ""room_type"": ""interaction"",
  ""description"": ""<rich atmospheric description>"",
  ""npcs"": [");

            // List NPC IDs
            for (int i = 0; i < roomNode.npcs.Count; i++)
            {
                string comma = i < roomNode.npcs.Count - 1 ? "," : "";
                sb.AppendLine($"    \"{roomNode.npcs[i]}\"{comma}");
            }

            sb.AppendLine(@"  ],
  ""items"": [],
  ""actions"": [");

            // Generate action stubs that MATCH the NPC IDs
            for (int i = 0; i < roomNode.npcs.Count; i++)
            {
                var npcId = roomNode.npcs[i];
                var npc = npcsInRoom?.Find(n => n.npcId == npcId);
                string npcDisplayName = npc?.npcName ?? npcId;
                string comma = i < roomNode.npcs.Count - 1 ? "," : "";
                sb.AppendLine($@"    {{
      ""action_id"": ""{npcId}"",
      ""action_description"": ""Talk to {npcDisplayName}""
    }}{comma}");
            }

            sb.AppendLine(@"  ],
  ""dialogues"": [");

            // Generate dialogue stubs that MATCH the NPC IDs
            for (int i = 0; i < roomNode.npcs.Count; i++)
            {
                var npcId = roomNode.npcs[i];
                var npc = npcsInRoom?.Find(n => n.npcId == npcId);
                string personality = npc?.personality ?? "mysterious";
                string comma = i < roomNode.npcs.Count - 1 ? "," : "";
                sb.AppendLine($@"    {{
      ""npc_name"": ""{npcId}"",
      ""dialogue_image"": ""npc_default"",
      ""dialogues"": [
        {{
          ""message"": ""<{personality} greeting>"",
          ""responses"": [
            {{ ""text"": ""<option 1>"", ""next_step"": 1 }},
            {{ ""text"": ""<option 2>"", ""next_step"": 2 }},
            {{ ""text"": ""Farewell."", ""next_step"": -1 }}
          ]
        }},
        {{
          ""message"": ""<response to option 1>"",
          ""responses"": [
            {{ ""text"": ""<continue>"", ""next_step"": 3 }},
            {{ ""text"": ""Farewell."", ""next_step"": -1 }}
          ]
        }},
        {{
          ""message"": ""<response to option 2>"",
          ""responses"": [
            {{ ""text"": ""<continue>"", ""next_step"": 3 }},
            {{ ""text"": ""Farewell."", ""next_step"": -1 }}
          ]
        }},
        {{
          ""message"": ""<deeper conversation>"",
          ""responses"": [
            {{ ""text"": ""<conclude>"", ""next_step"": -1 }}
          ]
        }}
      ]
    }}{comma}");
            }

            sb.AppendLine(@"  ],
  ""exits"": [");

            // Generate exit stubs
            for (int i = 0; i < roomNode.connectsTo.Count; i++)
            {
                var targetId = roomNode.connectsTo[i];
                var targetRoom = graph.GetRoom(targetId);
                string targetName = targetRoom?.roomName ?? targetId;
                string comma = i < roomNode.connectsTo.Count - 1 ? "," : "";
                sb.AppendLine($@"    {{
      ""exit_name"": ""<direction to {targetName}>"",
      ""leads_to"": ""{targetId}"",
      ""conditions"": [],
      ""conditions_not"": []
    }}{comma}");
            }

            sb.AppendLine(@"  ],
  ""combat"": null,
  ""events"": []
}");

            return sb.ToString();
        }

        /// <summary>
        /// Prompt for generating a COMBAT room (enemy encounter).
        /// </summary>
        public static string GetCombatRoomPrompt(RoomNode roomNode, RoomGraph graph, ChapterData chapter,
            WorldPrompt worldPrompt, EnemySummary enemy)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Generate a COMBAT (encounter) room JSON for: {roomNode.roomName}");
            sb.AppendLine();
            sb.AppendLine("=== THIS IS A COMBAT ROOM ===");
            sb.AppendLine("- Contains an enemy encounter");
            sb.AppendLine("- May have treasure/loot after combat");
            sb.AppendLine("- Usually 1-2 exits (back, and forward after victory)");
            sb.AppendLine();
            sb.AppendLine("=== ROOM DETAILS ===");
            sb.AppendLine($"Room ID: {roomNode.roomId}");
            sb.AppendLine($"Room Name: {roomNode.roomName}");
            sb.AppendLine($"Brief Context: {roomNode.description}");
            sb.AppendLine($"Chapter: {chapter.chapterNumber} - {chapter.chapterName}");
            sb.AppendLine();
            sb.AppendLine("=== ENEMY ===");
            if (enemy != null)
            {
                sb.AppendLine($"- {enemy.enemyId}: {enemy.enemyName} (CR {enemy.challengeRating})");
                sb.AppendLine($"- Description: {enemy.description}");
            }
            else
            {
                sb.AppendLine($"- {roomNode.enemyId}");
            }
            sb.AppendLine();
            sb.AppendLine("=== VALID EXIT DESTINATIONS (use ONLY these room IDs) ===");
            foreach (var connectedId in roomNode.connectsTo)
            {
                var connected = graph.GetRoom(connectedId);
                if (connected != null)
                {
                    sb.AppendLine($"- {connectedId}: {connected.roomName}");
                }
            }
            sb.AppendLine();
            sb.AppendLine("=== REQUIREMENTS ===");
            sb.AppendLine("- Tense atmospheric description hinting at danger");
            sb.AppendLine("- actions array: empty or simple 'Search area' after combat");
            sb.AppendLine("- dialogues array: EMPTY []");
            sb.AppendLine("- combat field must reference the enemy");
            sb.AppendLine($"- Exactly {roomNode.connectsTo.Count} exits using ONLY the room IDs listed above");
            sb.AppendLine();
            sb.AppendLine("=== OUTPUT FORMAT ===");
            sb.AppendLine(@"{
  ""room_id"": """ + roomNode.roomId + @""",
  ""room_type"": ""combat"",
  ""description"": ""<tense atmospheric description - signs of danger, enemy presence>"",
  ""npcs"": [],
  ""items"": [],
  ""actions"": [],
  ""dialogues"": [],
  ""exits"": [");

            // Generate exit stubs
            for (int i = 0; i < roomNode.connectsTo.Count; i++)
            {
                var targetId = roomNode.connectsTo[i];
                var targetRoom = graph.GetRoom(targetId);
                string targetName = targetRoom?.roomName ?? targetId;
                string comma = i < roomNode.connectsTo.Count - 1 ? "," : "";

                // Combat rooms often have victory conditions on forward exits
                bool isBackExit = i == 0; // Assume first exit is back
                string conditions = isBackExit ? "[]" : $"[\"{roomNode.enemyId}_defeated\"]";

                sb.AppendLine($@"    {{
      ""exit_name"": ""<direction to {targetName}>"",
      ""leads_to"": ""{targetId}"",
      ""conditions"": {conditions},
      ""conditions_not"": []
    }}{comma}");
            }

            sb.AppendLine(@"  ],
  ""combat"": {
    ""enemyId"": """ + (enemy?.enemyId ?? roomNode.enemyId) + @""",
    ""isBoss"": false,
    ""defeatFlag"": """ + (enemy?.enemyId ?? roomNode.enemyId) + @"_defeated""
  },
  ""events"": []
}");

            return sb.ToString();
        }

        /// <summary>
        /// OLD prompt - kept for backwards compatibility but prefer type-specific prompts.
        /// </summary>
        [System.Obsolete("Use GetCrossroadRoomPrompt, GetInteractionRoomPrompt, or GetCombatRoomPrompt instead")]
        public static string GetRoomPrompt(LocationSummary locationSummary, ChapterData chapter,
            WorldPrompt worldPrompt)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Generate a complete room JSON for: {locationSummary.locationName}");
            sb.AppendLine();
            sb.AppendLine("=== LOCATION DETAILS ===");
            sb.AppendLine($"ID: {locationSummary.locationId}");
            sb.AppendLine($"Type: {locationSummary.locationType}");
            sb.AppendLine($"Description: {locationSummary.description}");
            sb.AppendLine($"Chapter: {chapter.chapterNumber} - {chapter.chapterName}");
            sb.AppendLine($"Difficulty: {chapter.baseDifficulty}");
            sb.AppendLine();
            sb.AppendLine("!!! CRITICAL RULE !!!");
            sb.AppendLine("action_id MUST EXACTLY EQUAL npc_name for dialogue actions");
            sb.AppendLine();
            sb.AppendLine("=== REQUIREMENTS ===");
            sb.AppendLine("- Rich, atmospheric description (2-3 paragraphs)");
            sb.AppendLine("- 2-4 interactive actions the player can take");
            sb.AppendLine("- Exits to connected locations with proper conditions");

            if (locationSummary.locationType == "hub")
            {
                sb.AppendLine("- This is a safe hub: include merchants, quest givers, rest options");
            }
            else if (locationSummary.locationType == "dungeon" || locationSummary.locationType == "boss")
            {
                sb.AppendLine("- Include combat encounters appropriate to difficulty");
            }

            sb.AppendLine();
            sb.AppendLine("=== OUTPUT FORMAT ===");
            sb.AppendLine(@"{
  ""room_id"": """ + locationSummary.locationId + @""",
  ""description"": ""<rich atmospheric description with sensory details>"",
  ""npcs"": [""<npc_id_1>"", ""<npc_id_2>""],
  ""items"": [],
  ""actions"": [
    {
      ""action_id"": ""<MUST_MATCH_npc_name_below>"",
      ""action_description"": ""<what player sees as option>""
    }
  ],
  ""dialogues"": [
    {
      ""npc_name"": ""<MUST_MATCH_action_id_above>"",
      ""dialogue_image"": ""npc_default"",
      ""dialogues"": [
        {
          ""message"": ""<NPC's opening line with personality>"",
          ""responses"": [
            {
              ""text"": ""<player response option>"",
              ""next_step"": 1
            },
            {
              ""text"": ""Goodbye"",
              ""next_step"": -1
            }
          ]
        },
        {
          ""message"": ""<NPC's follow-up>"",
          ""responses"": [
            {
              ""text"": ""<continue conversation>"",
              ""next_step"": 2
            }
          ]
        }
      ]
    }
  ],
  ""exits"": [
    {
      ""exit_name"": ""<direction or destination name>"",
      ""leads_to"": ""<room_id>"",
      ""conditions"": [],
      ""conditions_not"": []
    }
  ],
  ""combat"": null,
  ""events"": []
}");

            return sb.ToString();
        }

        /// <summary>
        /// Prompt for generating a complete quest with objectives.
        /// </summary>
        public static string GetQuestPrompt(QuestSummary questSummary, ChapterData chapter,
            WorldPrompt worldPrompt, bool isMainQuest)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Generate a complete quest JSON for: {questSummary.questName}");
            sb.AppendLine();
            sb.AppendLine("=== QUEST DETAILS ===");
            sb.AppendLine($"ID: {questSummary.questId}");
            sb.AppendLine($"Type: {(isMainQuest ? "MAIN QUEST (required for progression)" : "Side Quest (optional)")}");
            sb.AppendLine($"Description: {questSummary.description}");
            sb.AppendLine($"Quest Giver: {questSummary.questGiver}");
            sb.AppendLine($"Task Location: {questSummary.taskLocation}");
            sb.AppendLine($"Difficulty: {questSummary.difficulty}");
            sb.AppendLine($"Chapter: {chapter.chapterNumber}");
            sb.AppendLine();
            sb.AppendLine("=== REQUIREMENTS ===");
            sb.AppendLine("- 2-4 meaningful objectives");
            sb.AppendLine("- Objectives should tell a mini-story");
            sb.AppendLine("- Rewards appropriate to difficulty");

            if (isMainQuest)
            {
                sb.AppendLine("- Should reveal new locations on completion");
                sb.AppendLine("- Critical to chapter narrative");
            }

            sb.AppendLine();
            sb.AppendLine("=== OBJECTIVE TYPES ===");
            sb.AppendLine("GoToRoom, TalkToNPC, CollectItem, DeliverItem, DefeatEnemy, DefeatCount");
            sb.AppendLine();
            sb.AppendLine("=== OUTPUT FORMAT ===");
            sb.AppendLine(@"{
  ""questId"": """ + questSummary.questId + @""",
  ""questName"": """ + questSummary.questName + @""",
  ""questDescription"": ""<engaging description>"",
  ""questGiver"": """ + questSummary.questGiver + @""",
  ""questGiverLocation"": ""<location_id>"",
  ""questType"": """ + (isMainQuest ? "Main" : "Side") + @""",
  ""chapterNumber"": " + chapter.chapterNumber + @",
  ""difficulty"": " + questSummary.difficulty + @",
  ""prerequisiteQuests"": [],
  ""prerequisiteFlags"": [],
  ""objectives"": [
    {
      ""objectiveId"": ""<objective_id>"",
      ""description"": ""<what player needs to do>"",
      ""type"": ""GoToRoom|TalkToNPC|CollectItem|DeliverItem|DefeatEnemy|DefeatCount"",
      ""targetId"": ""<target_room/npc/item/enemy_id>"",
      ""targetCount"": 1,
      ""isOptional"": false
    }
  ],
  ""revealsOnAccept"": [""<location_ids_to_reveal>""],
  ""revealsOnComplete"": [""<location_ids_to_reveal>""],
  ""rewards"": {
    ""experiencePoints"": <50-500 based on difficulty>,
    ""gold"": <25-250 based on difficulty>,
    ""itemIds"": [],
    ""flagsToSet"": [
      {
        ""flagName"": ""quest_" + questSummary.questId + @"_complete"",
        ""flagValue"": ""true""
      }
    ]
  },
  ""state"": ""NotStarted""
}");

            return sb.ToString();
        }

        /// <summary>
        /// Prompt for generating an enemy with attacks and loot.
        /// </summary>
        public static string GetEnemyPrompt(EnemySummary enemySummary, ChapterData chapter,
            WorldPrompt worldPrompt)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Generate a complete enemy JSON for: {enemySummary.enemyName}");
            sb.AppendLine();
            sb.AppendLine("=== ENEMY DETAILS ===");
            sb.AppendLine($"ID: {enemySummary.enemyId}");
            sb.AppendLine($"Type: {enemySummary.enemyType}");
            sb.AppendLine($"Challenge Rating: {enemySummary.challengeRating}");
            sb.AppendLine($"Description: {enemySummary.description}");
            sb.AppendLine();
            sb.AppendLine("=== STAT GUIDELINES (based on CR) ===");

            int cr = enemySummary.challengeRating;
            int baseHp = 20 + (cr * 15);
            int baseAc = 8 + cr;
            int baseDamage = 5 + (cr * 3);

            sb.AppendLine($"- HP: ~{baseHp} (range: {baseHp - 10} to {baseHp + 10})");
            sb.AppendLine($"- AC: ~{baseAc}");
            sb.AppendLine($"- Damage per attack: ~{baseDamage}");
            sb.AppendLine();
            sb.AppendLine("=== OUTPUT FORMAT ===");
            sb.AppendLine(@"{
  ""enemyId"": """ + enemySummary.enemyId + @""",
  ""enemyName"": """ + enemySummary.enemyName + @""",
  ""description"": ""<combat description>"",
  ""enemyImage"": ""enemy_default"",
  ""maxHitPoints"": " + baseHp + @",
  ""currentHitPoints"": " + baseHp + @",
  ""armorClass"": " + baseAc + @",
  ""experienceValue"": " + (cr * 25) + @",
  ""goldDrop"": " + (cr * 10) + @",
  ""attacks"": [
    {
      ""attackName"": ""<primary attack>"",
      ""attackDescription"": ""<flavor text>"",
      ""damageMin"": " + (baseDamage - 2) + @",
      ""damageMax"": " + (baseDamage + 2) + @",
      ""hitBonus"": " + cr + @"
    },
    {
      ""attackName"": ""<special attack>"",
      ""attackDescription"": ""<flavor text>"",
      ""damageMin"": " + (baseDamage) + @",
      ""damageMax"": " + (baseDamage + 5) + @",
      ""hitBonus"": " + (cr - 1) + @"
    }
  ],
  ""lootTable"": [
    {
      ""itemId"": ""health_potion"",
      ""dropChance"": 0.3
    }
  ]
}");

            return sb.ToString();
        }

        /// <summary>
        /// Prompt for generating a detailed NPC dialogue tree.
        /// </summary>
        public static string GetDialoguePrompt(NPCSummary npcSummary, string purpose,
            WorldPrompt worldPrompt)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Generate a complete dialogue tree for NPC: {npcSummary.npcName}");
            sb.AppendLine();
            sb.AppendLine("=== NPC DETAILS ===");
            sb.AppendLine($"ID: {npcSummary.npcId}");
            sb.AppendLine($"Role: {npcSummary.role}");
            sb.AppendLine($"Personality: {npcSummary.personality}");
            sb.AppendLine($"Purpose: {purpose}");
            sb.AppendLine($"Location: {npcSummary.locationId}");
            sb.AppendLine();
            sb.AppendLine("=== DIALOGUE STYLE ===");
            sb.AppendLine($"World Tone: {worldPrompt.tone}");
            sb.AppendLine($"Writing Style: {worldPrompt.writingStyle}");
            sb.AppendLine($"Dialogue Tone: {worldPrompt.dialogueTone}");
            sb.AppendLine();
            sb.AppendLine("=== REQUIREMENTS ===");
            sb.AppendLine("- At least 5 dialogue steps");
            sb.AppendLine("- Multiple response options per step");
            sb.AppendLine("- Dialogue should reveal character personality");
            sb.AppendLine("- Include world lore hints where appropriate");
            sb.AppendLine("- next_step: -1 ends conversation, 0+ continues to that step index");
            sb.AppendLine();
            sb.AppendLine("=== OUTPUT FORMAT ===");
            sb.AppendLine(@"{
  ""npc_name"": """ + npcSummary.npcName + @""",
  ""dialogue_image"": ""npc_default"",
  ""dialogues"": [
    {
      ""message"": ""<NPC greeting with personality>"",
      ""responses"": [
        {
          ""text"": ""<player option 1>"",
          ""next_step"": 1
        },
        {
          ""text"": ""<player option 2>"",
          ""next_step"": 2
        },
        {
          ""text"": ""Farewell."",
          ""next_step"": -1
        }
      ]
    },
    {
      ""message"": ""<response to option 1>"",
      ""responses"": [
        {
          ""text"": ""<continue>"",
          ""next_step"": 3
        }
      ]
    }
  ]
}");

            return sb.ToString();
        }

        /// <summary>
        /// Prompt for generating an item.
        /// </summary>
        public static string GetItemPrompt(string itemName, string itemType, int value,
            ChapterData chapter, WorldPrompt worldPrompt)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Generate a complete item JSON for: {itemName}");
            sb.AppendLine();
            sb.AppendLine("=== ITEM DETAILS ===");
            sb.AppendLine($"Type: {itemType}");
            sb.AppendLine($"Approximate Value: {value} gold");
            sb.AppendLine($"Chapter: {chapter.chapterNumber}");
            sb.AppendLine();
            sb.AppendLine("=== ITEM TYPES ===");
            sb.AppendLine("- weapon: Damage effect on NPC target");
            sb.AppendLine("- armor: AC bonus (effectType 3)");
            sb.AppendLine("- consumable: Heal, Bless, CurePoison effects on Self");
            sb.AppendLine("- key: Open effect on Lock target");
            sb.AppendLine("- quest: No combat use, story item");
            sb.AppendLine();
            sb.AppendLine("=== OUTPUT FORMAT ===");
            sb.AppendLine(@"{
  ""itemId"": ""<snake_case_id>"",
  ""shortDescription"": ""<Display Name>"",
  ""description"": ""<flavor description>"",
  ""usageSuccess"": ""<message when used successfully>"",
  ""usageFail"": ""<message when use fails>"",
  ""category"": ""<weapon|armor|consumable|key|quest>"",
  ""effectType"": 0,
  ""effectAmount"": 0,
  ""target"": 0,
  ""stacking"": true|false,
  ""maxStack"": 1-99,
  ""buyPrice"": " + value + @",
  ""sellPrice"": " + (value / 2) + @",
  ""image"": ""item_default"",
  ""combatUsable"": true|false
}");

            sb.AppendLine();
            sb.AppendLine("effectType values: 0=Damage, 1=Heal, 2=Bless, 3=CurePoison, 4=Open");
            sb.AppendLine("target values: 0=NPC, 1=Self, 2=Lock, 3=None");

            return sb.ToString();
        }
    }
}
