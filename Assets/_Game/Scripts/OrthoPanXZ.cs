using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Camera))]
public class OrthoPanXZ : MonoBehaviour
{
    [Header("Entrada")]
    [Tooltip("Pañear con Space + clic izquierdo (estilo Illustrator).")]
    public bool allowSpaceLeftMouse = true;
    [Tooltip("Permitir pan con botón central SIN teclas.")]
    public bool allowMiddleMouse = true;

    [Header("Sensación de movimiento")]
    public float sensitivity = 1f;                   // Escala de paneo
    [Range(0f, 1f)] public float inertia = 0.85f;    // Inercia al soltar
    public float damping = 8f;                        // Amortiguación de inercia
    public float maxSpeed = 50f;                      // Límite de velocidad

    [Header("Límites en mundo (XZ)")]
    public float minX = -50f, maxX = 50f;
    public float minZ = -50f, maxZ = 50f;

    [Header("Bloquear sobre UI")]
    public bool blockWhenPointerOverUI = true;

    Camera cam;
    Vector3 velocityXZ;        // Velocidad en XZ para inercia
    Vector3 lastMousePos;
    bool isDragging;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true; // Aseguramos ortográfica
    }

    void Update()
    {
        bool pointerOverUI = blockWhenPointerOverUI && IsPointerOverUIAny();

        // Condiciones de entrada:
        bool middleDown = allowMiddleMouse && Input.GetMouseButton(2);
        bool leftWithSpace = allowSpaceLeftMouse && Input.GetKey(KeyCode.Space) && Input.GetMouseButton(0);

        bool draggingNow = !pointerOverUI && (middleDown || leftWithSpace);

        if (draggingNow)
        {
            if (!isDragging)
            {
                isDragging = true;
                lastMousePos = Input.mousePosition;
                velocityXZ = Vector3.zero; // reset al empezar
            }

            Vector3 mouseNow = Input.mousePosition;
            Vector2 deltaPix = (Vector2)(mouseNow - lastMousePos);

            // Unidades de mundo por píxel vertical (en ortográfica)
            float worldPerPixel = (2f * cam.orthographicSize) / Mathf.Max(1, Screen.height);

            // Paneo puro XZ: pantalla X -> mundo X; pantalla Y -> mundo Z
            float dx = -deltaPix.x * worldPerPixel * sensitivity; // mano: arrastras derecha, escena va a la izquierda
            float dz = -deltaPix.y * worldPerPixel * sensitivity;

            Vector3 deltaWorld = new Vector3(dx, 0f, dz);

            // Limitar por velocidad (independiente del frame)
            float dt = Mathf.Max(Time.deltaTime, 1e-5f);
            Vector3 targetVel = deltaWorld / dt;
            if (targetVel.magnitude > maxSpeed)
                deltaWorld = targetVel.normalized * maxSpeed * dt;

            // Aplicar y clamp
            Vector3 newPos = transform.position + deltaWorld;
            newPos = ClampXZ(newPos);
            transform.position = newPos;

            // Guardar velocidad para inercia
            velocityXZ = Vector3.Lerp(velocityXZ, deltaWorld / dt, 0.5f);

            lastMousePos = mouseNow;
        }
        else
        {
            // Salir de arrastre cuando sueltas botones
            if (isDragging && !Input.GetMouseButton(0) && !Input.GetMouseButton(2))
                isDragging = false;

            // Inercia
            if (velocityXZ.sqrMagnitude > 1e-6f)
            {
                float decay = Mathf.Exp(-damping * Time.deltaTime);
                velocityXZ *= Mathf.Lerp(inertia, 0f, 1f - decay);

                if (velocityXZ.magnitude > maxSpeed)
                    velocityXZ = velocityXZ.normalized * maxSpeed;

                Vector3 newPos = transform.position + velocityXZ * Time.deltaTime;
                Vector3 clamped = ClampXZ(newPos);

                // Si pegamos contra límites, disipar más
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

        // Mouse
        if (EventSystem.current.IsPointerOverGameObject())
            return true;

        // Touch
        for (int i = 0; i < Input.touchCount; i++)
            if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId))
                return true;

        return false;
    }
}
