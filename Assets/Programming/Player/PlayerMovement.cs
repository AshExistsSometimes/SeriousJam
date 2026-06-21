using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float acceleration = 12f;
    public float deceleration = 16f;

    [Header("Spin Dash")]
    [Tooltip("Total distance travelled during the dash")]
    public float spinDashDistance = 6f;
    [Tooltip("How long the dash takes in seconds")]
    public float spinDashDuration = 0.3f;
    [Tooltip("Curve controlling POSITION progress over time (0?1 on both axes). " +
             "S-curve = ease in/out. Starts flat, steep middle, flat end. " +
             "Must start at (0,0) and end at (1,1).")]
    public AnimationCurve spinDashCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("References")]
    public CameraFollow cameraFollow;

    private CharacterController controller;
    private Vector3 currentVelocity;

    private bool isDashing;
    private Vector3 dashDir;
    private float dashElapsed;
    private Vector3 dashStartPos;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (isDashing)
            HandleDash();
        else
            HandleMovement();
    }

    // ?? MOVEMENT ??????????????????????????????????????????????????????????????

    private void HandleMovement()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 moveDirection;
        if (cameraFollow != null)
        {
            Vector3 forward = cameraFollow.GetFlatForward();
            Vector3 right = cameraFollow.GetFlatRight();
            moveDirection = (right * x + forward * z).normalized;
        }
        else
        {
            moveDirection = new Vector3(x, 0f, z).normalized;
        }

        Vector3 targetVelocity = moveDirection * moveSpeed;
        float rate = moveDirection.sqrMagnitude > 0.01f ? acceleration : deceleration;

        currentVelocity = Vector3.MoveTowards(
            currentVelocity, targetVelocity, rate * Time.deltaTime);

        controller.Move(currentVelocity * Time.deltaTime);
    }

    // ?? DASH ??????????????????????????????????????????????????????????????????

    /// <summary>Called by PlayerCombat with a flat normalised aim direction.</summary>
    public void StartDash(Vector3 dir)
    {
        if (isDashing) return;

        dashDir = dir;
        dashElapsed = 0f;
        dashStartPos = transform.position;
        isDashing = true;
        currentVelocity = Vector3.zero;
    }

    private void HandleDash()
    {
        dashElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(dashElapsed / spinDashDuration);

        // Previous and current progress along the total distance
        float prevProgress = spinDashCurve.Evaluate(Mathf.Clamp01((dashElapsed - Time.deltaTime) / spinDashDuration));
        float currProgress = spinDashCurve.Evaluate(t);

        // Move only the delta this frame — guarantees exact total distance travelled
        float frameDelta = (currProgress - prevProgress) * spinDashDistance;
        controller.Move(dashDir * frameDelta);

        if (t >= 1f)
        {
            isDashing = false;
            currentVelocity = Vector3.zero;
        }
    }

    public bool IsDashing => isDashing;
}