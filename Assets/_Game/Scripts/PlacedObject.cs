using UnityEngine;

public class PlacedObject : MonoBehaviour
{
    public int gridX;
    public int gridZ;
    public int layer;
    public string prefabName;

    void Start()
    {
        // Asegurar que el objeto tenga un collider
        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<BoxCollider>();
        }
    }

    public void OnDestroy()
    {
        // Cleanup cuando el objeto es destruido
    }
}