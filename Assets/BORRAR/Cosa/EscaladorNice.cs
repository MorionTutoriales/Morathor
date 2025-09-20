using UnityEngine;

public class EscaladorNice : MonoBehaviour
{
    public Transform canhon;
    public Transform cabeza;
    public float umbral = 0.1f;
    private void FixedUpdate()
    {
        if (canhon.position.x < cabeza.position.x)
        {
            if (transform.localScale.x < 0)
            {
                if (canhon.position.x - cabeza.position.x < umbral)
                    transform.localScale = Vector3.one;
            }
        }
        else
        {
            if (transform.localScale.x > 0)
            {
                if (canhon.position.x - cabeza.position.x > umbral)
                    transform.localScale = new Vector3(-1, 1, 1);
            }
            
        }
    }
}
