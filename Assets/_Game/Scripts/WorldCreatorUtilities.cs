using UnityEngine;
using System.Collections.Generic;
using System.IO;

public static class WorldCreatorUtilities
{
    /// <summary>
    /// Convierte coordenadas del mundo a coordenadas del grid
    /// </summary>
    public static Vector2Int WorldToGrid(Vector3 worldPosition, float gridSize)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPosition.x / gridSize),
            Mathf.RoundToInt(worldPosition.z / gridSize)
        );
    }

    /// <summary>
    /// Convierte coordenadas del grid a coordenadas del mundo
    /// </summary>
    public static Vector3 GridToWorld(Vector2Int gridPosition, float gridSize, float yOffset = 0f)
    {
        return new Vector3(
            gridPosition.x * gridSize,
            yOffset,
            gridPosition.y * gridSize
        );
    }

    /// <summary>
    /// Verifica si una posición del grid está dentro de los límites
    /// </summary>
    public static bool IsValidGridPosition(Vector2Int gridPos, int gridWidth, int gridHeight)
    {
        return gridPos.x >= 0 && gridPos.x < gridWidth &&
               gridPos.y >= 0 && gridPos.y < gridHeight;
    }

    /// <summary>
    /// Obtiene todos los archivos de mundo guardados
    /// </summary>
    public static List<string> GetSavedWorldFiles()
    {
        List<string> worldFiles = new List<string>();
        string savePath = Application.persistentDataPath;

        if (Directory.Exists(savePath))
        {
            string[] files = Directory.GetFiles(savePath, "*.json");
            foreach (string file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                worldFiles.Add(fileName);
            }
        }

        return worldFiles;
    }

    /// <summary>
    /// Crea un thumbnail de un mundo guardado
    /// </summary>
    public static void CreateWorldThumbnail(string worldName, Camera camera, int width = 256, int height = 256)
    {
        RenderTexture renderTexture = new RenderTexture(width, height, 24);
        camera.targetTexture = renderTexture;
        camera.Render();

        RenderTexture.active = renderTexture;
        Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenshot.Apply();

        byte[] data = screenshot.EncodeToPNG();
        string thumbnailPath = Path.Combine(Application.persistentDataPath, worldName + "_thumbnail.png");
        File.WriteAllBytes(thumbnailPath, data);

        camera.targetTexture = null;
        RenderTexture.active = null;
        Object.DestroyImmediate(renderTexture);
        Object.DestroyImmediate(screenshot);
    }

    /// <summary>
    /// Carga un thumbnail de un mundo
    /// </summary>
    public static Texture2D LoadWorldThumbnail(string worldName)
    {
        string thumbnailPath = Path.Combine(Application.persistentDataPath, worldName + "_thumbnail.png");
        if (File.Exists(thumbnailPath))
        {
            byte[] data = File.ReadAllBytes(thumbnailPath);
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(data);
            return texture;
        }
        return null;
    }

    /// <summary>
    /// Calcula estadísticas de un mundo
    /// </summary>
    public static WorldStats CalculateWorldStats(GameObject[,,] gridObjects, int gridWidth, int gridHeight)
    {
        WorldStats stats = new WorldStats();

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                for (int layer = 0; layer < 3; layer++)
                {
                    if (gridObjects[x, z, layer] != null)
                    {
                        stats.totalObjects++;
                        stats.objectsByLayer[layer]++;

                        PlacedObject placedObj = gridObjects[x, z, layer].GetComponent<PlacedObject>();
                        if (placedObj != null)
                        {
                            if (!stats.prefabCounts.ContainsKey(placedObj.prefabName))
                            {
                                stats.prefabCounts[placedObj.prefabName] = 0;
                            }
                            stats.prefabCounts[placedObj.prefabName]++;
                        }
                    }
                }
            }
        }

        stats.occupiedCells = 0;
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                bool hasObject = false;
                for (int layer = 0; layer < 3; layer++)
                {
                    if (gridObjects[x, z, layer] != null)
                    {
                        hasObject = true;
                        break;
                    }
                }
                if (hasObject) stats.occupiedCells++;
            }
        }

        stats.totalCells = gridWidth * gridHeight;
        stats.occupancyPercentage = (float)stats.occupiedCells / stats.totalCells * 100f;

        return stats;
    }

    /// <summary>
    /// Exporta el mundo a un formato de texto legible
    /// </summary>
    public static void ExportWorldToText(WorldSaveData saveData, string fileName)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("=== WORLD EXPORT ===");
        sb.AppendLine($"Generated: {System.DateTime.Now}");
        sb.AppendLine($"Total Objects: {saveData.objects.Count}");
        sb.AppendLine();

        // Agrupar por capa
        Dictionary<int, List<ObjectData>> objectsByLayer = new Dictionary<int, List<ObjectData>>();

        foreach (ObjectData obj in saveData.objects)
        {
            if (!objectsByLayer.ContainsKey(obj.layer))
            {
                objectsByLayer[obj.layer] = new List<ObjectData>();
            }
            objectsByLayer[obj.layer].Add(obj);
        }

        // Escribir cada capa
        string[] layerNames = { "FLOOR", "OBJECTS", "DECORATION" };
        for (int layer = 0; layer < 3; layer++)
        {
            if (objectsByLayer.ContainsKey(layer))
            {
                sb.AppendLine($"=== LAYER {layer} - {layerNames[layer]} ===");
                sb.AppendLine($"Objects: {objectsByLayer[layer].Count}");
                sb.AppendLine();

                foreach (ObjectData obj in objectsByLayer[layer])
                {
                    sb.AppendLine($"Prefab: {obj.prefabName}");
                    sb.AppendLine($"Position: ({obj.position.x:F1}, {obj.position.y:F1}, {obj.position.z:F1})");
                    sb.AppendLine($"Rotation: {obj.rotationY:F1}°");
                    sb.AppendLine();
                }
            }
        }

        string exportPath = Path.Combine(Application.persistentDataPath, fileName + "_export.txt");
        File.WriteAllText(exportPath, sb.ToString());
        Debug.Log($"World exported to: {exportPath}");
    }

    /// <summary>
    /// Valida la integridad de un archivo de mundo
    /// </summary>
    public static bool ValidateWorldFile(string fileName, out string errorMessage)
    {
        errorMessage = "";
        string filePath = Path.Combine(Application.persistentDataPath, fileName + ".json");

        if (!File.Exists(filePath))
        {
            errorMessage = "El archivo no existe.";
            return false;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            WorldSaveData saveData = JsonUtility.FromJson<WorldSaveData>(json);

            if (saveData == null)
            {
                errorMessage = "No se pudo deserializar el archivo JSON.";
                return false;
            }

            if (saveData.objects == null)
            {
                errorMessage = "La lista de objetos es null.";
                return false;
            }

            // Validar cada objeto
            for (int i = 0; i < saveData.objects.Count; i++)
            {
                ObjectData obj = saveData.objects[i];

                if (string.IsNullOrEmpty(obj.prefabName))
                {
                    errorMessage = $"Objeto {i}: Nombre de prefab vacío.";
                    return false;
                }

                if (obj.layer < 0 || obj.layer > 2)
                {
                    errorMessage = $"Objeto {i}: Layer inválida ({obj.layer}).";
                    return false;
                }
            }

            return true;
        }
        catch (System.Exception ex)
        {
            errorMessage = $"Error al validar el archivo: {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// Crea un backup de un mundo
    /// </summary>
    public static void BackupWorld(string worldName)
    {
        string originalPath = Path.Combine(Application.persistentDataPath, worldName + ".json");
        if (File.Exists(originalPath))
        {
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string backupPath = Path.Combine(Application.persistentDataPath, $"{worldName}_backup_{timestamp}.json");
            File.Copy(originalPath, backupPath);
            Debug.Log($"Backup created: {backupPath}");
        }
    }

    /// <summary>
    /// Optimiza un mundo removiendo objetos duplicados en la misma posición
    /// </summary>
    public static int OptimizeWorld(WorldSaveData saveData)
    {
        Dictionary<string, ObjectData> positionMap = new Dictionary<string, ObjectData>();
        List<ObjectData> optimizedObjects = new List<ObjectData>();
        int removedCount = 0;

        foreach (ObjectData obj in saveData.objects)
        {
            string positionKey = $"{obj.position.x}_{obj.position.z}_{obj.layer}";

            if (positionMap.ContainsKey(positionKey))
            {
                // Objeto duplicado encontrado
                removedCount++;
                Debug.LogWarning($"Removed duplicate object: {obj.prefabName} at {obj.position}");
            }
            else
            {
                positionMap[positionKey] = obj;
                optimizedObjects.Add(obj);
            }
        }

        saveData.objects = optimizedObjects;
        return removedCount;
    }
}

[System.Serializable]
public class WorldStats
{
    public int totalObjects = 0;
    public int totalCells = 0;
    public int occupiedCells = 0;
    public float occupancyPercentage = 0f;
    public int[] objectsByLayer = new int[3];
    public Dictionary<string, int> prefabCounts = new Dictionary<string, int>();

    public override string ToString()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("=== WORLD STATISTICS ===");
        sb.AppendLine($"Total Objects: {totalObjects}");
        sb.AppendLine($"Occupied Cells: {occupiedCells}/{totalCells} ({occupancyPercentage:F1}%)");
        sb.AppendLine($"Layer 0 (Floor): {objectsByLayer[0]} objects");
        sb.AppendLine($"Layer 1 (Objects): {objectsByLayer[1]} objects");
        sb.AppendLine($"Layer 2 (Decoration): {objectsByLayer[2]} objects");
        sb.AppendLine();
        sb.AppendLine("Prefab Usage:");

        foreach (var kvp in prefabCounts)
        {
            sb.AppendLine($"  {kvp.Key}: {kvp.Value}");
        }

        return sb.ToString();
    }
}

