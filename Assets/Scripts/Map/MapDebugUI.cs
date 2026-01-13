using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// In-game debug panel for testing map functionality.
/// Only active in development builds.
/// </summary>
public class MapDebugUI : MonoBehaviour
{
    [Header("Debug Panel")]
    public GameObject debugPanel;
    public Button toggleButton;

    [Header("Controls")]
    public TMP_Dropdown locationDropdown;
    public Button teleportButton;
    public Button revealAllButton;
    public Button hideAllButton;
    public Button refreshButton;

    [Header("Flag Controls")]
    public TMP_InputField flagNameInput;
    public TMP_InputField flagValueInput;
    public Button setFlagButton;

    [Header("Info Display")]
    public TMP_Text infoText;

    private MapSystem mapSystem;
    private bool isPanelVisible = false;

    private void Start()
    {
        // Only enable in editor or development builds
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        gameObject.SetActive(true);
        SetupDebugControls();
#else
        gameObject.SetActive(false);
#endif
    }

    private void SetupDebugControls()
    {
        mapSystem = FindObjectOfType<MapSystem>();

        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(TogglePanel);
        }

        if (debugPanel != null)
        {
            debugPanel.SetActive(false);
        }

        if (teleportButton != null)
        {
            teleportButton.onClick.AddListener(TeleportToSelected);
        }

        if (revealAllButton != null)
        {
            revealAllButton.onClick.AddListener(RevealAllLocations);
        }

        if (hideAllButton != null)
        {
            hideAllButton.onClick.AddListener(HideAllLocations);
        }

        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(RefreshMap);
        }

        if (setFlagButton != null)
        {
            setFlagButton.onClick.AddListener(SetDebugFlag);
        }

        PopulateLocationDropdown();
    }

    private void TogglePanel()
    {
        isPanelVisible = !isPanelVisible;
        if (debugPanel != null)
        {
            debugPanel.SetActive(isPanelVisible);
        }

        if (isPanelVisible)
        {
            PopulateLocationDropdown();
            UpdateInfoDisplay();
        }
    }

    private void PopulateLocationDropdown()
    {
        if (locationDropdown == null) return;

        locationDropdown.ClearOptions();

        var options = new List<string>();

        // Get locations from current map
        if (mapSystem != null)
        {
            var pins = mapSystem.GetVisiblePins();
            foreach (var pin in pins)
            {
                options.Add($"{pin.PinData.displayName} ({pin.PinData.locationId})");
            }
        }

        // Also add all room IDs from RoomManager if available
        // This allows teleporting to any room, not just map locations
        var roomFiles = Resources.LoadAll<TextAsset>("Rooms");
        foreach (var roomFile in roomFiles)
        {
            string roomId = roomFile.name;
            if (!options.Exists(o => o.Contains(roomId)))
            {
                options.Add($"[Room] {roomId}");
            }
        }

        locationDropdown.AddOptions(options);
    }

    private void TeleportToSelected()
    {
        if (locationDropdown == null || locationDropdown.options.Count == 0) return;

        string selected = locationDropdown.options[locationDropdown.value].text;

        // Extract room/location ID
        string targetId;
        if (selected.StartsWith("[Room] "))
        {
            targetId = selected.Substring("[Room] ".Length);
        }
        else
        {
            // Extract ID from "Name (id)" format
            int startParen = selected.LastIndexOf('(');
            int endParen = selected.LastIndexOf(')');
            if (startParen >= 0 && endParen > startParen)
            {
                targetId = selected.Substring(startParen + 1, endParen - startParen - 1);
            }
            else
            {
                targetId = selected;
            }
        }

        // Attempt to load the room
        var roomManager = FindObjectOfType<RoomManager>();
        if (roomManager != null)
        {
            roomManager.LoadRoomFromJson(targetId, "[DEBUG] Teleported to location.\n");
            Debug.Log($"[MapDebug] Teleported to: {targetId}");
        }

        UpdateInfoDisplay();
    }

    private void RevealAllLocations()
    {
        if (mapSystem == null) return;

        // Set reveal flags for all locations
        // This is a debug shortcut - normally locations are revealed through gameplay

        var flags = RoomManager.playerData?.Flags;
        if (flags == null) return;

        // Get all pins and set their reveal flags
        var pins = mapSystem.GetVisiblePins();
        foreach (var pin in pins)
        {
            if (!string.IsNullOrEmpty(pin.PinData.revealFlag))
            {
                RoomManager.playerData.SetFlag(pin.PinData.revealFlag, "true");
            }
        }

        // Also reveal via a debug flag
        RoomManager.playerData.SetFlag("debug_reveal_all_map", "true");

        mapSystem.RefreshVisibility();
        Debug.Log("[MapDebug] All locations revealed");
        UpdateInfoDisplay();
    }

    private void HideAllLocations()
    {
        if (mapSystem == null) return;

        RoomManager.playerData?.SetFlag("debug_reveal_all_map", "false");
        mapSystem.RefreshVisibility();

        Debug.Log("[MapDebug] Non-default locations hidden");
        UpdateInfoDisplay();
    }

    private void RefreshMap()
    {
        if (mapSystem != null)
        {
            mapSystem.RefreshVisibility();
            Debug.Log("[MapDebug] Map refreshed");
        }
        UpdateInfoDisplay();
    }

    private void SetDebugFlag()
    {
        if (flagNameInput == null || flagValueInput == null) return;

        string flagName = flagNameInput.text.Trim();
        string flagValue = flagValueInput.text.Trim();

        if (string.IsNullOrEmpty(flagName))
        {
            Debug.LogWarning("[MapDebug] Flag name cannot be empty");
            return;
        }

        RoomManager.playerData?.SetFlag(flagName, flagValue);
        Debug.Log($"[MapDebug] Set flag: {flagName} = {flagValue}");

        // Refresh map in case this was a reveal flag
        if (mapSystem != null)
        {
            mapSystem.RefreshVisibility();
        }

        UpdateInfoDisplay();
    }

    private void UpdateInfoDisplay()
    {
        if (infoText == null) return;

        var sb = new System.Text.StringBuilder();

        // Current room
        string currentRoom = RoomManager.playerData?.currentRoom ?? "Unknown";
        sb.AppendLine($"Current Room: {currentRoom}");

        // Player stats
        if (RoomManager.playerData != null)
        {
            if (RoomManager.playerData.UsesEnhancedStats)
            {
                var stats = RoomManager.playerData.stats;
                sb.AppendLine($"HP: {stats.currentHitPoints}/{stats.maxHitPoints}");
                sb.AppendLine($"Level: {stats.level} (XP: {stats.experiencePoints})");
            }
            else
            {
                sb.AppendLine($"HP: {RoomManager.playerData.health}");
            }

            sb.AppendLine($"Gold: {RoomManager.playerData.coins}");
            sb.AppendLine();

            // Active flags (limit display)
            sb.AppendLine("Active Flags:");
            int flagCount = 0;
            foreach (var flag in RoomManager.playerData.Flags)
            {
                if (flag.Value == "true" || flag.Value == "active" || flag.Value == "concluded")
                {
                    sb.AppendLine($"  {flag.Key}: {flag.Value}");
                    flagCount++;
                    if (flagCount >= 10)
                    {
                        sb.AppendLine("  ...(more)");
                        break;
                    }
                }
            }
        }

        infoText.text = sb.ToString();
    }

    private void Update()
    {
        // Quick toggle with backtick key
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            TogglePanel();
        }
    }
}
