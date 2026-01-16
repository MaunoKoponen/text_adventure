using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WorldGen
{
    /// <summary>
    /// Developer UI for world generation. Toggle with F12.
    /// Builds its own UI at runtime - no manual setup required.
    /// Only active in Editor or Development builds.
    /// </summary>
    public class WorldGenDevUI : MonoBehaviour
    {
        // UI References (created at runtime)
        private GameObject panelRoot;
        private TMP_InputField worldNameInput;
        private TMP_Dropdown themeDropdown;
        private TMP_Dropdown toneDropdown;
        private TMP_InputField settingInput;
        private TMP_InputField conflictInput;
        private TMP_Dropdown providerDropdown;
        private TMP_InputField modelInput;
        private TMP_InputField apiKeyInput;
        private TMP_InputField outputFolderInput;
        private Slider chaptersSlider;
        private Slider locationsSlider;
        private Slider questsSlider;
        private TMP_Text chaptersLabel;
        private TMP_Text locationsLabel;
        private TMP_Text questsLabel;
        private Toggle hardQuestsToggle;
        private Slider progressBar;
        private TMP_Text statusText;
        private TMP_Text logText;
        private ScrollRect logScroll;
        private Button generateButton;
        private Button generateChapterButton;
        private Button cancelButton;

        // References
        private WorldGenerator generator;
        private Canvas canvas;
        private StringBuilder logBuilder = new StringBuilder();

        // Styling
        private Color panelColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        private Color headerColor = new Color(0.2f, 0.4f, 0.6f, 1f);
        private Color buttonColor = new Color(0.2f, 0.5f, 0.3f, 1f);
        private Color inputBgColor = new Color(0.15f, 0.15f, 0.2f, 1f);

        private void Start()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Initialize();
#else
            gameObject.SetActive(false);
#endif
        }

        private void Initialize()
        {
            // Find or create WorldGenerator
            generator = FindObjectOfType<WorldGenerator>();
            if (generator == null)
            {
                var go = new GameObject("WorldGenerator");
                generator = go.AddComponent<WorldGenerator>();
                go.AddComponent<LLMService>();
            }

            // Subscribe to events
            generator.OnStatusUpdate += OnStatusUpdate;
            generator.OnProgressUpdate += OnProgressUpdate;
            generator.OnChapterGenerated += OnChapterGenerated;
            generator.OnGenerationComplete += OnGenerationComplete;
            generator.OnError += OnError;
            generator.OnValidationErrors += OnValidationErrors;

            
            var canvasGo = new GameObject("WorldGenCanvas");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            canvasGo.AddComponent<GraphicRaycaster>();
          

            BuildUI();
            LoadSettings();
            panelRoot.SetActive(false);

            Log("WorldGen Dev UI ready. Press F12 to toggle.");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F12))
            {
                TogglePanel();
            }
        }

        public void TogglePanel()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(!panelRoot.activeSelf);
            }
        }

        private void BuildUI()
        {
            // Main panel
            panelRoot = CreatePanel("WorldGenPanel", canvas.transform, panelColor);
            var panelRect = panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.05f, 0.05f);
            panelRect.anchorMax = new Vector2(0.95f, 0.95f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // Main layout
            var mainLayout = panelRoot.AddComponent<VerticalLayoutGroup>();
            mainLayout.padding = new RectOffset(15, 15, 15, 15);
            mainLayout.spacing = 10;
            mainLayout.childForceExpandWidth = true;
            mainLayout.childForceExpandHeight = false;
            mainLayout.childControlHeight = true;
            mainLayout.childControlWidth = true;

            // Header
            var header = CreateHeader("World Generation (F12 to close)");

            // Scroll view for content
            var scrollView = CreateScrollView(panelRoot.transform);
            var content = scrollView.content.gameObject;
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(5, 5, 5, 5);
            contentLayout.spacing = 8;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            // === World Prompt Section ===
            CreateSectionHeader(content.transform, "World Prompt");

            worldNameInput = CreateInputField(content.transform, "World Name", "Enter world name...");
            themeDropdown = CreateDropdown(content.transform, "Theme",
                new[] { "Dark Fantasy", "High Fantasy", "Steampunk", "Horror", "Sci-Fi", "Post-Apocalyptic" });
            toneDropdown = CreateDropdown(content.transform, "Tone",
                new[] { "Gritty", "Whimsical", "Serious", "Epic", "Mysterious", "Humorous" });
            settingInput = CreateInputField(content.transform, "Setting Description", "Describe the world...", 80);
            conflictInput = CreateInputField(content.transform, "Main Conflict", "Central tension...");

            // === Generation Settings ===
            CreateSectionHeader(content.transform, "Generation Settings");

            var chaptersRow = CreateSliderRow(content.transform, "Chapters", 1, 10, 5);
            chaptersSlider = chaptersRow.slider;
            chaptersLabel = chaptersRow.label;

            var locationsRow = CreateSliderRow(content.transform, "Locations/Chapter", 5, 20, 10);
            locationsSlider = locationsRow.slider;
            locationsLabel = locationsRow.label;

            var questsRow = CreateSliderRow(content.transform, "Quests/Chapter", 3, 15, 7);
            questsSlider = questsRow.slider;
            questsLabel = questsRow.label;

            hardQuestsToggle = CreateToggle(content.transform, "Allow Hard Side Quests", true);

            // === API Settings ===
            CreateSectionHeader(content.transform, "API Settings");

            providerDropdown = CreateDropdown(content.transform, "Provider", new[] { "Anthropic", "OpenAI"  });
            providerDropdown.onValueChanged.AddListener(OnProviderChanged);

            modelInput = CreateInputField(content.transform, "Model", "claude-3-haiku-20240307");
            apiKeyInput = CreateInputField(content.transform, "API Key", "sk-...");
            apiKeyInput.contentType = TMP_InputField.ContentType.Password;

            outputFolderInput = CreateInputField(content.transform, "Output Folder", "generated_world");

            // === Progress Section ===
            CreateSectionHeader(content.transform, "Progress");

            statusText = CreateLabel(content.transform, "Ready", 14);
            progressBar = CreateProgressBar(content.transform);

            // === Log Section ===
            CreateSectionHeader(content.transform, "Log");
            var logScrollView = CreateLogArea(content.transform);
            logScroll = logScrollView.GetComponent<ScrollRect>();
            logText = logScrollView.content.GetComponentInChildren<TMP_Text>();

            // === Buttons ===
            var buttonRow = CreateButtonRow(content.transform);

            generateButton = CreateButton(buttonRow.transform, "Generate World", buttonColor, StartGeneration);
            generateChapterButton = CreateButton(buttonRow.transform, "Next Chapter", buttonColor, StartChapterGeneration);
            cancelButton = CreateButton(buttonRow.transform, "Cancel", new Color(0.6f, 0.2f, 0.2f, 1f), CancelGeneration);
            cancelButton.interactable = false;

            // Close button in header
            var closeBtn = CreateButton(header.transform, "X", new Color(0.5f, 0.2f, 0.2f, 1f), () => panelRoot.SetActive(false));
            var closeBtnRect = closeBtn.GetComponent<RectTransform>();
            closeBtnRect.anchorMin = new Vector2(1, 0.5f);
            closeBtnRect.anchorMax = new Vector2(1, 0.5f);
            closeBtnRect.pivot = new Vector2(1, 0.5f);
            closeBtnRect.sizeDelta = new Vector2(40, 30);
            closeBtnRect.anchoredPosition = new Vector2(-5, 0);
        }

        #region UI Creation Helpers

        private GameObject CreatePanel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            var image = go.AddComponent<Image>();
            image.color = color;

            return go;
        }

        private GameObject CreateHeader(string text)
        {
            var header = CreatePanel("Header", panelRoot.transform, headerColor);
            var headerRect = header.GetComponent<RectTransform>();
            var headerLayout = header.AddComponent<LayoutElement>();
            headerLayout.minHeight = 40;
            headerLayout.preferredHeight = 40;

            var headerText = CreateLabel(header.transform, text, 18, TextAlignmentOptions.Center);
            var textRect = headerText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return header;
        }

        private void CreateSectionHeader(Transform parent, string text)
        {
            var header = new GameObject("Section_" + text);
            header.transform.SetParent(parent, false);

            var rect = header.AddComponent<RectTransform>();
            var layout = header.AddComponent<LayoutElement>();
            layout.minHeight = 25;
            layout.preferredHeight = 25;

            var label = CreateLabel(header.transform, "— " + text + " —", 14, TextAlignmentOptions.Center);
            label.color = new Color(0.7f, 0.8f, 1f, 1f);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }

        private ScrollRect CreateScrollView(Transform parent)
        {
            var scrollGo = new GameObject("ScrollView");
            scrollGo.transform.SetParent(parent, false);

            var scrollRect = scrollGo.AddComponent<RectTransform>();
            var scrollLayout = scrollGo.AddComponent<LayoutElement>();
            scrollLayout.flexibleHeight = 1;
            scrollLayout.minHeight = 100;

            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRect = viewportGo.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            var viewportMask = viewportGo.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;
            var viewportImage = viewportGo.AddComponent<Image>();
            viewportImage.color = Color.white;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            var contentFitter = contentGo.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewportRect;
            scroll.content = contentRect;

            return scroll;
        }

        private TMP_InputField CreateInputField(Transform parent, string label, string placeholder, float height = 35)
        {
            var row = new GameObject("Input_" + label);
            row.transform.SetParent(parent, false);

            var rowRect = row.AddComponent<RectTransform>();
            var rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.minHeight = height;
            rowLayout.preferredHeight = height;

            var horizontal = row.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 10;
            horizontal.childForceExpandWidth = true;
            horizontal.childControlWidth = true;
            horizontal.childForceExpandHeight = true;

            // Label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(row.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            var labelLayout = labelGo.AddComponent<LayoutElement>();
            labelLayout.minWidth = 120;
            labelLayout.preferredWidth = 120;

            var labelText = labelGo.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 14;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            labelText.color = Color.white;

            // Input field
            var inputGo = new GameObject("InputField");
            inputGo.transform.SetParent(row.transform, false);

            var inputRect = inputGo.AddComponent<RectTransform>();
            var inputLayout = inputGo.AddComponent<LayoutElement>();
            inputLayout.flexibleWidth = 1;

            var inputBg = inputGo.AddComponent<Image>();
            inputBg.color = inputBgColor;

            var input = inputGo.AddComponent<TMP_InputField>();

            // Text area
            var textArea = new GameObject("TextArea");
            textArea.transform.SetParent(inputGo.transform, false);
            var textAreaRect = textArea.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(5, 2);
            textAreaRect.offsetMax = new Vector2(-5, -2);

            // Placeholder
            var placeholderGo = new GameObject("Placeholder");
            placeholderGo.transform.SetParent(textArea.transform, false);
            var placeholderRect = placeholderGo.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;
            var placeholderText = placeholderGo.AddComponent<TextMeshProUGUI>();
            placeholderText.text = placeholder;
            placeholderText.fontSize = 14;
            placeholderText.fontStyle = FontStyles.Italic;
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
            placeholderText.alignment = TextAlignmentOptions.MidlineLeft;

            // Text
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(textArea.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var textComponent = textGo.AddComponent<TextMeshProUGUI>();
            textComponent.fontSize = 14;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.MidlineLeft;

            input.textViewport = textAreaRect;
            input.textComponent = textComponent;
            input.placeholder = placeholderText;

            return input;
        }

        private TMP_Dropdown CreateDropdown(Transform parent, string label, string[] options)
        {
            var row = new GameObject("Dropdown_" + label);
            row.transform.SetParent(parent, false);

            var rowRect = row.AddComponent<RectTransform>();
            var rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.minHeight = 35;
            rowLayout.preferredHeight = 35;

            var horizontal = row.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 10;
            horizontal.childForceExpandWidth = true;
            horizontal.childControlWidth = true;
            horizontal.childForceExpandHeight = true;

            // Label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(row.transform, false);
            var labelLayout = labelGo.AddComponent<LayoutElement>();
            labelLayout.minWidth = 120;
            labelLayout.preferredWidth = 120;

            var labelText = labelGo.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 14;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            labelText.color = Color.white;

            // Dropdown
            var dropdownGo = new GameObject("Dropdown");
            dropdownGo.transform.SetParent(row.transform, false);

            var dropdownRect = dropdownGo.AddComponent<RectTransform>();
            var dropdownLayout = dropdownGo.AddComponent<LayoutElement>();
            dropdownLayout.flexibleWidth = 1;

            var dropdownBg = dropdownGo.AddComponent<Image>();
            dropdownBg.color = inputBgColor;

            var dropdown = dropdownGo.AddComponent<TMP_Dropdown>();

            // Caption
            var captionGo = new GameObject("Caption");
            captionGo.transform.SetParent(dropdownGo.transform, false);
            var captionRect = captionGo.AddComponent<RectTransform>();
            captionRect.anchorMin = Vector2.zero;
            captionRect.anchorMax = Vector2.one;
            captionRect.offsetMin = new Vector2(10, 2);
            captionRect.offsetMax = new Vector2(-25, -2);
            var captionText = captionGo.AddComponent<TextMeshProUGUI>();
            captionText.fontSize = 14;
            captionText.color = Color.white;
            captionText.alignment = TextAlignmentOptions.MidlineLeft;

            dropdown.captionText = captionText;

            // Template (simplified - uses built-in)
            var templateGo = new GameObject("Template");
            templateGo.transform.SetParent(dropdownGo.transform, false);
            templateGo.SetActive(false);
            var templateRect = templateGo.AddComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.pivot = new Vector2(0.5f, 1);
            templateRect.sizeDelta = new Vector2(0, 150);
            var templateBg = templateGo.AddComponent<Image>();
            templateBg.color = new Color(0.2f, 0.2f, 0.25f, 1f);
            var templateScroll = templateGo.AddComponent<ScrollRect>();

            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(templateGo.transform, false);
            var viewportRect = viewportGo.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            var viewportMask = viewportGo.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;
            viewportGo.AddComponent<Image>().color = Color.white;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 28);

            var itemGo = new GameObject("Item");
            itemGo.transform.SetParent(contentGo.transform, false);
            var itemRect = itemGo.AddComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0, 0.5f);
            itemRect.anchorMax = new Vector2(1, 0.5f);
            itemRect.sizeDelta = new Vector2(0, 28);
            var itemToggle = itemGo.AddComponent<Toggle>();

            var itemBgGo = new GameObject("Background");
            itemBgGo.transform.SetParent(itemGo.transform, false);
            var itemBgRect = itemBgGo.AddComponent<RectTransform>();
            itemBgRect.anchorMin = Vector2.zero;
            itemBgRect.anchorMax = Vector2.one;
            itemBgRect.offsetMin = Vector2.zero;
            itemBgRect.offsetMax = Vector2.zero;
            var itemBg = itemBgGo.AddComponent<Image>();
            itemBg.color = new Color(0.25f, 0.25f, 0.3f, 1f);

            var itemLabelGo = new GameObject("Label");
            itemLabelGo.transform.SetParent(itemGo.transform, false);
            var itemLabelRect = itemLabelGo.AddComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new Vector2(10, 0);
            itemLabelRect.offsetMax = new Vector2(-10, 0);
            var itemLabel = itemLabelGo.AddComponent<TextMeshProUGUI>();
            itemLabel.fontSize = 14;
            itemLabel.color = Color.white;
            itemLabel.alignment = TextAlignmentOptions.MidlineLeft;

            itemToggle.targetGraphic = itemBg;

            templateScroll.viewport = viewportRect;
            templateScroll.content = contentRect;

            dropdown.template = templateRect;
            dropdown.itemText = itemLabel;

            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(options));

            return dropdown;
        }

        private (Slider slider, TMP_Text label) CreateSliderRow(Transform parent, string labelText, float min, float max, float defaultValue)
        {
            var row = new GameObject("Slider_" + labelText);
            row.transform.SetParent(parent, false);

            var rowRect = row.AddComponent<RectTransform>();
            var rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.minHeight = 30;
            rowLayout.preferredHeight = 30;

            var horizontal = row.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 10;
            horizontal.childForceExpandWidth = false;
            horizontal.childControlWidth = true;
            horizontal.childForceExpandHeight = true;

            // Label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(row.transform, false);
            var labelLayout = labelGo.AddComponent<LayoutElement>();
            labelLayout.minWidth = 120;
            labelLayout.preferredWidth = 120;

            var label = labelGo.AddComponent<TextMeshProUGUI>();
            label.text = labelText;
            label.fontSize = 14;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = Color.white;

            // Slider
            var sliderGo = new GameObject("Slider");
            sliderGo.transform.SetParent(row.transform, false);

            var sliderLayout = sliderGo.AddComponent<LayoutElement>();
            sliderLayout.flexibleWidth = 1;
            sliderLayout.minWidth = 100;

            // Background
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(sliderGo.transform, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.25f);
            bgRect.anchorMax = new Vector2(1, 0.75f);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImage = bgGo.AddComponent<Image>();
            bgImage.color = inputBgColor;

            // Fill Area
            var fillAreaGo = new GameObject("FillArea");
            fillAreaGo.transform.SetParent(sliderGo.transform, false);
            var fillAreaRect = fillAreaGo.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1, 0.75f);
            fillAreaRect.offsetMin = new Vector2(5, 0);
            fillAreaRect.offsetMax = new Vector2(-5, 0);

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(fillAreaGo.transform, false);
            var fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImage = fillGo.AddComponent<Image>();
            fillImage.color = headerColor;

            // Handle
            var handleAreaGo = new GameObject("HandleArea");
            handleAreaGo.transform.SetParent(sliderGo.transform, false);
            var handleAreaRect = handleAreaGo.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10, 0);
            handleAreaRect.offsetMax = new Vector2(-10, 0);

            var handleGo = new GameObject("Handle");
            handleGo.transform.SetParent(handleAreaGo.transform, false);
            var handleRect = handleGo.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 0);
            var handleImage = handleGo.AddComponent<Image>();
            handleImage.color = Color.white;

            var slider = sliderGo.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = true;
            slider.value = defaultValue;

            // Value label
            var valueLabelGo = new GameObject("Value");
            valueLabelGo.transform.SetParent(row.transform, false);
            var valueLabelLayout = valueLabelGo.AddComponent<LayoutElement>();
            valueLabelLayout.minWidth = 40;
            valueLabelLayout.preferredWidth = 40;

            var valueLabel = valueLabelGo.AddComponent<TextMeshProUGUI>();
            valueLabel.text = defaultValue.ToString();
            valueLabel.fontSize = 14;
            valueLabel.alignment = TextAlignmentOptions.MidlineRight;
            valueLabel.color = Color.white;

            slider.onValueChanged.AddListener(v => valueLabel.text = ((int)v).ToString());

            return (slider, valueLabel);
        }

        private Toggle CreateToggle(Transform parent, string labelText, bool defaultValue)
        {
            var row = new GameObject("Toggle_" + labelText);
            row.transform.SetParent(parent, false);

            var rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.minHeight = 30;
            rowLayout.preferredHeight = 30;

            var horizontal = row.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 10;
            horizontal.childForceExpandWidth = true;
            horizontal.childControlWidth = true;
            horizontal.childForceExpandHeight = true;

            // Label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(row.transform, false);
            var labelLayout = labelGo.AddComponent<LayoutElement>();
            labelLayout.minWidth = 120;
            labelLayout.flexibleWidth = 1;

            var label = labelGo.AddComponent<TextMeshProUGUI>();
            label.text = labelText;
            label.fontSize = 14;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = Color.white;

            // Toggle
            var toggleGo = new GameObject("Toggle");
            toggleGo.transform.SetParent(row.transform, false);

            var toggleLayout = toggleGo.AddComponent<LayoutElement>();
            toggleLayout.minWidth = 30;
            toggleLayout.preferredWidth = 30;

            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(toggleGo.transform, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.5f, 0.5f);
            bgRect.anchorMax = new Vector2(0.5f, 0.5f);
            bgRect.sizeDelta = new Vector2(24, 24);
            var bgImage = bgGo.AddComponent<Image>();
            bgImage.color = inputBgColor;

            var checkGo = new GameObject("Checkmark");
            checkGo.transform.SetParent(bgGo.transform, false);
            var checkRect = checkGo.AddComponent<RectTransform>();
            checkRect.anchorMin = Vector2.zero;
            checkRect.anchorMax = Vector2.one;
            checkRect.offsetMin = new Vector2(4, 4);
            checkRect.offsetMax = new Vector2(-4, -4);
            var checkImage = checkGo.AddComponent<Image>();
            checkImage.color = headerColor;

            var toggle = toggleGo.AddComponent<Toggle>();
            toggle.targetGraphic = bgImage;
            toggle.graphic = checkImage;
            toggle.isOn = defaultValue;

            return toggle;
        }

        private TMP_Text CreateLabel(Transform parent, string text, int fontSize, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = Color.white;

            return label;
        }

        private Slider CreateProgressBar(Transform parent)
        {
            var row = new GameObject("ProgressBar");
            row.transform.SetParent(parent, false);

            var rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.minHeight = 20;
            rowLayout.preferredHeight = 20;

            var bgImage = row.AddComponent<Image>();
            bgImage.color = inputBgColor;

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(row.transform, false);
            var fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImage = fillGo.AddComponent<Image>();
            fillImage.color = buttonColor;

            var slider = row.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = 0;
            slider.interactable = false;

            return slider;
        }

        private ScrollRect CreateLogArea(Transform parent)
        {
            var scrollGo = new GameObject("LogScroll");
            scrollGo.transform.SetParent(parent, false);

            var scrollLayout = scrollGo.AddComponent<LayoutElement>();
            scrollLayout.minHeight = 120;
            scrollLayout.preferredHeight = 120;

            var scrollBg = scrollGo.AddComponent<Image>();
            scrollBg.color = new Color(0.05f, 0.05f, 0.08f, 1f);

            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;

            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRect = viewportGo.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(5, 5);
            viewportRect.offsetMax = new Vector2(-5, -5);
            var viewportMask = viewportGo.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;
            viewportGo.AddComponent<Image>().color = Color.white;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0, 1);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            var contentFitter = contentGo.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var textGo = new GameObject("LogText");
            textGo.transform.SetParent(contentGo.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.fontSize = 12;
            text.color = new Color(0.7f, 0.9f, 0.7f, 1f);
            text.alignment = TextAlignmentOptions.TopLeft;

            scroll.viewport = viewportRect;
            scroll.content = contentRect;

            return scroll;
        }

        private GameObject CreateButtonRow(Transform parent)
        {
            var row = new GameObject("ButtonRow");
            row.transform.SetParent(parent, false);

            var rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.minHeight = 40;
            rowLayout.preferredHeight = 40;

            var horizontal = row.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 10;
            horizontal.childForceExpandWidth = true;
            horizontal.childControlWidth = true;
            horizontal.childForceExpandHeight = true;

            return row;
        }

        private Button CreateButton(Transform parent, string text, Color color, Action onClick)
        {
            var go = new GameObject("Button_" + text);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            var layout = go.AddComponent<LayoutElement>();
            layout.minHeight = 35;
            layout.flexibleWidth = 1;

            var image = go.AddComponent<Image>();
            image.color = color;

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick?.Invoke());

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var buttonText = textGo.AddComponent<TextMeshProUGUI>();
            buttonText.text = text;
            buttonText.fontSize = 14;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.color = Color.white;

            return button;
        }

        #endregion

        #region Generation Methods

        private void StartGeneration()
        {
            if (string.IsNullOrEmpty(apiKeyInput?.text))
            {
                Log("ERROR: API key is required");
                return;
            }

            var config = BuildConfig();
            SaveSettings();

            Log($"Starting generation: {config.worldPrompt.worldName}");
            SetGeneratingState(true);

            generator.outputStoryId = outputFolderInput?.text ?? "generated_world";
            generator.StartGeneration(config, apiKeyInput.text);
        }

        private void StartChapterGeneration()
        {
            if (string.IsNullOrEmpty(apiKeyInput?.text))
            {
                Log("ERROR: API key is required");
                return;
            }

            if (generator.currentConfig == null)
            {
                Log("ERROR: No world loaded. Generate a world first.");
                return;
            }

            Log("Generating next chapter...");
            SetGeneratingState(true);
            generator.GenerateNextChapter(apiKeyInput.text);
        }

        private void CancelGeneration()
        {
            generator.CancelGeneration();
            SetGeneratingState(false);
            Log("Cancelled.");
        }

        private WorldGenerationConfig BuildConfig()
        {
            return new WorldGenerationConfig
            {
                configId = $"gen_{DateTime.Now:yyyyMMdd_HHmmss}",
                configName = worldNameInput?.text ?? "Generated World",
                createdAt = DateTime.Now.ToString("o"),
                generatedBy = providerDropdown?.options[providerDropdown.value].text.ToLower() ?? "openai",

                worldPrompt = new WorldPrompt
                {
                    worldName = worldNameInput?.text ?? "Unnamed World",
                    theme = themeDropdown?.options[themeDropdown.value].text ?? "Dark Fantasy",
                    tone = toneDropdown?.options[toneDropdown.value].text ?? "Gritty",
                    settingDescription = settingInput?.text ?? "",
                    mainConflict = conflictInput?.text ?? "",
                    writingStyle = "Descriptive",
                    dialogueTone = toneDropdown?.options[toneDropdown.value].text ?? "Gritty"
                },

                settings = new GenerationSettings
                {
                    totalChapters = (int)(chaptersSlider?.value ?? 5),
                    locationsPerChapter = (int)(locationsSlider?.value ?? 10),
                    questsPerChapter = (int)(questsSlider?.value ?? 7),
                    mainQuestsPerChapter = 2,
                    allowHardSideQuests = hardQuestsToggle?.isOn ?? true
                },

                apiConfig = new LLMApiConfig
                {
                    provider = providerDropdown?.options[providerDropdown.value].text.ToLower() ?? "openai",
                    model = modelInput?.text ?? "gpt-4",
                    temperature = 0.7f,
                    maxTokensPerRequest = 4000,
                    requestDelayMs = 1000,
                    maxRetries = 3
                }
            };
        }

        private void SetGeneratingState(bool generating)
        {
            if (generateButton != null) generateButton.interactable = !generating;
            if (generateChapterButton != null) generateChapterButton.interactable = !generating;
            if (cancelButton != null) cancelButton.interactable = generating;
        }

        #endregion

        #region Event Handlers

        private void OnProviderChanged(int index)
        {
            if (modelInput == null) return;
            string provider = providerDropdown.options[index].text.ToLower();
            modelInput.text = provider == "openai" ? "gpt-4" : "claude-3-opus-20240229";
        }

        private void OnStatusUpdate(string status)
        {
            Log(status);
            if (statusText != null) statusText.text = status;
        }

        private void OnProgressUpdate(float progress)
        {
            if (progressBar != null) progressBar.value = progress;
        }

        private void OnChapterGenerated(ChapterData chapter)
        {
            Log($"Chapter {chapter.chapterNumber} complete: {chapter.chapterName}");
        }

        private void OnGenerationComplete(WorldGenerationConfig config)
        {
            Log("=== GENERATION COMPLETE ===");
            Log($"Output: Stories/{generator.outputStoryId}/");
            SetGeneratingState(false);
        }

        private void OnError(string error)
        {
            Log($"ERROR: {error}");
            SetGeneratingState(false);
        }

        private void OnValidationErrors(List<string> errors)
        {
            Log($"Validation: {errors.Count} issues found");
        }

        #endregion

        #region Logging & Settings

        private void Log(string message)
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            logBuilder.AppendLine($"[{time}] {message}");

            if (logText != null)
            {
                logText.text = logBuilder.ToString();
                Canvas.ForceUpdateCanvases();
                if (logScroll != null) logScroll.verticalNormalizedPosition = 0f;
            }

            Debug.Log($"[WorldGen] {message}");
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetString("WG_Provider", providerDropdown?.options[providerDropdown.value].text ?? "OpenAI");
            PlayerPrefs.SetString("WG_Model", modelInput?.text ?? "gpt-4");
            PlayerPrefs.SetString("WG_Output", outputFolderInput?.text ?? "generated_world");
#if UNITY_EDITOR
            UnityEditor.EditorPrefs.SetString("WG_ApiKey", apiKeyInput?.text ?? "");
#endif
            PlayerPrefs.Save();
        }

        private void LoadSettings()
        {
            if (modelInput != null)
                modelInput.text = PlayerPrefs.GetString("WG_Model", "gpt-4");
            if (outputFolderInput != null)
                outputFolderInput.text = PlayerPrefs.GetString("WG_Output", "generated_world");
#if UNITY_EDITOR
            if (apiKeyInput != null)
                apiKeyInput.text = UnityEditor.EditorPrefs.GetString("WG_ApiKey", "");
#endif
        }

        private void OnDestroy()
        {
            if (generator != null)
            {
                generator.OnStatusUpdate -= OnStatusUpdate;
                generator.OnProgressUpdate -= OnProgressUpdate;
                generator.OnChapterGenerated -= OnChapterGenerated;
                generator.OnGenerationComplete -= OnGenerationComplete;
                generator.OnError -= OnError;
                generator.OnValidationErrors -= OnValidationErrors;
            }
        }

        #endregion
    }
}
