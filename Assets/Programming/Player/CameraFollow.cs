using UnityEngine;

/// <summary>
/// Top-down camera that follows player with a fixed offset.
/// Adds subtle screen-edge look-ahead based on mouse position.
/// Does NOT rotate or orbit.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Base Offset")]
    public Vector3 baseOffset = new Vector3(0f, 15f, -8f);

    [Header("Mouse Look-Ahead")]
    public float lookAheadStrength = 3f;
    public float maxLookAhead = 3f;

    [Header("Smoothing")]
    public float smoothSpeed = 8f;

    private void LateUpdate()
    {
        if (player == null) return;

        Vector3 targetPos = player.position + baseOffset;

        Vector3 mouseOffset = CalculateMouseOffset();

        targetPos += mouseOffset;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPos,
            smoothSpeed * Time.deltaTime
        );

        // IMPORTANT: no rotation tracking, fixed angle only
        transform.rotation = Quaternion.Euler(60f, 0f, 0f);
    }

    /// <summary>
    /// Computes camera offset based on mouse proximity to screen edges.
    /// </summary>
    private Vector3 CalculateMouseOffset()
    {
        Vector3 mouse = Input.mousePosition;

        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        // Normalized mouse position (-1 to 1)
        float x = (mouse.x / screenWidth - 0.5f) * 2f;
        float z = (mouse.y / screenHeight - 0.5f) * 2f;

        // Clamp to prevent extreme camera movement
        Vector3 offset = new Vector3(x, 0f, z);
        offset = Vector3.ClampMagnitude(offset, 1f);

        return offset * lookAheadStrength;
    }

    /// <summary>
    /// Returns camera-relative movement forward (flat).
    /// </summary>
    public Vector3 GetFlatForward()
    {
        Vector3 f = transform.forward;
        f.y = 0f;
        return f.normalized;
    }

    /// <summary>
    /// Returns camera-relative right direction (flat).
    /// </summary>
    public Vector3 GetFlatRight()
    {
        Vector3 r = transform.right;
        r.y = 0f;
        return r.normalized;
    }
}