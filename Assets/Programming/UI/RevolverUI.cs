using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RevolverUI : MonoBehaviour
{
    [Header("References")]
    public RectTransform drum;
    public Image[] bulletSlots;

    [Header("Rotation Settings")]
    public AnimationCurve rotationCurve;
    public float rotateDuration = 0.2f;

    private float accumulatedRotation;
    private bool isAnimating;
    private int activeSlotIndex = 0;

    // Cached slot count — avoids repeated .Length calls and protects against
    // the array being unassigned in the Inspector
    private int SlotCount => bulletSlots != null ? bulletSlots.Length : 0;

    private void Awake()
    {
        // Catch misconfiguration immediately with a useful message
        if (SlotCount == 0)
            Debug.LogError("[RevolverUI] bulletSlots is empty or unassigned! " +
                           "Drag your 6 bullet Image components into the array in the Inspector.", this);
    }

    // ?? PUBLIC API ????????????????????????????????????????????????????????????

    public void OnFire()
    {
        if (SlotCount == 0) return;

        SetSlotSprite(activeSlotIndex, null);
        activeSlotIndex = (activeSlotIndex + 1) % SlotCount;
        StartCoroutine(AnimateRotation(60f));
    }

    public void OnReloadStep(BulletSO bullet, int slotIndex)
    {
        if (SlotCount == 0) return;

        int physicalSlot = (activeSlotIndex + slotIndex) % SlotCount;
        SetSlotSprite(physicalSlot, bullet.icon);
        StartCoroutine(AnimateRotation(60f));
    }

    public void ClearAllSlots()
    {
        activeSlotIndex = 0;
        accumulatedRotation = 0f;
        drum.localRotation = Quaternion.identity;

        for (int i = 0; i < SlotCount; i++)
            SetSlotSprite(i, null);
    }

    public void SyncInstant(List<BulletSO> ammo)
    {
        activeSlotIndex = 0;
        accumulatedRotation = 0f;
        drum.localRotation = Quaternion.identity;

        for (int i = 0; i < SlotCount; i++)
        {
            Sprite icon = (i < ammo.Count && ammo[i] != null) ? ammo[i].icon : null;
            SetSlotSprite(i, icon);
        }
    }

    public IEnumerator WaitForAnimation()
    {
        while (isAnimating)
            yield return null;
    }

    // ?? INTERNAL ??????????????????????????????????????????????????????????????

    private void SetSlotSprite(int index, Sprite sprite)
    {
        if (index < 0 || index >= SlotCount) return;
        bulletSlots[index].sprite = sprite;
        bulletSlots[index].enabled = sprite != null;
    }

    private IEnumerator AnimateRotation(float delta)
    {
        while (isAnimating)
            yield return null;

        isAnimating = true;

        float startRot = accumulatedRotation;
        float endRot = accumulatedRotation + delta;
        float elapsed = 0f;

        while (elapsed < rotateDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / rotateDuration);
            drum.localRotation = Quaternion.Euler(0f, 0f,
                Mathf.Lerp(startRot, endRot, rotationCurve.Evaluate(t)));
            yield return null;
        }

        accumulatedRotation = endRot;
        drum.localRotation = Quaternion.Euler(0f, 0f, accumulatedRotation);
        isAnimating = false;
    }
}