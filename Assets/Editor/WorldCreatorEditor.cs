//#if UNITY_EDITOR
//using UnityEngine;
//using UnityEditor;
//using System.Collections.Generic;

//[CustomEditor(typeof(WorldCreatorManager))]
//public class WorldCreatorEditor : Editor
//{
//    private WorldCreatorManager manager;
//    private bool showGridSettings = true;
//    private bool showPrefabSettings = true;
//    private bool showLayerSettings = true;
//    private bool showUISettings = true;

//    void OnEnable()
//    {
//        manager = (WorldCreatorManager)target;
//    }

//    public override void OnInspectorGUI()
//    {
//        serializedObject.Update();

//        EditorGUILayout.Space();
//        EditorGUILayout.LabelField("World Creator Manager", EditorStyles.boldLabel);
//        EditorGUILayout.Space();

//        DrawGridSettings();
//        EditorGUILayout.Space();

//        DrawPrefabSettings();
//        EditorGUILayout.Space();

//        DrawUISettings();
//        EditorGUILayout.Space();

//        DrawUtilityButtons();

//        serializedObject.ApplyModifiedProperties();
//    }

//    void DrawGridSettings()
//    {
//        showGridSettings = EditorGUILayout.Foldout(showGridSettings, "Grid Settings", true);
//        if (showGridSettings)
//        {
//            EditorGUI.indentLevel++;

//            EditorGUILayout.PropertyField(serializedObject.FindProperty("gridSize"));
//            EditorGUILayout.PropertyField(serializedObject.FindProperty("gridWidth"));
//            EditorGUILayout.PropertyField(serializedObject.FindProperty("gridHeight"));
//            EditorGUILayout.PropertyField(serializedObject.FindProperty("gridMaterial"));

//            EditorGUILayout.Space();
//            EditorGUILayout.LabelField("Grid Info:", EditorStyles.miniBoldLabel);
//            EditorGUILayout.LabelField($"Total Cells: {manager.gridWidth * manager.gridHeight}");
//            EditorGUILayout.LabelField($"World Size: {manager.gridWidth * manager.gridSize}x{manager.gridHeight * manager.gridSize}");

//            EditorGUI.indentLevel--;
//        }
//    }

//    void DrawPrefabSettings()
//    {
//        showPrefabSettings = EditorGUILayout.Foldout(showPrefabSettings, "Prefab Settings", true);
//        if (showPrefabSettings)
//        {
//            EditorGUI.indentLevel++;

//            SerializedProperty prefabsProperty = serializedObject.FindProperty("availablePrefabs");

//            EditorGUILayout.PropertyField(prefabsProperty, new GUIContent("Available Prefabs"), true);

//            // Botones de utilidad para prefabs
//            EditorGUILayout.Space();
//            EditorGUILayout.BeginHorizontal();

//            if (GUILayout.Button("Add Prefab Slot"))
//            {
//                prefabsProperty.arraySize++;
//            }

//            if (GUILayout.Button("Sort by Layer"))
//            {
//                SortPrefabsByLayer();
//            }

//            EditorGUILayout.EndHorizontal();

//            // Mostrar resumen por capas
//            ShowPrefabSummary();

//            EditorGUI.indentLevel--;
//        }
//    }

//    void DrawUISettings()
//    {
//        showUISettings = EditorGUILayout.Foldout(showUISettings, "UI Settings", true);
//        if (showUISettings)
//        {
//            EditorGUI.indentLevel++;

//            EditorGUILayout.PropertyField(serializedObject.FindProperty("uiManager"));

//            EditorGUILayout.Space();
//            if (manager.uiManager == null)
//            {
//                EditorGUILayout.HelpBox("UI Manager no asignado. Crea un GameObject con el componente WorldCreatorUI.", MessageType.Warning);

//                if (GUILayout.Button("Create UI Manager"))
//                {
//                    CreateUIManager();
//                }
//            }

//            EditorGUI.indentLevel--;
//        }
//    }

//    void DrawUtilityButtons()
//    {
//        EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);

//        EditorGUILayout.BeginHorizontal();

//        if (GUILayout.Button("Create Grid Visual"))
//        {
//            if (Application.isPlaying)
//            {
//                manager.Invoke("CreateGridVisual", 0);
//            }
//            else
//            {
//                EditorUtility.DisplayDialog("Info", "Esta función solo está disponible en tiempo de ejecución.", "OK");
//            }
//        }

