using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
public class CameraZoom : MonoBehaviour
{
    [Header("Configuraci�n")]
    public Slider zoomSlider;           // Asigna el slider desde el inspector
    public float minZoom = 2f;          // Valor m�nimo de ortographicSize
    public float maxZoom = 20f;         // Valor m�ximo de ortographicSize
    public float zoomStep = 1f;         // Incremento por �ruedita�

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true; // aseguramos que sea ortogr�fica

        if (zoomSlider != null)
        {
            // configurar l�mites del slider
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
            // Esto disparar� OnSliderChanged autom�ticamente
        }
    }

    // M�todo p�blico para que el slider tambi�n pueda controlar el zoom
    public void OnSliderChanged(float value)
    {
        if (cam != null)
            cam.orthographicSize = value;
    }
}
