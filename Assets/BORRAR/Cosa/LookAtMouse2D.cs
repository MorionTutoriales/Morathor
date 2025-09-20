using UnityEngine;

public class LookAtMouse2D : MonoBehaviour
{
    public Transform padre;
    [Tooltip("Si tu sprite 'mira' con -X (izquierda) por defecto, deja esto en true. Si mira con +X, ponlo en false.")]
    public bool frontIsNegativeX = true;

    [Tooltip("Desfase extra en grados si tu arte está rotado distinto (0, 90, etc.).")]
    public float extraAngleOffset = 0f;

    void Update()
    {
        if (Camera.main == null) return;

        // Distancia en Z desde la cámara hasta el plano de tu objeto
        float zDistance;
        if (Camera.main.orthographic)
        {
            // En ortográfica no importa, pero usamos la Z del objeto para ser consistentes
            zDistance = transform.position.z - Camera.main.transform.position.z;
        }
        else
        {
            // En perspectiva, esta distancia SÍ importa
            zDistance = Mathf.Abs(transform.position.z - Camera.main.transform.position.z);
        }

        // Posición del mouse en mundo
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(
            new Vector3(Input.mousePosition.x, Input.mousePosition.y, zDistance)
        );

        // Dirección en 2D (ignoramos Z)
        Vector2 dir = (mouseWorld - transform.position);
        if (dir.sqrMagnitude < 0.000001f) return; // evita NaN cuando el mouse está justo encima

        // Ángulo hacia el mouse (0° = +X)
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // Si el frente del sprite es -X, sumamos 180
        if (frontIsNegativeX) angle += 180f;

        // Offset adicional por si tu arte está rotado
        angle += extraAngleOffset;

        // Solo rotamos en Z para 2D
        if (padre.localScale.x < 0)
        {
            angle += 180;
        }
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
