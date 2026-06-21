using System.Collections;
using UnityEngine;

public class PlayerModelSpin : MonoBehaviour
{
    [Header("Spin Settings")]
    [Tooltip("Total number of full 360° rotations the model makes")]
    public int spinRevolutions = 2;

    [Tooltip("How long the full spin animation takes in seconds")]
    public float spinDuration = 0.6f;

    [Tooltip("Curve controlling rotation POSITION over time (not speed). " +
             "X = normalised time 0?1. Y = normalised progress 0?1. " +
             "Use an ease-in-out S-curve: start flat, steep in the middle, flat at the end. " +
             "MUST start at (0,0) and end at (1,1).")]
    public AnimationCurve spinCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private bool isSpinning;

    public void TriggerSpin()
    {
        if (isSpinning) return;
        StartCoroutine(SpinRoutine());
    }

    // Lets PlayerCombat wait until the spin finishes if needed
    public IEnumerator WaitForSpin()
    {
        while (isSpinning)
            yield return null;
    }

    private IEnumerator SpinRoutine()
    {
        isSpinning = true;

        float totalDegrees = 360f * spinRevolutions;
        float elapsed = 0f;

        while (elapsed < spinDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / spinDuration);
            float progress = spinCurve.Evaluate(t);   // 0?1 positional progress
            transform.localRotation = Quaternion.Euler(0f, totalDegrees * progress, 0f);
            yield return null;
        }

        // Always land exactly on identity — no snap because the curve already
        // brought us to progress=1 smoothly
        transform.localRotation = Quaternion.identity;

        isSpinning = false;
    }
}