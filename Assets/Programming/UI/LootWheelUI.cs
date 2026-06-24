using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LootWheelUI : MonoBehaviour
{
    [System.Serializable]
    public class WheelSlot
    {
        public Image iconImage;
        public bool isJackpotSlot;
        [HideInInspector] public BulletSO assignedReward;
    }

    public static LootWheelUI Instance;

    [Header("Wheel Setup")]
    [SerializeField] private RectTransform wheelRoot;
    [SerializeField] private List<WheelSlot> slots = new();

    [Header("Spin Settings")]
    [SerializeField] private float spinTime = 2.5f;
    [SerializeField] private int revolutions = 4;
    [SerializeField] private AnimationCurve spinCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Pointer Alignment")]
    [Tooltip("Angle offset (degrees) to align slot 0 with your pointer at rest. " +
             "Tweak this if the wrong slot lands under the pointer.")]
    [SerializeField] private float pointerOffsetDegrees = 0f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip spinMusic;
    [SerializeField] private AudioClip tickSound;
    [SerializeField] private AudioClip normalWinSound;
    [SerializeField] private AudioClip jackpotWinSound;

    [Header("UI")]
    [SerializeField] private TMP_Text rewardText;
    [SerializeField] private GameObject jackpotBanner;
    [SerializeField] private GameObject PressToSpinPopup;

    // ?? STATE ?????????????????????????????????????????????????????????????????

    private readonly List<BulletSO> standardPool = new();
    private readonly List<BulletSO> jackpotPool = new();

    private LootPickup currentPickup;
    private PlayerCombat playerCombat;
    private PlayerMovement playerMovement;

    private bool isSpinning;
    private bool waitingForClose;

    private BulletSO lockedReward;
    private int lockedIndex;
    private int previousSegment = -1;

    private static int spinCount = 0;

    // ?? LIFECYCLE ?????????????????????????????????????????????????????????????

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        playerCombat = FindFirstObjectByType<PlayerCombat>();
        playerMovement = FindFirstObjectByType<PlayerMovement>();

        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!gameObject.activeSelf) return;

        if (waitingForClose)
        {
            if (Input.anyKeyDown) CloseWheel();
            return;
        }

        if (isSpinning) return;

        bool spinPressed =
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.LeftShift) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetMouseButtonDown(0);

        if (spinPressed)
            StartCoroutine(SpinRoutine());
    }

    // ?? OPEN / CLOSE ??????????????????????????????????????????????????????????

    public void OpenWheel(List<BulletSO> regularPool, List<BulletSO> jackpot, LootPickup pickup)
    {
        if (isSpinning) return;

        standardPool.Clear();
        jackpotPool.Clear();

        rewardText.gameObject.SetActive(false);
        jackpotBanner.SetActive(false);
        PressToSpinPopup.SetActive(true);

        standardPool.AddRange(regularPool);
        jackpotPool.AddRange(jackpot);

        currentPickup = pickup;

        ResetWheelRotation();
        PopulateWheel();
        PausePlayer(true);

        waitingForClose = false;
        isSpinning = false;

        gameObject.SetActive(true);

        Debug.Log($"[LootWheel] Opened. Standard={standardPool.Count} Jackpot={jackpotPool.Count}");
    }

    private void CloseWheel()
    {
        currentPickup?.Consume();
        currentPickup = null;

        if (jackpotBanner != null)
            jackpotBanner.SetActive(false);

        PausePlayer(false);

        waitingForClose = false;
        isSpinning = false;

        gameObject.SetActive(false);
    }

    // ?? POPULATE ??????????????????????????????????????????????????????????????

    private void PopulateWheel()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            WheelSlot slot = slots[i];

            BulletSO reward = null;

            if (slot.isJackpotSlot && jackpotPool.Count > 0)
                reward = jackpotPool[Random.Range(0, jackpotPool.Count)];
            else if (standardPool.Count > 0)
                reward = standardPool[Random.Range(0, standardPool.Count)];

            slot.assignedReward = reward;

            if (slot.iconImage != null)
                slot.iconImage.sprite = reward != null ? reward.icon : null;

            Debug.Log($"[LootWheel] Slot {i} = {(reward ? reward.bulletName : "NULL")} | Jackpot={slot.isJackpotSlot}");
        }
    }

    // ?? SPIN ??????????????????????????????????????????????????????????????????

    private IEnumerator SpinRoutine()
    {
        isSpinning = true;
        previousSegment = -1;

        PressToSpinPopup.SetActive(false);

        int slotCount = slots.Count;
        float anglePerSlot = 360f / slotCount;

        // Pick winner — on first spin, avoid slot 0 (too obviously rigged)
        if (spinCount == 0)
        {
            List<int> biased = new();
            for (int i = 1; i < slotCount; i++)
                biased.Add(i);
            lockedIndex = biased[Random.Range(0, biased.Count)];
        }
        else
        {
            lockedIndex = Random.Range(0, slotCount);
        }
        spinCount++;

        lockedReward = slots[lockedIndex].assignedReward;

        Debug.Log($"[LootWheel] Target index={lockedIndex} reward={lockedReward?.bulletName}");

        float slotAngle = lockedIndex * anglePerSlot;
        float landAngle = slotAngle + pointerOffsetDegrees;

        // Normalise landAngle into 0-360 so we always spin in the same direction
        landAngle = ((landAngle % 360f) + 360f) % 360f;

        // Add full revolutions (always spin the same direction: increasing Z)
        float targetAngle = landAngle + (360f * revolutions);

        float startAngle = wheelRoot.eulerAngles.z;

        Debug.Log($"[LootWheel] startAngle={startAngle:F1} slotAngle={slotAngle:F1} targetAngle={targetAngle:F1}");

        // ?? AUDIO ?????????????????????????????????????????????????????????????

        if (audioSource != null && spinMusic != null)
        {
            audioSource.clip = spinMusic;
            audioSource.loop = true;
            audioSource.Play();
        }

        // ?? ANIMATE ???????????????????????????????????????????????????????????

        float elapsed = 0f;

        while (elapsed < spinTime)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / spinTime);
            float progress = spinCurve.Evaluate(t);
            float z = Mathf.Lerp(startAngle, targetAngle, progress);

            wheelRoot.rotation = Quaternion.Euler(0f, 0f, z);

            // Tick sound each time we cross a new segment
            float wrapped = Mathf.Repeat(z - pointerOffsetDegrees, 360f);
            int segment = Mathf.FloorToInt(wrapped / anglePerSlot) % slotCount;

            if (segment != previousSegment)
            {
                previousSegment = segment;
                if (audioSource != null && tickSound != null)
                    audioSource.PlayOneShot(tickSound);
            }

            yield return null;
        }

        // Snap exactly to target so floating point can't drift
        wheelRoot.rotation = Quaternion.Euler(0f, 0f, targetAngle);

        if (audioSource != null)
            audioSource.Stop();

        // ?? VERIFY (now should always match) ??????????????????????????????????

        float finalWrapped = Mathf.Repeat(targetAngle - pointerOffsetDegrees, 360f);
        int finalSegment = Mathf.FloorToInt(finalWrapped / anglePerSlot) % slotCount;

        if (finalSegment != lockedIndex)
            Debug.LogError($"[LootWheel] MISMATCH: expected {lockedIndex} but pointer is on {finalSegment}. " +
                           $"Adjust pointerOffsetDegrees (currently {pointerOffsetDegrees}).");
        else
            Debug.Log($"[LootWheel] Landed correctly on index={lockedIndex} reward={lockedReward?.bulletName}");

        // ?? REWARD ????????????????????????????????????????????????????????????

        if (lockedReward != null && playerCombat != null)
            playerCombat.AmmoPool.Add(lockedReward);

        if (rewardText != null)
        {
            rewardText.gameObject.SetActive(true);
            rewardText.text = lockedReward != null ? $"You got: {lockedReward.bulletName}" : "No reward!";
        }

        // ?? WIN AUDIO ?????????????????????????????????????????????????????????

        if (audioSource != null)
        {
            bool isJackpot = slots[lockedIndex].isJackpotSlot;

            if (isJackpot && jackpotWinSound != null)
            {
                audioSource.PlayOneShot(jackpotWinSound);
                if (jackpotBanner != null)
                    jackpotBanner.SetActive(true);
            }
            else if (normalWinSound != null)
            {
                audioSource.PlayOneShot(normalWinSound);
            }
        }

        waitingForClose = true;
        isSpinning = false;
    }

    // ?? HELPERS ???????????????????????????????????????????????????????????????

    private void ResetWheelRotation()
    {
        wheelRoot.rotation = Quaternion.identity;
    }

    private void PausePlayer(bool paused)
    {
        if (playerCombat != null) playerCombat.CombatPaused = paused;
        if (playerMovement != null) playerMovement.MovementPaused = paused;
    }
}