public class WorldCreatorSettings : ScriptableObject
{
    [Header("Default Grid Settings")]
    public float defaultGridSize = 1f;
    public int defaultGridWidth = 50;
    public int defaultGridHeight = 50;

    [Header("UI Colors")]
    public Color buildModeColor = Color.green;
    public Color selectModeColor = Color.blue;
    public Color rotateModeColor = Color.yellow;

    [Header("Layer Colors")]
    public Color[] layerColors = { Color.black, Color.green, Color.cyan };

    [Header("Performance")]
    public bool enableObjectPooling = true;
    public int maxObjectsPerFrame = 10;
    public bool enableLOD = false;

    [Header("Auto-Save")]
    public bool enableAutoSave = true;
    public float autoSaveInterval = 300f; // 5 minutos

    [Header("Validation")]
    public bool validatePrefabsOnStart = true;
    public bool showValidationWarnings = true;
}

public class AutoSaveManager : MonoBehaviour
{
    private WorldCreatorManager worldManager;
    private WorldCreatorSettings settings;
    private float lastSaveTime;

    void Start()
    {
        worldManager = GetComponent<WorldCreatorManager>();
        // Aquí cargarías los settings desde un ScriptableObject
        // settings = Resources.Load<WorldCreatorSettings>("WorldCreatorSettings");

        if (settings == null)
        {
            // Crear settings por defecto
            settings = ScriptableObject.CreateInstance<WorldCreatorSettings>();
        }
    }

