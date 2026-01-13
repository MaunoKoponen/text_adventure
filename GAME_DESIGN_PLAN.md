# Text Adventure RPG - System Overhaul Plan

## Overview

Overhaul of Unity text adventure game with:
- **Combat**: Core SRD 5.1 style (6 stats, AC, d20 rolls) - classless with skills
- **Map**: Hierarchical locations with freeform PNG map + manual pin placement
- **Quests**: Dependencies, location reveals, objective tracking
- **Stories**: Folder-based world swapping with dev world for testing

---

## Phase 1: Core Data Structures

### New Files Created

**Combat Stats** (`Assets/Scripts/Combat/`):
- `AbilityScores.cs` - STR, DEX, CON, INT, WIS, CHA with modifier calculation
- `DiceRoll.cs` - Parse "2d6+3" notation, roll with results
- `PlayerStats.cs` - Level, HP, AC calculation, skill ranks, proficiency bonus
- `EquipmentSlots.cs` - mainHand, offHand, armor, helmet, boots, gloves, rings, amulet
- `EquipmentItem.cs` - Extends Item with damage dice, armor class, weapon properties (finesse, two-handed, etc.)
- `EnemyData.cs` - AC, HP, attacks array, resistances/immunities, loot table

### Modified Existing

**PlayerData.cs** - Added nullable fields for backward compatibility:
```csharp
public PlayerStats stats;           // New
public EquipmentSlots equipment;    // New
```

---

## Phase 2: Combat System

### New Files

- `Assets/Scripts/Combat/CombatManager.cs` - Central combat logic
  - Initiative rolls (d20 + DEX mod)
  - Attack rolls (d20 + ability mod + proficiency vs AC)
  - Damage rolls (weapon dice + ability mod)
  - Critical hits (nat 20 = double damage dice)
  - Enemy AI turn handling

### Modified Existing

**RoomManager.cs** (`HandleCombatAction`):
- Delegate to CombatManager when enhanced combat data exists
- Fallback to simple HP combat for legacy rooms

**Room.Combat class**:
- Add optional fields: `armorClass`, `attacks[]`, `resistances[]`
- Keep `enemy_health` and `enemyDamage` for backward compatibility

---

## Phase 3: Map System

### New Files

**Data** (`Assets/Scripts/Map/`):
- `LocationData.cs` - Hierarchical: Region > Area > Location
- `MapData.cs` - Map image path, pins array, paths array
- `MapPinData.cs` - Position, revealFlag, requiredQuests, connectedLocations

**UI**:
- `MapSystem.cs` - Load map, spawn pins, draw paths, handle clicks
- `MapPinUI.cs` - Pin prefab controller with reveal/hide

**Editor** (`Assets/Editor/`):
- `MapEditorWindow.cs` - Unity Editor window for pin placement
  - Drag pins on map image
  - Set location IDs, reveal conditions
  - Draw connection paths
  - Export to JSON

**Debug**:
- `MapDebugUI.cs` - In-game debug panel (reveal all, teleport, set flags)

### Resource Structure

```
Assets/Resources/Maps/
  region_name.json    # Pin positions, connections, reveal conditions
```

---

## Phase 4: Quest System Enhancement

### New Files

- `Assets/Scripts/Quest/QuestManager.cs` - Central quest logic
  - Track active quests
  - Update objectives on events
  - Trigger location reveals
  - Handle quest chains/prerequisites

- `Assets/Scripts/Quest/QuestObjective.cs`:
  - Types: GoToRoom, TalkToNPC, CollectItem, DefeatEnemy, DeliverItem
  - Target ID, count required, current progress

### Modified Existing

**QuestLog.cs/QuestLogEntry**:
```csharp
public QuestObjective[] objectives;     // New
public string[] prerequisiteQuests;     // New
public string[] revealsLocations;       // New
public QuestReward rewards;             // New
```

**Diary.cs** - Display objectives with completion status

---

## Phase 5: Story/World System

### Folder Structure

```
Assets/Resources/Stories/
  story_name/
    config.json         # Story metadata, starting state
    Rooms/              # Room JSON files
    Quests/             # Quest JSON files
    Maps/               # Map data JSON files
    Enemies/            # Enemy templates
    Images/             # Story-specific images
```

### New Files

- `Assets/Scripts/Story/StoryConfig.cs` - Story metadata, starting room/items/flags
- `Assets/Scripts/Story/StoryManager.cs` - Load story, provide resource paths

### Modified Existing

**RoomManager.cs**:
```csharp
// Before: Resources.Load<TextAsset>("Rooms/" + roomId)
// After:  StoryManager.Instance?.LoadRoomData(roomId) ?? fallback
```

**Diary.cs** - Same pattern for quest loading

---

## Phase 6: Dev World

### Structure

```
Assets/Resources/Stories/dev_world/
  config.json
  Rooms/
    dev_hub.json              # Central hub with debug actions
    combat_test_arena.json    # Multiple enemy types
    equipment_test_room.json  # Armor/weapon testing
    map_test_room.json        # Pin reveal testing
    quest_test_start.json     # Quest chain start
    quest_test_end.json       # Quest chain completion
  Maps/
    dev_region.json
  Enemies/
    goblin.json, skeleton.json, ogre.json, knight.json
```

### Test Scenarios

| Location | Tests |
|----------|-------|
| Combat Arena | AC ranges (5-20), damage types, resistances |
| Equipment Room | Armor AC bonus, weapon dice, finesse |
| Map Test | Flag-based reveal, quest-based reveal |
| Quest Chain | Prerequisites, objectives, rewards, location unlock |

---

## Implementation Progress

```
Phase 1: Data Structures     [COMPLETE]
    |
    v
Phase 2: Combat System       [COMPLETE]
    |
    +---> Phase 3: Map System    [COMPLETE]
    |         |
    v         v
Phase 4: Quest System        [IN PROGRESS]
    |
    v
Phase 5: Story System        [PENDING]
    |
    v
Phase 6: Dev World           [PENDING]
```

---

## Files Summary

### New Files Created

| Category | Files |
|----------|-------|
| Combat | AbilityScores.cs, DiceRoll.cs, PlayerStats.cs, EquipmentSlots.cs, EquipmentItem.cs, EnemyData.cs, CombatManager.cs |
| Map | LocationData.cs, MapData.cs, MapSystem.cs, MapPinUI.cs, MapDebugUI.cs |
| Quest | QuestManager.cs, QuestObjective.cs |
| Editor | MapEditorWindow.cs |

### Modified Files

| File | Changes |
|------|---------|
| `PlayerData.cs` | Add stats, equipment fields |
| `RoomManager.cs` | Combat delegation, story-aware paths |

---

## Backward Compatibility

- All new PlayerData fields nullable (old saves work)
- StoryManager falls back to legacy "Rooms/" path
- CombatManager falls back to simple HP combat
- Existing room/quest JSON files work unchanged

---

## Remaining Work

1. **Phase 5**: Create `StoryManager.cs` and `StoryConfig.cs`
2. **Phase 6**: Create dev_world test content (6 rooms, enemies, map, quests)
3. Update `MapUI.cs` with complete implementation
4. Update `Diary.cs` with objective display
