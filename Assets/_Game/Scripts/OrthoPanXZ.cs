using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Camera))]
public class OrthoPanXZ : MonoBehaviour
{
    [Header("Entrada")]
    public bool allowSpaceLeftMouse = true;   // Space + LMB
    public bool allowMiddleMouse = true;      // MMB sin teclas

    [Header("Sensación de movimiento")]
    public float sensitivity = 1f;
    [Range(0f, 1f)] public float inertia = 0.85f;
    public float damping = 8f;
    public float maxSpeed = 50f;
    [Tooltip("Al cambiar zoom, ¿cancelar inercia para evitar saltos?")]
    public bool cancelInertiaOnZoomChange = true;

    [Header("Límites en mundo (XZ)")]
    public float minX = -50f, maxX = 50f;
    public float minZ = -50f, maxZ = 50f;

    [Header("Bloquear sobre UI")]
    public bool blockWhenPointerOverUI = true;

    Camera cam;
    Vector3 velocityXZ;
    Vector3 lastMousePos;
    bool isDragging;

    float lastKnownZoom; // para escalar inercia cuando cambia el zoom

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        lastKnownZoom = cam.orthographicSize;
    }

    void Update()
    {
        bool pointerOverUI = blockWhenPointerOverUI && IsPointerOverUIAny();
        bool middleDown = allowMiddleMouse && Input.GetMouseButton(2);
        bool leftWithSpace = allowSpaceLeftMouse && Input.GetKey(KeyCode.Space) && Input.GetMouseButton(0);
        bool draggingNow = !pointerOverUI && (middleDown || leftWithSpace);

        if (draggingNow)
        {
            if (!isDragging)
            {
                isDragging = true;
                lastMousePos = Input.mousePosition;
                // No borramos velocityXZ aquí; mantenerla da una sensación más continua.
            }

            Vector3 mouseNow = Input.mousePosition;
            Vector2 deltaPix = (Vector2)(mouseNow - lastMousePos);

            // Escala dependiente del zoom: unidades de mundo por píxel vertical
            float worldPerPixel = (2f * cam.orthographicSize) / Mathf.Max(1, Screen.height);

            // Mano: arrastras derecha -> escena se mueve a la izquierda
            float dx = -deltaPix.x * worldPerPixel * sensitivity;
            float dz = -deltaPix.y * worldPerPixel * sensitivity;

            Vector3 deltaWorld = new Vector3(dx, 0f, dz);

            float dt = Mathf.Max(Time.deltaTime, 1e-5f);
            Vector3 targetVel = deltaWorld / dt;
            if (targetVel.magnitude > maxSpeed)
                deltaWorld = targetVel.normalized * maxSpeed * dt;

            Vector3 newPos = transform.position + deltaWorld;
            newPos = ClampXZ(newPos);
            transform.position = newPos;

            // Guardar velocidad para inercia (promedia para suavizar)
            velocityXZ = Vector3.Lerp(velocityXZ, deltaWorld / dt, 0.5f);

            lastMousePos = mouseNow;
        }
        else
        {
            if (isDragging && !Input.GetMouseButton(0) && !Input.GetMouseButton(2))
                isDragging = false;

            // Inercia con amortiguación
            if (velocityXZ.sqrMagnitude > 1e-6f)
            {
                float decay = Mathf.Exp(-damping * Time.deltaTime);
                velocityXZ *= Mathf.Lerp(inertia, 0f, 1f - decay);

                if (velocityXZ.magnitude > maxSpeed)
                    velocityXZ = velocityXZ.normalized * maxSpeed;

                Vector3 newPos = transform.position + velocityXZ * Time.deltaTime;
                Vector3 clamped = ClampXZ(newPos);

                if (clamped == transform.position)
                    velocityXZ *= 0.25f;

                transform.position = clamped;

                if (velocityXZ.sqrMagnitude < 1e-6f)
                    velocityXZ = Vector3.zero;
            }
        }
    }

    Vector3 ClampXZ(Vector3 pos)
    {
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.z = Mathf.Clamp(pos.z, minZ, maxZ);
        return pos;
    }

    bool IsPointerOverUIAny()
    {
        if (EventSystem.current == null) return false;
        if (EventSystem.current.IsPointerOverGameObject()) return true;
        for (int i = 0; i < Input.touchCount; i++)
            if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId))
                return true;
        return false;
    }

    /// <summary>
    /// Llama este método cuando cambie el zoom (antes/después está bien).
    /// Escala la inercia para que no se sienta un “salto” y reinicia el anclaje del mouse.
    /// </summary>
    public void OnZoomChanged(float oldZoom, float newZoom)
    {
        if (cancelInertiaOnZoomChange)
        {
            velocityXZ = Vector3.zero;  // opción segura: sin saltos
        }
        else
        {
            // Opción avanzada: reescalar la velocidad a la nueva “magnitud del mundo por píxel”
            float oldWorldPerPixel = (2f * Mathf.Max(oldZoom, 1e-5f)) / Mathf.Max(1, Screen.height);
            float newWorldPerPixel = (2f * Mathf.Max(newZoom, 1e-5f)) / Mathf.Max(1, Screen.height);
            float k = (newWorldPerPixel / oldWorldPerPixel);
            velocityXZ *= k;
        }

        lastKnownZoom = newZoom;
        lastMousePos = Input.mousePosition; // Evita un delta gigante en el próximo frame
    }
}
