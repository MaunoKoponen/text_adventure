using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI component for a single map pin.
/// Handles display, interaction, and state visualization.
/// </summary>
public class MapPinUI : MonoBehaviour
{
    [Header("UI References")]
    public Image pinIcon;
    public Image questIndicator;
    public TMP_Text labelText;
    public Button button;
    public CanvasGroup canvasGroup;

    [Header("Visual Settings")]
    public Color normalColor = Color.white;
    public Color currentColor = Color.yellow;
    public Color visitedColor = new Color(0.6f, 0.6f, 0.6f);
    public Color hiddenColor = new Color(1f, 1f, 1f, 0.3f);

    // State
    public MapPinData PinData { get; private set; }
    public bool IsRevealed { get; private set; }
    public bool IsCurrent { get; private set; }
    public bool IsVisited { get; private set; }
    public bool HasQuest { get; private set; }

    private Action<MapPinData> onClickCallback;

    private void Awake()
    {
        // Get references if not assigned
        if (pinIcon == null)
            pinIcon = GetComponent<Image>();

        if (button == null)
            button = GetComponent<Button>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        // Add canvas group if missing
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Setup button click
        if (button != null)
        {
            button.onClick.AddListener(OnClick);
        }
    }

    /// <summary>
    /// Initialize the pin with data.
    /// </summary>
    public void Setup(MapPinData data, Action<MapPinData> onClick)
    {
        PinData = data;
        onClickCallback = onClick;

        // Set label
        if (labelText != null)
        {
            labelText.text = data.displayName;
        }

        // Set custom icon if specified
        if (!string.IsNullOrEmpty(data.iconSpritePath))
        {
            Sprite customIcon = Resources.Load<Sprite>(data.iconSpritePath);
            if (customIcon != null && pinIcon != null)
            {
                pinIcon.sprite = customIcon;
            }
        }

        // Set color
        if (!string.IsNullOrEmpty(data.iconColor) && pinIcon != null)
        {
            if (ColorUtility.TryParseHtmlString(data.iconColor, out Color color))
            {
                pinIcon.color = color;
                normalColor = color;
            }
        }

        // Initial state
        IsRevealed = data.alwaysVisible;
        IsVisited = data.isVisited;
        IsCurrent = data.isCurrentLocation;
        HasQuest = data.hasActiveQuest;

        UpdateVisuals();
    }

    /// <summary>
    /// Set whether this pin is revealed/visible.
    /// </summary>
    public void SetRevealed(bool revealed)
    {
        IsRevealed = revealed;
        UpdateVisuals();
    }

    /// <summary>
    /// Set this as the player's current location.
    /// </summary>
    public void SetAsCurrent(bool isCurrent)
    {
        IsCurrent = isCurrent;
        UpdateVisuals();
    }

    /// <summary>
    /// Mark this location as visited.
    /// </summary>
    public void SetVisited(bool visited)
    {
        IsVisited = visited;
        UpdateVisuals();
    }

    /// <summary>
    /// Show/hide quest indicator.
    /// </summary>
    public void SetQuestIndicator(bool hasQuest)
    {
        HasQuest = hasQuest;
        if (questIndicator != null)
        {
            questIndicator.gameObject.SetActive(hasQuest && IsRevealed);
        }
    }

    /// <summary>
    /// Update visual appearance based on state.
    /// </summary>
    private void UpdateVisuals()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = IsRevealed ? 1f : 0f;
            canvasGroup.interactable = IsRevealed;
            canvasGroup.blocksRaycasts = IsRevealed;
        }

        if (pinIcon != null)
        {
            if (IsCurrent)
            {
                pinIcon.color = currentColor;
                // Could also add pulsing animation here
            }
            else if (IsVisited)
            {
                pinIcon.color = visitedColor;
            }
            else
            {
                pinIcon.color = normalColor;
            }
        }

        // Show/hide label based on state
        if (labelText != null)
        {
            labelText.gameObject.SetActive(IsRevealed);
        }

        // Update quest indicator
        if (questIndicator != null)
        {
            questIndicator.gameObject.SetActive(HasQuest && IsRevealed);
        }
    }

    /// <summary>
    /// Handle button click.
    /// </summary>
    private void OnClick()
    {
        if (IsRevealed && onClickCallback != null)
        {
            onClickCallback.Invoke(PinData);
        }
    }

    /// <summary>
    /// Highlight on hover (called by EventTrigger).
    /// </summary>
    public void OnHoverEnter()
    {
        if (!IsRevealed) return;

        // Scale up slightly
        transform.localScale = Vector3.one * 1.2f;

        // Show label if hidden
        if (labelText != null)
        {
            labelText.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Remove highlight on hover exit.
    /// </summary>
    public void OnHoverExit()
    {
        // Reset scale
        transform.localScale = Vector3.one;

        // Hide label if normally hidden (for cleaner map)
        // Could be configurable
    }
}
