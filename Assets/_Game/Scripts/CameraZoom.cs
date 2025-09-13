using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
public class CameraZoom : MonoBehaviour
{
    [Header("Configuración")]
    public Slider zoomSlider;           // Asigna el slider desde el inspector
    public float minZoom = 2f;          // Valor mínimo de ortographicSize
    public float maxZoom = 20f;         // Valor máximo de ortographicSize
    public float zoomStep = 1f;         // Incremento por “ruedita”

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true; // aseguramos que sea ortográfica

        if (zoomSlider != null)
        {
            // configurar límites del slider
            zoomSlider.minValue = minZoom;
            zoomSlider.maxValue = maxZoom;

            // inicializar valor actual
            zoomSlider.value = cam.orthographicSize;

            // suscribirse al evento del slider
            zoomSlider.onValueChanged.AddListener(OnSliderChanged);
        }
    }

    void Update()
    {
        if (zoomSlider == null) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel"); // + arriba, - abajo

        if (Mathf.Abs(scroll) > 0.001f)
        {
            float newZoom = Mathf.Clamp(zoomSlider.value - scroll * zoomStep, minZoom, maxZoom);
            zoomSlider.value = newZoom;
            // Esto disparará OnSliderChanged automáticamente
        }
    }

    // Método público para que el slider también pueda controlar el zoom
    public void OnSliderChanged(float value)
    {
        if (cam != null)
            cam.orthographicSize = value;
    }
}
