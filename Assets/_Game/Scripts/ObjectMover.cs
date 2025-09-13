using UnityEngine;

public class ObjectMover : MonoBehaviour
{
    private WorldCreatorManager manager;
    private GameObject draggedObject = null;
    private Vector3 dragOffset;
    private int originalX, originalZ, originalLayer;
    private bool isDragging = false;

    [Header("Visual Feedback")]
    public Material previewMaterial;
    public Color validPlacementColor = Color.green;
    public Color invalidPlacementColor = Color.red;

    private GameObject previewObject;
    private Camera mainCamera;

    void Start()
    {
        manager = GetComponent<WorldCreatorManager>();
        mainCamera = Camera.main;
        if (mainCamera == null)
            mainCamera = FindObjectOfType<Camera>();
    }

    void Update()
    {
        HandleDragAndDrop();
    }

    void HandleDragAndDrop()
    {
        // Solo funciona en modo selección
        if (manager == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            StartDrag();
        }
        else if (Input.GetMouseButton(0) && isDragging)
        {
            UpdateDrag();
        }
        else if (Input.GetMouseButtonUp(0) && isDragging)
        {
            EndDrag();
        }
    }

    void StartDrag()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            PlacedObject placedObj = hit.collider.GetComponent<PlacedObject>();
            if (placedObj != null)
            {
                draggedObject = hit.collider.gameObject;
                originalX = placedObj.gridX;
                originalZ = placedObj.gridZ;
                originalLayer = placedObj.layer;

                // Calcular offset para drag suave
                Vector3 worldPos = GetGridPosition(ray);
                dragOffset = draggedObject.transform.position - worldPos;

                CreatePreviewObject();
                SetObjectTransparency(draggedObject, 0.5f);

                isDragging = true;

                // Remover del grid temporalmente
                manager.RemoveObject(originalX, originalZ, originalLayer);
            }
        }
    }

    void UpdateDrag()
    {
        if (draggedObject == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Vector3 targetPos = GetGridPosition(ray);

        if (targetPos != Vector3.zero)
        {
            targetPos += dragOffset;

            // Actualizar posición del preview
            if (previewObject != null)
            {
                previewObject.transform.position = targetPos;

                // Verificar si la posición es válida
                int x = Mathf.RoundToInt(targetPos.x / manager.gridSize);
                int z = Mathf.RoundToInt(targetPos.z / manager.gridSize);

                bool canPlace = CanPlaceAt(x, z, originalLayer);
                UpdatePreviewColor(canPlace);
            }
        }
    }

    void EndDrag()
    {
        if (draggedObject == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Vector3 targetPos = GetGridPosition(ray);

        if (targetPos != Vector3.zero)
        {
            int newX = Mathf.RoundToInt(targetPos.x / manager.gridSize);
            int newZ = Mathf.RoundToInt(targetPos.z / manager.gridSize);

            if (CanPlaceAt(newX, newZ, originalLayer))
            {
                // Mover objeto a la nueva posición
                MoveObjectTo(draggedObject, newX, newZ, originalLayer);
            }
            else
            {
                // Regresar a posición original
                ReturnToOriginalPosition();
            }
        }
        else
        {
            // Regresar a posición original
            ReturnToOriginalPosition();
        }

        CleanupDrag();
    }

    void CreatePreviewObject()
    {
        if (draggedObject == null) return;

        // Crear una copia visual del objeto para preview
        previewObject = Instantiate(draggedObject);

        // Remover componentes innecesarios del preview
        PlacedObject placedComp = previewObject.GetComponent<PlacedObject>();
        if (placedComp != null)
            DestroyImmediate(placedComp);

        Collider collider = previewObject.GetComponent<Collider>();
        if (collider != null)
            collider.enabled = false;

        // Aplicar material de preview
        SetObjectTransparency(previewObject, 0.7f);
    }

    void UpdatePreviewColor(bool canPlace)
    {
        if (previewObject == null) return;

        Color targetColor = canPlace ? validPlacementColor : invalidPlacementColor;

        Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            foreach (Material mat in renderer.materials)
            {
                if (mat.HasProperty("_Color"))
                {
                    Color currentColor = mat.color;
                    currentColor.r = targetColor.r;
                    currentColor.g = targetColor.g;
                    currentColor.b = targetColor.b;
                    mat.color = currentColor;
                }
            }
        }
    }

    void SetObjectTransparency(GameObject obj, float alpha)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            foreach (Material mat in renderer.materials)
            {
                if (mat.HasProperty("_Color"))
                {
                    Color color = mat.color;
                    color.a = alpha;
                    mat.color = color;
                }

                // Cambiar rendering mode para transparencia
                if (alpha < 1.0f)
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
            }
        }
    }

    void RestoreObjectTransparency(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            foreach (Material mat in renderer.materials)
            {
                if (mat.HasProperty("_Color"))
                {
                    Color color = mat.color;
                    color.a = 1.0f;
                    mat.color = color;
                }

                // Restaurar rendering mode opaco
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
    }

    bool CanPlaceAt(int x, int z, int layer)
    {
        if (!manager.IsValidGridPosition(x, z))
            return false;

        // Verificar si es la posición original (siempre válida)
        if (x == originalX && z == originalZ && layer == originalLayer)
            return true;

        // Verificar colisiones
        Vector3 worldPos = new Vector3(x * manager.gridSize, 0, z * manager.gridSize);
        return !manager.HasCollisionAtPosition(worldPos, layer);
    }

    void MoveObjectTo(GameObject obj, int x, int z, int layer)
    {
        Vector3 newPos = new Vector3(x * manager.gridSize, 0, z * manager.gridSize);
        obj.transform.position = newPos;

        // Actualizar componente PlacedObject
        PlacedObject placedObj = obj.GetComponent<PlacedObject>();
        if (placedObj != null)
        {
            placedObj.gridX = x;
            placedObj.gridZ = z;
            placedObj.layer = layer;
        }

        // Actualizar grid del manager
        manager.gridObjects[x, z, layer] = obj;
    }

    void ReturnToOriginalPosition()
    {
        Vector3 originalPos = new Vector3(
            originalX * manager.gridSize,
            0,
            originalZ * manager.gridSize
        );

        draggedObject.transform.position = originalPos;

        // Restaurar en el grid
        manager.gridObjects[originalX, originalZ, originalLayer] = draggedObject;
    }

    void CleanupDrag()
    {
        if (draggedObject != null)
        {
            RestoreObjectTransparency(draggedObject);
        }

        if (previewObject != null)
        {
            DestroyImmediate(previewObject);
        }

        draggedObject = null;
        previewObject = null;
        isDragging = false;
    }

    Vector3 GetGridPosition(Ray ray)
    {
        Plane gridPlane = new Plane(Vector3.up, Vector3.zero);
        if (gridPlane.Raycast(ray, out float distance))
        {
            Vector3 hitPoint = ray.GetPoint(distance);
            return new Vector3(
                Mathf.Round(hitPoint.x / manager.gridSize) * manager.gridSize,
                0,
                Mathf.Round(hitPoint.z / manager.gridSize) * manager.gridSize
            );
        }
        return Vector3.zero;
    }

    // Método público para habilitar/deshabilitar el drag and drop
    public void SetDragEnabled(bool enabled)
    {
        this.enabled = enabled;
        if (!enabled && isDragging)
        {
            ReturnToOriginalPosition();
            CleanupDrag();
        }
    }
}