using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.EventSystems;

[System.Serializable]
public class PrefabData
{
    public string name;
    public GameObject prefab;
    public Sprite previewImage;
    public int layer; // 0, 1, 2 para las 3 capas
}

[System.Serializable]
public class WorldSaveData
{
    public List<ObjectData> objects = new List<ObjectData>();
}

[System.Serializable]
public class ObjectData
{
    public string prefabName;
    public Vector3 position;
    public float rotationY;
    public int layer;
}

public enum ToolMode
{
    Paneo,
    Build,
    Select,
    Rotate,
    Move
}

public class WorldCreatorManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public float gridSize = 1f;
    public int gridWidth = 50;
    public int gridHeight = 50;
    public Material gridMaterial;

    [Header("Prefabs")]
    public PrefabData[] availablePrefabs;

    [Header("UI References")]
    public WorldCreatorUI uiManager;

    // Grid data structure for each layer
    public GameObject[,,] gridObjects; // [x, z, layer]
    private Camera mainCamera;
    public ToolMode currentTool = ToolMode.Paneo;
    public int selectedPrefabIndex = -1;
    private GameObject selectedObject = null;
    private LineRenderer gridRenderer;

    // Events
    public static event Action<ToolMode> OnToolChanged;
    public static event Action<int> OnPrefabSelected;

    public static WorldCreatorManager singleton;

    private void Awake()
    {
        singleton = this;
    }

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
            mainCamera = FindObjectOfType<Camera>();

        InitializeGrid();
        CreateGridVisual();

        if (uiManager != null)
            uiManager.Initialize(this);
    }

    void Update()
    {
        HandleInput();
    }

    void InitializeGrid()
    {
        gridObjects = new GameObject[gridWidth, gridHeight, 3]; // 3 layers
    }

    void CreateGridVisual()
    {
        //GameObject gridObject = new GameObject("Grid Visual");
        //gridRenderer = gridObject.AddComponent<LineRenderer>();
        //gridRenderer.material = gridMaterial;
        ////gridRenderer.color = Color.white;
        //gridRenderer.startWidth = 0.1f;
        //gridRenderer.endWidth = 0.1f;
        //gridRenderer.useWorldSpace = true;

        //List<Vector3> points = new List<Vector3>();

        //// Líneas verticales
        //for (int x = 0; x <= gridWidth; x++)
        //{
        //    points.Add(new Vector3(x * gridSize, 0, 0));
        //    points.Add(new Vector3(x * gridSize, 0, gridHeight * gridSize));
        //}

        //// Líneas horizontales
        //for (int z = 0; z <= gridHeight; z++)
        //{
        //    points.Add(new Vector3(0, 0, z * gridSize));
        //    points.Add(new Vector3(gridWidth * gridSize, 0, z * gridSize));
        //}

        //gridRenderer.positionCount = points.Count;
        //gridRenderer.SetPositions(points.ToArray());
    }

    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            HandleMouseClick();
        }

        // Shortcuts
        if (Input.GetKeyDown(KeyCode.B))
            SetTool(ToolMode.Build);
        else if (Input.GetKeyDown(KeyCode.S))
            SetTool(ToolMode.Select);
        else if (Input.GetKeyDown(KeyCode.R))
            SetTool(ToolMode.Rotate);
    }

    void HandleMouseClick()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        switch (currentTool)
        {
            case ToolMode.Build:
                HandleBuildMode(ray);
                break;
            case ToolMode.Select:
                HandleSelectMode(ray);
                break;
            case ToolMode.Rotate:
                HandleRotateMode(ray);
                break;
        }
    }

    void HandleBuildMode(Ray ray)
    {
        Vector3 worldPos = GetGridPosition(ray);
        if (worldPos != Vector3.zero)
        {
            int x = Mathf.RoundToInt(worldPos.x / gridSize);
            int z = Mathf.RoundToInt(worldPos.z / gridSize);

            if (IsValidGridPosition(x, z))
            {
                PlacePrefab(x, z, selectedPrefabIndex);
            }
        }
    }

    void HandleSelectMode(Ray ray)
    {
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject hitObject = hit.collider.gameObject;
            if (IsPlacedObject(hitObject))
            {
                SelectObject(hitObject);
            }
        }
    }

    void HandleRotateMode(Ray ray)
    {
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject hitObject = hit.collider.gameObject;
            if (IsPlacedObject(hitObject))
            {
                RotateObject(hitObject);
            }
        }
    }

    Vector3 GetGridPosition(Ray ray)
    {
        Plane gridPlane = new Plane(Vector3.up, Vector3.zero);
        if (gridPlane.Raycast(ray, out float distance))
        {
            Vector3 hitPoint = ray.GetPoint(distance);
            return new Vector3(
                Mathf.Round(hitPoint.x / gridSize) * gridSize,
                0,
                Mathf.Round(hitPoint.z / gridSize) * gridSize
            );
        }
        return Vector3.zero;
    }

    public bool IsValidGridPosition(int x, int z)
    {
        return x >= 0 && x < gridWidth && z >= 0 && z < gridHeight;
    }

    public void PlacePrefab(int x, int z, int prefabIndex)
    {
        if (UIBlocker.IsPointerOverUI() || Input.GetKey(KeyCode.Space))
            return;

        if (!IsValidGridPosition(x, z) || prefabIndex >= availablePrefabs.Length)
            return;

        PrefabData prefabData = availablePrefabs[prefabIndex];
        int layer = prefabData.layer;

        // Verificar si ya hay algo en esa posición y capa
        if (gridObjects[x, z, layer] != null)
            return;

        // Verificar colisiones con objetos existentes
        Vector3 worldPos = new Vector3(x * gridSize, 0, z * gridSize);
        if (HasCollisionAtPosition(worldPos, layer))
            return;

        GameObject newObject = Instantiate(prefabData.prefab, worldPos, Quaternion.identity);
        newObject.name = prefabData.name + "_" + x + "_" + z;

        // Añadir componente para identificar objetos colocados
        PlacedObject placedObj = newObject.AddComponent<PlacedObject>();
        placedObj.gridX = x;
        placedObj.gridZ = z;
        placedObj.layer = layer;
        placedObj.prefabName = prefabData.name;

        gridObjects[x, z, layer] = newObject;
    }

    public bool HasCollisionAtPosition(Vector3 position, int layer)
    {
        Collider[] colliders = Physics.OverlapSphere(position, gridSize * 0.4f);
        foreach (Collider col in colliders)
        {
            PlacedObject placedObj = col.GetComponent<PlacedObject>();
            if (placedObj != null && placedObj.layer == layer)
                return true;
        }
        return false;
    }

    public void RemoveObject(int x, int z, int layer)
    {
        if (IsValidGridPosition(x, z) && gridObjects[x, z, layer] != null)
        {
            if (selectedObject == gridObjects[x, z, layer])
                selectedObject = null;

            DestroyImmediate(gridObjects[x, z, layer]);
            gridObjects[x, z, layer] = null;
        }
    }

    void SelectObject(GameObject obj)
    {
        if (selectedObject != null)
        {
            // Remover highlight del objeto anterior
            RemoveHighlight(selectedObject);
        }

        selectedObject = obj;
        AddHighlight(selectedObject);
    }

    void AddHighlight(GameObject obj)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Agregar un outline o cambiar el material temporalmente
            renderer.material.color = Color.yellow;
        }
    }

    void RemoveHighlight(GameObject obj)
    {
        if (obj != null)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.white;
            }
        }
    }

    void RotateObject(GameObject obj)
    {
        
        obj.transform.Rotate(0,
            Input.GetMouseButton(1)?-30f:30f,
            0);
    }


    bool IsPlacedObject(GameObject obj)
    {
        return obj.GetComponent<PlacedObject>() != null;
    }

    public void SetTool(ToolMode tool)
    {
        currentTool = tool;
        OnToolChanged?.Invoke(tool);

        if (tool != ToolMode.Select && selectedObject != null)
        {
            RemoveHighlight(selectedObject);
            selectedObject = null;
        }
    }

    public void SetSelectedPrefab(int index)
    {
        if (index >= 0 && index < availablePrefabs.Length)
        {
            selectedPrefabIndex = index;
            OnPrefabSelected?.Invoke(index);
        }
    }

    public void SaveWorld(string fileName)
    {
        WorldSaveData saveData = new WorldSaveData();

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                for (int layer = 0; layer < 3; layer++)
                {
                    GameObject obj = gridObjects[x, z, layer];
                    if (obj != null)
                    {
                        PlacedObject placedObj = obj.GetComponent<PlacedObject>();
                        if (placedObj != null)
                        {
                            ObjectData objData = new ObjectData
                            {
                                prefabName = placedObj.prefabName,
                                position = obj.transform.position,
                                rotationY = obj.transform.eulerAngles.y,
                                layer = layer
                            };
                            saveData.objects.Add(objData);
                        }
                    }
                }
            }
        }

        string json = JsonUtility.ToJson(saveData, true);
        System.IO.File.WriteAllText(Application.persistentDataPath + "/" + fileName + ".json", json);
        Debug.Log("Mundo guardado: " + fileName);
    }

    public void LoadWorld(string fileName)
    {
        string filePath = Application.persistentDataPath + "/" + fileName + ".json";
        if (System.IO.File.Exists(filePath))
        {
            ClearWorld();

            string json = System.IO.File.ReadAllText(filePath);
            WorldSaveData saveData = JsonUtility.FromJson<WorldSaveData>(json);

            foreach (ObjectData objData in saveData.objects)
            {
                int prefabIndex = GetPrefabIndexByName(objData.prefabName);
                if (prefabIndex >= 0)
                {
                    int x = Mathf.RoundToInt(objData.position.x / gridSize);
                    int z = Mathf.RoundToInt(objData.position.z / gridSize);

                    PlacePrefab(x, z, prefabIndex);

                    if (gridObjects[x, z, objData.layer] != null)
                    {
                        Vector3 euler = gridObjects[x, z, objData.layer].transform.eulerAngles;
                        euler.y = objData.rotationY;
                        gridObjects[x, z, objData.layer].transform.eulerAngles = euler;
                    }
                }
            }

            Debug.Log("Mundo cargado: " + fileName);
        }
        else
        {
            Debug.LogError("Archivo no encontrado: " + fileName);
        }
    }

    int GetPrefabIndexByName(string prefabName)
    {
        for (int i = 0; i < availablePrefabs.Length; i++)
        {
            if (availablePrefabs[i].name == prefabName)
                return i;
        }
        return -1;
    }

    public void ClearWorld()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                for (int layer = 0; layer < 3; layer++)
                {
                    if (gridObjects[x, z, layer] != null)
                    {
                        DestroyImmediate(gridObjects[x, z, layer]);
                        gridObjects[x, z, layer] = null;
                    }
                }
            }
        }

        selectedObject = null;
    }
}


public static class UIBlocker
{
    public static bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        // Mouse (Editor/PC/Web)
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
        return EventSystem.current.IsPointerOverGameObject();
#else
        // Touch (Android/iOS)
        for (int i = 0; i < Input.touchCount; i++)
        {
            if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId))
                return true;
        }
        return false;
#endif
    }
}
