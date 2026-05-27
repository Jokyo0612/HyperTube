using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float ForwardSpeed = 2f; 
    public float SlideSpeed = 150f;

    [Header("References")]
    public Transform Pivot;

    public Transform MainCamera;
    public Transform CamTarget;

    private Vector2 moveInput;

    void Update()
    {
        transform.Translate(Vector3.forward * ForwardSpeed * Time.deltaTime, Space.World);

        if (moveInput.x != 0)
        {
            float rotationAmount = moveInput.x * SlideSpeed * Time.deltaTime;

            Pivot.Rotate(0, 0, rotationAmount);
        }
    }

    void LateUpdate()
    {
        if (MainCamera != null && CamTarget != null)
        {
            MainCamera.position = CamTarget.position;
            MainCamera.rotation = transform.rotation;
        }
    }

    void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }
}