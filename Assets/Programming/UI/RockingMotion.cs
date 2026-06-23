using UnityEngine;

/// <summary>
/// Applies a continuous rocking motion to an object.
/// Useful for menu idle animations such as a character
/// leaning back in a chair.
/// </summary>
public class RockingMotion : MonoBehaviour
{
    [Header("Rocking")]
    [SerializeField] private Vector3 rotationAxis = Vector3.forward;
    [SerializeField] private float maxAngle = 5f;
    [SerializeField] private float speed = 1f;

    private Quaternion startingRotation;

    /// <summary>
    /// Stores the starting rotation.
    /// </summary>
    private void Start()
    {
        startingRotation = transform.localRotation;
    }

    /// <summary>
    /// Applies rocking motion.
    /// </summary>
    private void Update()
    {
        float angle = Mathf.Sin(Time.time * speed) * maxAngle;

        transform.localRotation =
            startingRotation *
            Quaternion.AngleAxis(angle, rotationAxis.normalized);
    }
}