//        if (GUILayout.Button("Validate Prefabs"))
//        {
//            ValidatePrefabs();
//        }

//        EditorGUILayout.EndHorizontal();

//        EditorGUILayout.BeginHorizontal();

//        if (GUILayout.Button("Clear World"))
//        {
//            if (Application.isPlaying)
//            {
//                if (EditorUtility.DisplayDialog("Clear World", "¿Estás seguro de que quieres limpiar el mundo?", "Sí", "No"))
//                {
//                    manager.ClearWorld();
//                }
//            }
//        }

//        if (GUILayout.Button("Setup Scene"))
//        {
//            SetupScene();
//        }

//        EditorGUILayout.EndHorizontal();
//    }

//    void ShowPrefabSummary()
//    {
//        if (manager.availablePrefabs == null) return;

//        EditorGUILayout.Space();
//        EditorGUILayout.LabelField("Prefab Summary:", EditorStyles.miniBoldLabel);

//        int[] layerCounts = new int[3];
//        int totalPrefabs = 0;

//        foreach (PrefabData prefab in manager.availablePrefabs)
//        {
//            if (prefab != null && prefab.prefab != null)
//            {
//                if (prefab.layer >= 0 && prefab.layer < 3)
//                {
//                    layerCounts[prefab.layer]++;
//                }
//                totalPrefabs++;
//            }
//        }

//        EditorGUI.indentLevel++;
//        EditorGUILayout.LabelField($"Layer 0 (Suelo): {layerCounts[0]} prefabs");
//        EditorGUILayout.LabelField($"Layer 1 (Objetos): {layerCounts[1]} prefabs");
//        EditorGUILayout.LabelField($"Layer 2 (Decoración): {layerCounts[2]} prefabs");
//        EditorGUILayout.LabelField($"Total: {totalPrefabs} prefabs");
//        EditorGUI.indentLevel--;
//    }

//    void SortPrefabsByLayer()
//    {
//        if (manager.availablePrefabs == null) return;

//        List<PrefabData> prefabList = new List<PrefabData>(manager.availablePrefabs);
//        prefabList.Sort((a, b) => {
//            if (a == null && b == null) return 0;
//            if (a == null) return 1;
//            if (b == null) return -1;
//            return a.layer.CompareTo(b.layer);
//        });

//        manager.availablePrefabs = prefabList.ToArray();
//        EditorUtility.SetDirty(manager);
//    }

//    void ValidatePrefabs()
//    {
//        if (manager.availablePrefabs == null)
//        {
//            EditorUtility.DisplayDialog("Validation", "No hay prefabs configurados.", "OK");
//            return;
//        }

//        List<string> issues = new List<string>();

//        for (int i = 0; i < manager.availablePrefabs.Length; i++)
//        {
//            PrefabData prefab = manager.availablePrefabs[i];

//            if (prefab == null)
//            {
//                issues.Add($"Slot {i}: PrefabData es null");
//                continue;
//            }

//            if (prefab.prefab == null)
//            {
//                issues.Add($"Slot {i} ({prefab.name}): Prefab no asignado");
//            }
//            else
//            {
//                // Verificar que tenga collider
//                if (prefab.prefab.GetComponent<Collider>() == null)
//                {
//                    issues.Add($"Slot {i} ({prefab.name}): Prefab no tiene Collider");
//                }
//            }

//            if (string.IsNullOrEmpty(prefab.name))
//            {
//                issues.Add($"Slot {i}: Nombre vacío");
//            }

//            if (prefab.layer < 0 || prefab.layer > 2)
//            {
//                issues.Add($"Slot {i} ({prefab.name}): Layer inválida ({prefab.layer})");
//            }

//            if (prefab.previewImage == null)
//            {
//                issues.Add($"Slot {i} ({prefab.name}): Imagen de preview no asignada");
//            }
//        }

//        if (issues.Count == 0)
//        {
//            EditorUtility.DisplayDialog("Validation", "Todos los prefabs están correctamente configurados.", "OK");
//        }
//        else
//        {
//            string message = "Se encontraron los siguientes problemas:\n\n";
//            foreach (string issue in issues)
//            {
//                message += "• " + issue + "\n";
//            }

//            EditorUtility.DisplayDialog("Validation Issues", message, "OK");
//        }
//    }

