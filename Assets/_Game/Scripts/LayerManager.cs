using UnityEngine;
using System.Collections.Generic;

public class LayerManager : MonoBehaviour
{
    [Header("Layer Settings")]
    public LayerVisibilitySettings[] layerSettings = new LayerVisibilitySettings[3];

    [Header("Height Offsets")]
    public float[] layerHeights = { 0f, 0.1f, 0.2f }; // Altura Y para cada capa

    private WorldCreatorManager worldManager;
    private int currentActiveLayer = 0;

    void Start()
    {
        worldManager = GetComponent<WorldCreatorManager>();

        // Configurar settings por defecto si no están asignados
        InitializeDefaultLayerSettings();

        // Aplicar visibilidad inicial
        UpdateLayerVisibility();
    }

    void InitializeDefaultLayerSettings()
    {
        if (layerSettings[0] == null)
        {
            layerSettings[0] = new LayerVisibilitySettings
            {
                layerName = "Suelo",
                layerColor = Color.black,
                isVisible = true,
                transparency = 1.0f
            };
        }

        if (layerSettings[1] == null)
        {
            layerSettings[1] = new LayerVisibilitySettings
            {
                layerName = "Objetos",
                layerColor = Color.green,
                isVisible = true,
                transparency = 1.0f
            };
        }

        if (layerSettings[2] == null)
        {
            layerSettings[2] = new LayerVisibilitySettings
            {
                layerName = "Decoración",
                layerColor = Color.cyan,
                isVisible = true,
                transparency = 1.0f
            };
        }
    }

    public void SetActiveLayer(int layerIndex)
    {
        if (layerIndex >= 0 && layerIndex < 3)
        {
            currentActiveLayer = layerIndex;
            UpdateLayerVisibility();

            // Notificar al UI manager si existe
            WorldCreatorUI uiManager = FindObjectOfType<WorldCreatorUI>();
            if (uiManager != null)
            {
                // El UI manager se actualizará automáticamente
            }
        }
    }

    public void ToggleLayerVisibility(int layerIndex)
    {
        if (layerIndex >= 0 && layerIndex < layerSettings.Length)
        {
            layerSettings[layerIndex].isVisible = !layerSettings[layerIndex].isVisible;
            UpdateLayerVisibility();
        }
    }

    public void SetLayerTransparency(int layerIndex, float transparency)
    {
        if (layerIndex >= 0 && layerIndex < layerSettings.Length)
        {
            layerSettings[layerIndex].transparency = Mathf.Clamp01(transparency);
            UpdateLayerVisibility();
        }
    }

    void UpdateLayerVisibility()
    {
        if (worldManager == null || worldManager.gridObjects == null) return;

        for (int x = 0; x < worldManager.gridWidth; x++)
        {
            for (int z = 0; z < worldManager.gridHeight; z++)
            {
                for (int layer = 0; layer < 3; layer++)
                {
                    GameObject obj = worldManager.gridObjects[x, z, layer];
                    if (obj != null)
                    {
                        UpdateObjectVisibility(obj, layer);
                    }
                }
            }
        }
    }

    void UpdateObjectVisibility(GameObject obj, int layer)
    {
        LayerVisibilitySettings settings = layerSettings[layer];

        // Control de visibilidad general
        obj.SetActive(settings.isVisible);

        if (!settings.isVisible) return;

        // Control de transparencia
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            foreach (Material mat in renderer.materials)
            {
                if (mat.HasProperty("_Color"))
                {
                    Color color = mat.color;

                    // Aplicar tinte de capa si no es la capa activa
                    if (layer != currentActiveLayer)
                    {
                        // Mezclar con color de capa para identificación visual
                        Color layerTint = settings.layerColor;
                        color = Color.Lerp(color, layerTint, 0.2f);
                    }

                    // Aplicar transparencia
                    color.a = settings.transparency;

                    // Reducir transparencia para capas no activas
                    if (layer != currentActiveLayer)
                    {
                        color.a *= 0.7f;
                    }

                    mat.color = color;

                    // Configurar material para transparencia
                    UpdateMaterialForTransparency(mat, color.a < 1.0f);
                }
            }
        }

        // Ajustar altura según la capa
        Vector3 pos = obj.transform.position;
        pos.y = layerHeights[layer];
        obj.transform.position = pos;
    }

    void UpdateMaterialForTransparency(Material mat, bool isTransparent)
    {
        if (isTransparent)
        {
            mat.SetFloat("_Mode", 2); // Fade mode
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }
        else
        {
            mat.SetFloat("_Mode", 0); // Opaque mode
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            mat.SetInt("_ZWrite", 1);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = -1;
        }
    }

    public void ShowOnlyLayer(int layerIndex)
    {
        for (int i = 0; i < layerSettings.Length; i++)
        {
            layerSettings[i].isVisible = (i == layerIndex);
        }
        UpdateLayerVisibility();
    }

    public void ShowAllLayers()
    {
        for (int i = 0; i < layerSettings.Length; i++)
        {
            layerSettings[i].isVisible = true;
            layerSettings[i].transparency = 1.0f;
        }
        UpdateLayerVisibility();
    }

    public LayerVisibilitySettings GetLayerSettings(int layerIndex)
    {
        if (layerIndex >= 0 && layerIndex < layerSettings.Length)
            return layerSettings[layerIndex];
        return null;
    }

    public int GetActiveLayer()
    {
        return currentActiveLayer;
    }

    // Método para ser llamado cuando se coloca un nuevo objeto
    public void OnObjectPlaced(GameObject obj, int layer)
    {
        UpdateObjectVisibility(obj, layer);
    }
}

[System.Serializable]
public class LayerVisibilitySettings
{
    public string layerName = "Layer";
    public Color layerColor = Color.white;
    public bool isVisible = true;
    [Range(0f, 1f)]
    public float transparency = 1f;
}