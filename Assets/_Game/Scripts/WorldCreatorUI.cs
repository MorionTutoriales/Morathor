using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class WorldCreatorUI : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject toolPanel;
    public GameObject prefabPanel;
    public GameObject saveLoadPanel;

    [Header("Tool Buttons")]
    public Button buildButton;
    public Button selectButton;
    public Button rotateButton;

    [Header("Prefab Selection")]
    public Transform prefabButtonParent;
    public GameObject prefabButtonPrefab;
    public ScrollRect prefabScrollRect;

    [Header("Layer Selection")]
    public Toggle[] layerToggles = new Toggle[3];
    public TextMeshProUGUI currentLayerText;

    [Header("Save/Load")]
    public TMP_InputField fileNameInput;
    public Button saveButton;
    public Button loadButton;
    public Button clearButton;

    [Header("Info Panel")]
    public TextMeshProUGUI infoText;
    public TextMeshProUGUI toolModeText;

    private WorldCreatorManager manager;
    private List<Button> prefabButtons = new List<Button>();
    private int currentLayer = 0;
    public Color originalButtonColor;
    public Color selectedButtonColor = Color.green;

    public void Initialize(WorldCreatorManager worldManager)
    {
        manager = worldManager;
        SetupUI();
        CreatePrefabButtons();
        UpdateToolDisplay(ToolMode.Build);

        // Subscribe to events
        WorldCreatorManager.OnToolChanged += UpdateToolDisplay;
        WorldCreatorManager.OnPrefabSelected += UpdateSelectedPrefab;
    }

    void OnDestroy()
    {
        WorldCreatorManager.OnToolChanged -= UpdateToolDisplay;
        WorldCreatorManager.OnPrefabSelected -= UpdateSelectedPrefab;
    }

    void SetupUI()
    {
        // Setup tool buttons
        if (buildButton != null)
        {
            buildButton.onClick.AddListener(() => manager.SetTool(ToolMode.Build));
            originalButtonColor = buildButton.GetComponent<Image>().color;
        }

        if (selectButton != null)
            selectButton.onClick.AddListener(() => manager.SetTool(ToolMode.Select));

        if (rotateButton != null)
            rotateButton.onClick.AddListener(() => manager.SetTool(ToolMode.Rotate));

        // Setup layer toggles
        for (int i = 0; i < layerToggles.Length; i++)
        {
            int layerIndex = i;
            if (layerToggles[i] != null)
            {
                layerToggles[i].onValueChanged.AddListener((bool value) => {
                    if (value) SetCurrentLayer(layerIndex);
                });
            }
        }

        // Set default layer
        if (layerToggles[0] != null)
            layerToggles[0].isOn = true;

        // Setup save/load buttons
        if (saveButton != null)
            saveButton.onClick.AddListener(SaveWorld);

        if (loadButton != null)
            loadButton.onClick.AddListener(LoadWorld);

        if (clearButton != null)
            clearButton.onClick.AddListener(ClearWorld);

        // Set default filename
        if (fileNameInput != null)
            fileNameInput.text = "MyWorld";

        UpdateLayerDisplay();
        UpdateInfoText();
    }

    void CreatePrefabButtons()
    {
        if (manager.availablePrefabs == null || prefabButtonParent == null || prefabButtonPrefab == null)
            return;

        // Clear existing buttons
        foreach (Transform child in prefabButtonParent)
        {
            DestroyImmediate(child.gameObject);
        }
        prefabButtons.Clear();

        // Create buttons for current layer
        for (int i = 0; i < manager.availablePrefabs.Length; i++)
        {
            PrefabData prefabData = manager.availablePrefabs[i];

            // Only show prefabs for current layer
            if (prefabData.layer != currentLayer)
                continue;

            GameObject buttonObj = Instantiate(prefabButtonPrefab, prefabButtonParent);
            Button button = buttonObj.GetComponent<Button>();

            // Setup button image
            Image buttonImage = buttonObj.GetComponent<Image>();
            if (prefabData.previewImage != null)
            {
                Image previewImage = buttonObj.transform.GetChild(0).GetComponent<Image>();
                if (previewImage != null)
                    previewImage.sprite = prefabData.previewImage;
            }

            // Setup button text
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
                buttonText.text = prefabData.name;

            // Setup button click
            int prefabIndex = i;
            button.onClick.AddListener(() => SelectPrefab(prefabIndex));

            prefabButtons.Add(button);
        }

        // Select first prefab by default
        if (prefabButtons.Count > 0)
        {
            SelectPrefab(GetFirstPrefabIndexForLayer(currentLayer));
        }
    }

    int GetFirstPrefabIndexForLayer(int layer)
    {
        for (int i = 0; i < manager.availablePrefabs.Length; i++)
        {
            if (manager.availablePrefabs[i].layer == layer)
                return i;
        }
        return 0;
    }

    void SelectPrefab(int prefabIndex)
    {
        manager.SetSelectedPrefab(prefabIndex);
    }

    void UpdateSelectedPrefab(int prefabIndex)
    {
        // Update button colors
        for (int i = 0; i < prefabButtons.Count; i++)
        {
            Image buttonImage = prefabButtons[i].GetComponent<Image>();
            buttonImage.color = originalButtonColor;
        }

        // Highlight selected button
        PrefabData selectedPrefab = manager.availablePrefabs[prefabIndex];
        for (int i = 0; i < prefabButtons.Count; i++)
        {
            TextMeshProUGUI buttonText = prefabButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null && buttonText.text == selectedPrefab.name)
            {
                Image buttonImage = prefabButtons[i].GetComponent<Image>();
                buttonImage.color = selectedButtonColor;
                break;
            }
        }

        UpdateInfoText();
    }

    void SetCurrentLayer(int layer)
    {
        currentLayer = layer;
        UpdateLayerDisplay();
        CreatePrefabButtons(); // Recreate buttons for new layer
        UpdateInfoText();
    }

    void UpdateLayerDisplay()
    {
        if (currentLayerText != null)
            currentLayerText.text = "Capa: " + (currentLayer + 1);
    }

    void UpdateToolDisplay(ToolMode toolMode)
    {
        // Update button colors
        if (buildButton != null)
            buildButton.GetComponent<Image>().color = originalButtonColor;
        if (selectButton != null)
            selectButton.GetComponent<Image>().color = originalButtonColor;
        if (rotateButton != null)
            rotateButton.GetComponent<Image>().color = originalButtonColor;

        // Highlight active tool
        Button activeButton = null;
        string toolName = "";

        switch (toolMode)
        {
            case ToolMode.Build:
                activeButton = buildButton;
                toolName = "Construcción";
                break;
            case ToolMode.Select:
                activeButton = selectButton;
                toolName = "Selección";
                break;
            case ToolMode.Rotate:
                activeButton = rotateButton;
                toolName = "Rotación";
                break;
        }

        if (activeButton != null)
            activeButton.GetComponent<Image>().color = selectedButtonColor;

        if (toolModeText != null)
            toolModeText.text = "Herramienta: " + toolName;
    }

    void UpdateInfoText()
    {
        if (infoText == null) return;

        string info = "Controles:\n";
        info += "B - Herramienta Construcción\n";
        info += "S - Herramienta Selección\n";
        info += "R - Herramienta Rotación\n";
        info += "Clic - Acción según herramienta\n\n";
        info += "Capa actual: " + (currentLayer + 1);

        infoText.text = info;
    }

    public void SaveWorld()
    {
        if (manager != null && fileNameInput != null)
        {
            string fileName = fileNameInput.text;
            if (!string.IsNullOrEmpty(fileName))
            {
                manager.SaveWorld(fileName);
                ShowMessage("Mundo guardado: " + fileName);
            }
        }
    }

    public void LoadWorld()
    {
        if (manager != null && fileNameInput != null)
        {
            string fileName = fileNameInput.text;
            if (!string.IsNullOrEmpty(fileName))
            {
                manager.LoadWorld(fileName);
                ShowMessage("Mundo cargado: " + fileName);
            }
        }
    }

    public void ClearWorld()
    {
        if (manager != null)
        {
            manager.ClearWorld();
            ShowMessage("Mundo limpiado");
        }
    }

    void ShowMessage(string message)
    {
        Debug.Log(message);
        // Aquí podrías mostrar un popup temporal o actualizar un texto de estado
    }

    void Update()
    {
        // Handle keyboard shortcuts
        if (Input.GetKeyDown(KeyCode.B))
            manager.SetTool(ToolMode.Build);
        else if (Input.GetKeyDown(KeyCode.S))
            manager.SetTool(ToolMode.Select);
        else if (Input.GetKeyDown(KeyCode.R))
            manager.SetTool(ToolMode.Rotate);
        else if (Input.GetKeyDown(KeyCode.M))
            manager.SetTool(ToolMode.Move);

        // Layer shortcuts
        if (Input.GetKeyDown(KeyCode.Alpha1) && layerToggles[0] != null)
            layerToggles[0].isOn = true;
        else if (Input.GetKeyDown(KeyCode.Alpha2) && layerToggles[1] != null)
            layerToggles[1].isOn = true;
        else if (Input.GetKeyDown(KeyCode.Alpha3) && layerToggles[2] != null)
            layerToggles[2].isOn = true;
    }
}