//    void CreateUIManager()
//    {
//        GameObject uiManagerGO = new GameObject("WorldCreatorUI");
//        WorldCreatorUI uiManager = uiManagerGO.AddComponent<WorldCreatorUI>();

//        manager.uiManager = uiManager;
//        EditorUtility.SetDirty(manager);

//        EditorUtility.DisplayDialog("UI Manager Created",
//            "Se ha creado el UI Manager. Ahora necesitas configurar la interfaz de usuario manualmente.",
//            "OK");
//    }

//    void SetupScene()
//    {
//        bool setupCamera = EditorUtility.DisplayDialog("Setup Scene",
//            "¿Quieres configurar automáticamente la cámara para vista Top-Down?",
//            "Sí", "No");

//        if (setupCamera)
//        {
//            SetupCamera();
//        }

//        bool createCanvas = EditorUtility.DisplayDialog("Setup Scene",
//            "¿Quieres crear un Canvas básico para la UI?",
//            "Sí", "No");

//        if (createCanvas)
//        {
//            CreateBasicCanvas();
//        }

//        EditorUtility.DisplayDialog("Setup Complete",
//            "Configuración de escena completada. Revisa los objetos creados y ajusta según sea necesario.",
//            "OK");
//    }

//    void SetupCamera()
//    {
//        Camera mainCam = Camera.main;
//        if (mainCam == null)
//        {
//            mainCam = FindObjectOfType<Camera>();
//        }

//        if (mainCam == null)
//        {
//            GameObject camGO = new GameObject("Main Camera");
//            mainCam = camGO.AddComponent<Camera>();
//            camGO.tag = "MainCamera";
//        }

//        // Configurar para vista top-down
//        mainCam.transform.position = new Vector3(
//            manager.gridWidth * manager.gridSize * 0.5f,
//            20f,
//            manager.gridHeight * manager.gridSize * 0.5f
//        );
//        mainCam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
//        mainCam.orthographic = true;
//        mainCam.orthographicSize = Mathf.Max(manager.gridWidth, manager.gridHeight) * manager.gridSize * 0.6f;

//        EditorUtility.SetDirty(mainCam);
//    }

//    void CreateBasicCanvas()
//    {
//        // Verificar si ya existe un Canvas
//        Canvas existingCanvas = FindObjectOfType<Canvas>();
//        if (existingCanvas != null)
//        {
//            if (!EditorUtility.DisplayDialog("Canvas Found",
//                "Ya existe un Canvas en la escena. ¿Quieres crear uno nuevo de todas formas?",
//                "Sí", "No"))
//            {
//                return;
//            }
//        }

//        // Crear Canvas principal
//        GameObject canvasGO = new GameObject("WorldCreator Canvas");
//        Canvas canvas = canvasGO.AddComponent<Canvas>();
//        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
//        canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
//        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

//        // Verificar EventSystem
//        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
//        {
//            GameObject eventSystemGO = new GameObject("EventSystem");
//            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
//            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
//        }

//        CreateUIStructure(canvasGO);

//        EditorUtility.SetDirty(canvasGO);
//    }

//    void CreateUIStructure(GameObject canvas)
//    {
//        // Panel de herramientas (arriba)
//        GameObject toolPanel = CreateUIPanel("ToolPanel", canvas.transform);
//        SetRectTransform(toolPanel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
//                        new Vector2(0f, -50f), new Vector2(300f, 50f));

//        // Panel de prefabs (izquierda)
//        GameObject prefabPanel = CreateUIPanel("PrefabPanel", canvas.transform);
//        SetRectTransform(prefabPanel, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
//                        new Vector2(150f, 0f), new Vector2(300f, 400f));

//        // Panel de guardar/cargar (derecha)
//        GameObject saveLoadPanel = CreateUIPanel("SaveLoadPanel", canvas.transform);
//        SetRectTransform(saveLoadPanel, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
//                        new Vector2(-150f, 0f), new Vector2(300f, 200f));

//        // Panel de información (abajo)
//        GameObject infoPanel = CreateUIPanel("InfoPanel", canvas.transform);
//        SetRectTransform(infoPanel, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
//                        new Vector2(0f, 75f), new Vector2(400f, 150f));
//    }

//    GameObject CreateUIPanel(string name, Transform parent)
//    {
//        GameObject panel = new GameObject(name);
//        panel.transform.SetParent(parent);

