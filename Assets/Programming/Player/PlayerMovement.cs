using UnityEngine;

/// <summary>
/// Top-down movement relative to camera orientation.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;

    [Header("References")]
    public CameraFollow cameraFollow;

    private CharacterController controller;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        HandleMovement();
    }

    /// <summary>
    /// Moves player relative to camera forward/right direction.
    /// </summary>
    private void HandleMovement()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 move = Vector3.zero;

        if (cameraFollow != null)
        {
            Vector3 forward = cameraFollow.GetFlatForward();
            Vector3 right = cameraFollow.GetFlatRight();

            move = (right * x + forward * z).normalized;
        }
        else
        {
            move = new Vector3(x, 0f, z).normalized;
        }

        controller.Move(move * moveSpeed * Time.deltaTime);
    }
}