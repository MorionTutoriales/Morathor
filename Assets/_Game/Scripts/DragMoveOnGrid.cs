using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class DragMoveOnGrid : MonoBehaviour
{
    [Header("Feedback opcional")]
    public Color validColor = new Color(0.4f, 1f, 0.6f, 1f);
    public Color invalidColor = new Color(1f, 0.4f, 0.4f, 1f);
    public bool tintWhileDragging = true;

    Camera cam;
    PlacedObject placed;
    Renderer rend;
    Color originalColor;

    public bool dragging;
    Vector3 originalWorldPos;
    int originalX, originalZ;

    void Awake()
    {
        cam = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
        placed = GetComponent<PlacedObject>();
        rend = GetComponentInChildren<Renderer>();
        if (rend != null) originalColor = rend.material.color;
    }

    void OnMouseDown()
    {
        print(0);
        // Bloquear si UI / si no es modo Mover / sin datos
        if (UIBlocker.IsPointerOverUI()) return;
        if (WorldCreatorManager.singleton == null) return;
        if (WorldCreatorManager.singleton.currentTool != ToolMode.Move) return;
        
        if (placed == null) placed = GetComponent<PlacedObject>();
        if (placed == null) return;

        dragging = true;

        // Guardar estado inicial
        originalWorldPos = transform.position;
        originalX = placed.gridX;
        originalZ = placed.gridZ;
        print(5);
    }

    void OnMouseDrag()
    {
        if (!dragging) return;
        var mgr = WorldCreatorManager.singleton;
        if (mgr == null) return;

        // Ray al plano Y=0 (misma lógica que el manager)
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float dist))
            return;

        Vector3 hit = ray.GetPoint(dist);

        // Snap a la grilla
        int gx = Mathf.RoundToInt(hit.x / mgr.gridSize);
        int gz = Mathf.RoundToInt(hit.z / mgr.gridSize);
        Vector3 snapped = new Vector3(gx * mgr.gridSize, 0f, gz * mgr.gridSize);

        // Validación: dentro de la grilla
        bool valid = mgr.IsValidGridPosition(gx, gz);

        // Validación: celda libre en la misma capa (o es mi propia celda)
        if (valid)
        {
            GameObject occupant = mgr.gridObjects[gx, gz, placed.layer];
            if (occupant != null && occupant != this.gameObject)
                valid = false;
        }

        // Validación: colisión por capa (ignorando mis propios colliders)
        if (valid)
        {
            float radius = mgr.gridSize * 0.4f;
            Collider[] cols = Physics.OverlapSphere(snapped, radius);
            foreach (var col in cols)
            {
                // Ignorarme a mí mismo
                if (col.transform.root == this.transform) continue;

                var otherPlaced = col.GetComponent<PlacedObject>();
                if (otherPlaced != null && otherPlaced.layer == placed.layer)
                {
                    valid = false;
                    break;
                }
            }
        }

        // Mover visualmente durante el drag (aunque no sea válido mostramos dónde quedaría)
        transform.position = snapped;

        // Tint opcional para feedback
        if (tintWhileDragging && rend != null)
            rend.material.color = valid ? validColor : invalidColor;
    }

    void OnMouseUp()
    {
        if (!dragging) return;
        dragging = false;

        var mgr = WorldCreatorManager.singleton;
        if (mgr == null || placed == null) { Revert(); return; }

        // Recalcular la celda destino final
        int gx = Mathf.RoundToInt(transform.position.x / mgr.gridSize);
        int gz = Mathf.RoundToInt(transform.position.z / mgr.gridSize);
        Vector3 snapped = new Vector3(gx * mgr.gridSize, 0f, gz * mgr.gridSize);

        bool valid = mgr.IsValidGridPosition(gx, gz);

        if (valid)
        {
            GameObject occupant = mgr.gridObjects[gx, gz, placed.layer];
            if (occupant != null && occupant != this.gameObject)
                valid = false;
        }

        if (valid)
        {
            float radius = mgr.gridSize * 0.4f;
            Collider[] cols = Physics.OverlapSphere(snapped, radius);
            foreach (var col in cols)
            {
                if (col.transform.root == this.transform) continue;
                var otherPlaced = col.GetComponent<PlacedObject>();
                if (otherPlaced != null && otherPlaced.layer == placed.layer)
                {
                    valid = false;
                    break;
                }
            }
        }

        if (!valid)
        {
            // Revertir si no es válido
            Revert();
        }
        else
        {
            // Actualizar matriz y datos del objeto
            // Borrar referencia vieja
            if (mgr.IsValidGridPosition(originalX, originalZ) &&
                mgr.gridObjects[originalX, originalZ, placed.layer] == this.gameObject)
            {
                mgr.gridObjects[originalX, originalZ, placed.layer] = null;
            }

            // Escribir nueva
            mgr.gridObjects[gx, gz, placed.layer] = this.gameObject;

            // Actualizar metadata
            placed.gridX = gx;
            placed.gridZ = gz;

            // Asegurar snap final
            transform.position = snapped;
        }

        // Restaurar color
        if (rend != null) rend.material.color = originalColor;
    }

    void Revert()
    {
        // Volver a la celda original
        transform.position = originalWorldPos;
        if (rend != null) rend.material.color = originalColor;
    }
}
