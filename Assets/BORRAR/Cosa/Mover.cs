using UnityEngine;
using UnityEngine.InputSystem;

public class Mover : MonoBehaviour
{
    public float vel = 5;
    public InputActionProperty mover;

    public Vector2 dirMovimiento;
    private void Start()
    {
        mover.action.Enable();
    }

    void Update()
    {
        dirMovimiento = mover.action.ReadValue<Vector2>();
        transform.Translate(dirMovimiento * vel * Time.deltaTime);
    }
}