    void Update()
    {
        if (settings.enableAutoSave && worldManager != null)
        {
            if (Time.time - lastSaveTime >= settings.autoSaveInterval)
            {
                AutoSave();
                lastSaveTime = Time.time;
            }
        }
    }

    void AutoSave()
    {
        string autoSaveFileName = "AutoSave_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        worldManager.SaveWorld(autoSaveFileName);
        Debug.Log($"Auto-save completed: {autoSaveFileName}");

        // Mantener solo los últimos 5 auto-saves
        CleanupAutoSaves();
    }

    void CleanupAutoSaves()
    {
        List<string> autoSaves = new List<string>();
        string savePath = Application.persistentDataPath;

        if (Directory.Exists(savePath))
        {
            string[] files = Directory.GetFiles(savePath, "AutoSave_*.json");
            foreach (string file in files)
            {
                autoSaves.Add(file);
            }

            // Ordenar por fecha de modificación (más recientes primero)
            autoSaves.Sort((a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));

            // Eliminar archivos antiguos (mantener solo los 5 más recientes)
            for (int i = 5; i < autoSaves.Count; i++)
            {
                File.Delete(autoSaves[i]);
                Debug.Log($"Deleted old auto-save: {Path.GetFileName(autoSaves[i])}");
            }
        }
    }
}