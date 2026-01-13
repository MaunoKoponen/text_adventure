#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Unity Editor window for placing and editing map pins.
/// Allows drag-and-drop positioning of location pins on a map image.
/// </summary>
public class MapEditorWindow : EditorWindow
{
    // Map data being edited
    private MapData editingMapData;
    private Texture2D mapTexture;
    private string currentMapPath;

    // View state
    private Vector2 scrollPosition;
    private float zoomLevel = 1f;
    private int selectedPinIndex = -1;
    private bool isDraggingPin = false;

    // UI sizes
    private const float PIN_SIZE = 20f;
    private const float SIDEBAR_WIDTH = 300f;

    [MenuItem("Tools/Map Editor")]
    public static void ShowWindow()
    {
        var window = GetWindow<MapEditorWindow>("Map Editor");
        window.minSize = new Vector2(800, 600);
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();

        // Left side: Map view
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width - SIDEBAR_WIDTH));
        DrawToolbar();
        DrawMapArea();
        EditorGUILayout.EndVertical();

        // Right side: Properties panel
        EditorGUILayout.BeginVertical(GUILayout.Width(SIDEBAR_WIDTH));
        DrawPropertiesPanel();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        HandleMapInput();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("New Map", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            CreateNewMap();
        }

        if (GUILayout.Button("Load Map", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            LoadMap();
        }

        if (GUILayout.Button("Save Map", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            SaveMap();
        }

        GUILayout.FlexibleSpace();

        // Zoom controls
        GUILayout.Label("Zoom:", GUILayout.Width(40));
        zoomLevel = EditorGUILayout.Slider(zoomLevel, 0.25f, 2f, GUILayout.Width(100));

        if (GUILayout.Button("Reset", EditorStyles.toolbarButton, GUILayout.Width(50)))
        {
            zoomLevel = 1f;
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawMapArea()
    {
        Rect mapAreaRect = GUILayoutUtility.GetRect(
            position.width - SIDEBAR_WIDTH - 20,
            position.height - 60);

        // Draw background
        EditorGUI.DrawRect(mapAreaRect, new Color(0.2f, 0.2f, 0.2f));

        if (mapTexture == null)
        {
            EditorGUI.LabelField(mapAreaRect, "Load a map image to begin editing",
                new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 14 });
            return;
        }

        // Calculate scaled map size
        float scaledWidth = mapTexture.width * zoomLevel;
        float scaledHeight = mapTexture.height * zoomLevel;

        // Scrollable view
        scrollPosition = GUI.BeginScrollView(
            mapAreaRect,
            scrollPosition,
            new Rect(0, 0, scaledWidth, scaledHeight));

        // Draw map image
        Rect mapRect = new Rect(0, 0, scaledWidth, scaledHeight);
        GUI.DrawTexture(mapRect, mapTexture);

        // Draw paths first (behind pins)
        DrawPaths();

        // Draw pins
        DrawPins();

        GUI.EndScrollView();
    }

    private void DrawPaths()
    {
        if (editingMapData?.paths == null) return;

        Handles.BeginGUI();

        foreach (var path in editingMapData.paths)
        {
            MapPinData fromPin = editingMapData.GetPin(path.fromLocationId);
            MapPinData toPin = editingMapData.GetPin(path.toLocationId);

            if (fromPin == null || toPin == null) continue;

            Vector2 start = fromPin.position * zoomLevel;
            Vector2 end = toPin.position * zoomLevel;

            // Draw dotted line
            Handles.color = new Color(0.6f, 0.5f, 0.3f, 0.8f);
            Handles.DrawDottedLine(
                new Vector3(start.x, start.y, 0),
                new Vector3(end.x, end.y, 0),
                4f);
        }

        Handles.EndGUI();
    }

    private void DrawPins()
    {
        if (editingMapData?.pins == null) return;

        for (int i = 0; i < editingMapData.pins.Count; i++)
        {
            var pin = editingMapData.pins[i];
            Vector2 pinPos = pin.position * zoomLevel;
            float size = PIN_SIZE * zoomLevel;

            Rect pinRect = new Rect(
                pinPos.x - size / 2,
                pinPos.y - size / 2,
                size, size);

            // Determine pin color
            Color pinColor = (i == selectedPinIndex) ? Color.yellow : Color.red;

            // Draw pin background
            EditorGUI.DrawRect(pinRect, pinColor);

            // Draw pin border
            Handles.BeginGUI();
            Handles.color = Color.black;
            Handles.DrawWireDisc(
                new Vector3(pinPos.x, pinPos.y, 0),
                Vector3.forward,
                size / 2);
            Handles.EndGUI();

            // Draw label below pin
            Rect labelRect = new Rect(
                pinPos.x - 50,
                pinPos.y + size / 2 + 2,
                100, 20);

            var labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = Color.white }
            };
            GUI.Label(labelRect, pin.displayName, labelStyle);
        }
    }

    private void DrawPropertiesPanel()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Map Properties", EditorStyles.boldLabel);

        if (editingMapData == null)
        {
            EditorGUILayout.HelpBox("No map loaded. Create or load a map to begin.", MessageType.Info);
            return;
        }

        // Map info
        editingMapData.mapId = EditorGUILayout.TextField("Map ID", editingMapData.mapId);
        editingMapData.regionId = EditorGUILayout.TextField("Region ID", editingMapData.regionId);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Map Image", EditorStyles.boldLabel);

        if (GUILayout.Button("Select Map Image"))
        {
            SelectMapImage();
        }

        if (mapTexture != null)
        {
            EditorGUILayout.LabelField($"Size: {mapTexture.width} x {mapTexture.height}");
        }

        EditorGUILayout.Space(20);
        DrawPinList();

        EditorGUILayout.Space(20);
        DrawSelectedPinProperties();
    }

    private void DrawPinList()
    {
        EditorGUILayout.LabelField("Pins", EditorStyles.boldLabel);

        if (GUILayout.Button("+ Add Pin"))
        {
            AddNewPin();
        }

        if (editingMapData?.pins == null) return;

        EditorGUILayout.BeginVertical("box");
        for (int i = 0; i < editingMapData.pins.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();

            bool isSelected = (i == selectedPinIndex);
            if (GUILayout.Toggle(isSelected, "", GUILayout.Width(20)))
            {
                selectedPinIndex = i;
            }

            var pin = editingMapData.pins[i];
            EditorGUILayout.LabelField(pin.displayName, GUILayout.ExpandWidth(true));

            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                DeletePin(i);
            }

            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawSelectedPinProperties()
    {
        if (selectedPinIndex < 0 || editingMapData?.pins == null ||
            selectedPinIndex >= editingMapData.pins.Count)
        {
            EditorGUILayout.HelpBox("Select a pin to edit its properties.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("Selected Pin", EditorStyles.boldLabel);

        var pin = editingMapData.pins[selectedPinIndex];

        pin.locationId = EditorGUILayout.TextField("Location ID", pin.locationId);
        pin.displayName = EditorGUILayout.TextField("Display Name", pin.displayName);

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Position (drag pin on map to change)");
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.Vector2Field("", pin.position);
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(5);
        pin.alwaysVisible = EditorGUILayout.Toggle("Always Visible", pin.alwaysVisible);
        pin.revealFlag = EditorGUILayout.TextField("Reveal Flag", pin.revealFlag);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Connections", EditorStyles.boldLabel);

        if (GUILayout.Button("+ Add Connection"))
        {
            AddConnectionFromSelected();
        }
    }

    private void HandleMapInput()
    {
        Event e = Event.current;

        if (mapTexture == null || editingMapData == null) return;

        // Calculate map area rect
        Rect mapAreaRect = new Rect(0, 40, position.width - SIDEBAR_WIDTH - 20, position.height - 60);

        if (!mapAreaRect.Contains(e.mousePosition)) return;

        // Adjust mouse position for scroll and zoom
        Vector2 mapPos = (e.mousePosition - new Vector2(mapAreaRect.x, mapAreaRect.y) + scrollPosition) / zoomLevel;

        // Right-click context menu
        if (e.type == EventType.ContextClick)
        {
            GenericMenu menu = new GenericMenu();
            Vector2 clickPos = mapPos;
            menu.AddItem(new GUIContent("Add Pin Here"), false, () => AddPinAtPosition(clickPos));
            menu.ShowAsContext();
            e.Use();
        }

        // Pin selection and dragging
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            int clickedPin = GetPinAtPosition(mapPos);
            if (clickedPin >= 0)
            {
                selectedPinIndex = clickedPin;
                isDraggingPin = true;
                e.Use();
                Repaint();
            }
        }

        if (e.type == EventType.MouseDrag && isDraggingPin && selectedPinIndex >= 0)
        {
            editingMapData.pins[selectedPinIndex].position = mapPos;
            e.Use();
            Repaint();
        }

        if (e.type == EventType.MouseUp)
        {
            isDraggingPin = false;
        }
    }

    private int GetPinAtPosition(Vector2 pos)
    {
        if (editingMapData?.pins == null) return -1;

        float hitRadius = PIN_SIZE / 2 / zoomLevel;

        for (int i = editingMapData.pins.Count - 1; i >= 0; i--)
        {
            if (Vector2.Distance(editingMapData.pins[i].position, pos) < hitRadius)
            {
                return i;
            }
        }

        return -1;
    }

    private void CreateNewMap()
    {
        editingMapData = new MapData
        {
            mapId = "new_map",
            regionId = "new_region",
            pins = new List<MapPinData>(),
            paths = new List<MapPathData>()
        };
        selectedPinIndex = -1;
        mapTexture = null;
        currentMapPath = null;
    }

    private void LoadMap()
    {
        string path = EditorUtility.OpenFilePanel("Load Map Data", "Assets/Resources/Maps", "json");
        if (string.IsNullOrEmpty(path)) return;

        string json = File.ReadAllText(path);
        editingMapData = JsonUtility.FromJson<MapData>(json);
        currentMapPath = path;

        // Try to load associated map image
        if (!string.IsNullOrEmpty(editingMapData.mapImagePath))
        {
            mapTexture = Resources.Load<Texture2D>(editingMapData.mapImagePath);
        }

        selectedPinIndex = -1;
        Repaint();
    }

    private void SaveMap()
    {
        if (editingMapData == null) return;

        string path = currentMapPath;
        if (string.IsNullOrEmpty(path))
        {
            path = EditorUtility.SaveFilePanel("Save Map Data", "Assets/Resources/Maps",
                editingMapData.mapId, "json");
        }

        if (string.IsNullOrEmpty(path)) return;

        string json = JsonUtility.ToJson(editingMapData, true);
        File.WriteAllText(path, json);
        currentMapPath = path;

        AssetDatabase.Refresh();
        Debug.Log($"Map saved to: {path}");
    }

    private void SelectMapImage()
    {
        string path = EditorUtility.OpenFilePanel("Select Map Image", "Assets/Resources", "png,jpg");
        if (string.IsNullOrEmpty(path)) return;

        // Convert to relative path for Resources.Load
        int resourcesIndex = path.IndexOf("Resources/");
        if (resourcesIndex >= 0)
        {
            string relativePath = path.Substring(resourcesIndex + "Resources/".Length);
            relativePath = relativePath.Substring(0, relativePath.LastIndexOf('.')); // Remove extension

            editingMapData.mapImagePath = relativePath;
            mapTexture = Resources.Load<Texture2D>(relativePath);

            if (mapTexture != null)
            {
                editingMapData.mapSize = new Vector2(mapTexture.width, mapTexture.height);
            }
        }
        else
        {
            EditorUtility.DisplayDialog("Invalid Path",
                "Map image must be inside a Resources folder.", "OK");
        }
    }

    private void AddNewPin()
    {
        if (editingMapData == null) return;

        Vector2 defaultPos = mapTexture != null
            ? new Vector2(mapTexture.width / 2, mapTexture.height / 2)
            : Vector2.zero;

        AddPinAtPosition(defaultPos);
    }

    private void AddPinAtPosition(Vector2 position)
    {
        if (editingMapData == null) return;

        int pinNum = editingMapData.pins.Count + 1;
        var newPin = new MapPinData
        {
            locationId = $"location_{pinNum}",
            displayName = $"Location {pinNum}",
            position = position,
            iconScale = 1f,
            alwaysVisible = false
        };

        editingMapData.pins.Add(newPin);
        selectedPinIndex = editingMapData.pins.Count - 1;
        Repaint();
    }

    private void DeletePin(int index)
    {
        if (editingMapData?.pins == null || index < 0 || index >= editingMapData.pins.Count)
            return;

        string deletedId = editingMapData.pins[index].locationId;

        // Remove any paths connected to this pin
        editingMapData.paths.RemoveAll(p =>
            p.fromLocationId == deletedId || p.toLocationId == deletedId);

        editingMapData.pins.RemoveAt(index);

        if (selectedPinIndex >= editingMapData.pins.Count)
            selectedPinIndex = editingMapData.pins.Count - 1;

        Repaint();
    }

    private void AddConnectionFromSelected()
    {
        if (selectedPinIndex < 0 || editingMapData?.pins == null) return;

        var fromPin = editingMapData.pins[selectedPinIndex];

        // Show menu of other pins to connect to
        GenericMenu menu = new GenericMenu();

        for (int i = 0; i < editingMapData.pins.Count; i++)
        {
            if (i == selectedPinIndex) continue;

            var toPin = editingMapData.pins[i];

            // Check if connection already exists
            bool exists = editingMapData.paths.Exists(p =>
                (p.fromLocationId == fromPin.locationId && p.toLocationId == toPin.locationId) ||
                (p.fromLocationId == toPin.locationId && p.toLocationId == fromPin.locationId));

            if (exists) continue;

            int targetIndex = i;
            menu.AddItem(new GUIContent(toPin.displayName), false, () =>
            {
                var newPath = new MapPathData
                {
                    pathId = $"path_{fromPin.locationId}_{editingMapData.pins[targetIndex].locationId}",
                    fromLocationId = fromPin.locationId,
                    toLocationId = editingMapData.pins[targetIndex].locationId,
                    style = PathStyle.Dotted,
                    pathWidth = 2f,
                    visibleWhenBothRevealed = true
                };
                editingMapData.paths.Add(newPath);
                Repaint();
            });
        }

        menu.ShowAsContext();
    }
}
#endif