//        UnityEngine.UI.Image image = panel.AddComponent<UnityEngine.UI.Image>();
//        image.color = new Color(0f, 0f, 0f, 0.5f);

//        return panel;
//    }

//    void SetRectTransform(GameObject obj, Vector2 anchorMin, Vector2 anchorMax,
//                         Vector2 anchoredPosition, Vector2 sizeDelta)
//    {
//        RectTransform rectTransform = obj.GetComponent<RectTransform>();
//        if (rectTransform == null)
//        {
//            rectTransform = obj.AddComponent<RectTransform>();
//        }

//        rectTransform.anchorMin = anchorMin;
//        rectTransform.anchorMax = anchorMax;
//        rectTransform.anchoredPosition = anchoredPosition;
//        rectTransform.sizeDelta = sizeDelta;
//    }
//}

//[CustomPropertyDrawer(typeof(PrefabData))]
//public class PrefabDataDrawer : PropertyDrawer
//{
//    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
//    {
//        EditorGUI.BeginProperty(position, label, property);

//        // Calcular alturas de líneas
//        float lineHeight = EditorGUIUtility.singleLineHeight;
//        float spacing = EditorGUIUtility.standardVerticalSpacing;

//        Rect foldoutRect = new Rect(position.x, position.y, position.width, lineHeight);

//        // Foldout principal
//        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded,
//            GetPrefabDisplayName(property), true);

//        if (property.isExpanded)
//        {
//            EditorGUI.indentLevel++;

//            float currentY = position.y + lineHeight + spacing;

//            // Name field
//            Rect nameRect = new Rect(position.x, currentY, position.width, lineHeight);
//            EditorGUI.PropertyField(nameRect, property.FindPropertyRelative("name"));
//            currentY += lineHeight + spacing;

//            // Layer field con colores
//            Rect layerRect = new Rect(position.x, currentY, position.width * 0.5f, lineHeight);
//            SerializedProperty layerProp = property.FindPropertyRelative("layer");

//            Color originalColor = GUI.color;
//            GUI.color = GetLayerColor(layerProp.intValue);
//            EditorGUI.PropertyField(layerRect, layerProp);
//            GUI.color = originalColor;

//            // Layer info
//            Rect layerInfoRect = new Rect(position.x + position.width * 0.5f + 5f, currentY,
//                                         position.width * 0.5f - 5f, lineHeight);
//            EditorGUI.LabelField(layerInfoRect, GetLayerName(layerProp.intValue),
//                               EditorStyles.miniLabel);
//            currentY += lineHeight + spacing;

//            // Prefab field
//            Rect prefabRect = new Rect(position.x, currentY, position.width, lineHeight);
//            EditorGUI.PropertyField(prefabRect, property.FindPropertyRelative("prefab"));
//            currentY += lineHeight + spacing;

//            // Preview image field
//            Rect previewRect = new Rect(position.x, currentY, position.width, lineHeight);
//            EditorGUI.PropertyField(previewRect, property.FindPropertyRelative("previewImage"));

//            EditorGUI.indentLevel--;
//        }

//        EditorGUI.EndProperty();
//    }

//    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
//    {
//        float baseHeight = EditorGUIUtility.singleLineHeight;

//        if (!property.isExpanded)
//            return baseHeight;

//        // 5 líneas: foldout, name, layer, prefab, preview
//        return baseHeight * 5 + EditorGUIUtility.standardVerticalSpacing * 4;
//    }

//    string GetPrefabDisplayName(SerializedProperty property)
//    {
//        SerializedProperty nameProp = property.FindPropertyRelative("name");
//        SerializedProperty layerProp = property.FindPropertyRelative("layer");

//        string displayName = string.IsNullOrEmpty(nameProp.stringValue) ? "Unnamed Prefab" : nameProp.stringValue;
//        return $"{displayName} (Layer {layerProp.intValue})";
//    }

//    Color GetLayerColor(int layer)
//    {
//        switch (layer)
//        {
//            case 0: return Color.black;
//            case 1: return Color.green;
//            case 2: return Color.cyan;
//            default: return Color.white;
//        }
//    }

//    string GetLayerName(int layer)
//    {
//        switch (layer)
//        {
//            case 0: return "Suelo";
//            case 1: return "Objetos";
//            case 2: return "Decoración";
//            default: return "Inválida";
//        }
//    }
//}
//#endif