using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
public class CameraZoom : MonoBehaviour
{
    public Slider zoomSlider;
    public float minZoom = 2f;
    public float maxZoom = 20f;
    public float zoomStep = 1f;

    private Camera cam;
    private OrthoPanXZ pan;    // referencia al script de paneo
    private float lastZoom;

    void Start()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        pan = GetComponent<OrthoPanXZ>();

        if (zoomSlider != null)
        {
            zoomSlider.minValue = minZoom;
            zoomSlider.maxValue = maxZoom;
            zoomSlider.value = cam.orthographicSize;
            zoomSlider.onValueChanged.AddListener(OnSliderChanged);
        }

        lastZoom = cam.orthographicSize;
        // Notificar estado inicial (opcional):
        if (pan != null) pan.OnZoomChanged(lastZoom, cam.orthographicSize);
    }

    void Update()
    {
        if (zoomSlider == null) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel"); // + arriba, - abajo
        if (Mathf.Abs(scroll) > 0.001f)
        {
            float newZoom = Mathf.Clamp(zoomSlider.value - scroll * zoomStep, minZoom, maxZoom);
            if (!Mathf.Approximately(newZoom, zoomSlider.value))
            {
                float old = zoomSlider.value;
                zoomSlider.value = newZoom; // Dispara OnSliderChanged

                // Aviso manual si quieres exactitud de old/new:
                if (pan != null) pan.OnZoomChanged(old, newZoom);
            }
        }
    }

    // Público para usar desde el Slider (UI)
    public void OnSliderChanged(float value)
    {
        float old = cam.orthographicSize;
        cam.orthographicSize = Mathf.Clamp(value, minZoom, maxZoom);

        if (pan != null && !Mathf.Approximately(old, cam.orthographicSize))
            pan.OnZoomChanged(old, cam.orthographicSize);
    }
}
