using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RoomFogOfWar : MonoBehaviour
{
    [Header("Room Type")]
    [SerializeField] private bool isStartRoom;

    [Header("Fog Overlay")]
    [SerializeField] private Renderer overlayRenderer;
    [SerializeField] private float fadeSpeed = 2f;

    [Header("Map Blocker")]
    [SerializeField] private GameObject mapBlocker;
    [SerializeField] private float blockerFadeSpeed = 2f;

    [Header("Room Camera System")]
    [SerializeField] private bool useRoomCameraSystem = true;
    [SerializeField] private Transform roomCamPosition;
    [SerializeField]
    private AnimationCurve cameraLerpCurve =
        AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float cameraLerpSpeed = 2f;

    private MaterialPropertyBlock propertyBlock;
    private Coroutine fadeRoutine;

    private bool playerInside;
    private bool blockerRevealed;

    public bool PlayerInside => playerInside;

    public Transform RoomCameraPosition => roomCamPosition;

    private float currentAlpha;

    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorID = Shader.PropertyToID("_Color");

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();

        GetComponent<Collider>().isTrigger = true;

        currentAlpha = isStartRoom ? 0f : 1f;
        SetOverlayAlpha(currentAlpha);

        if (isStartRoom)
        {
            playerInside = true;
            blockerRevealed = true;

            if (mapBlocker != null)
                mapBlocker.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;

        playerInside = true;

        StartFade(0f);

        if (!blockerRevealed && mapBlocker != null)
        {
            blockerRevealed = true;
            StartCoroutine(FadeAndDestroyBlocker());
        }

    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) return;

        playerInside = false;

        StartFade(1f);
    }


    // ---------------- FOG ----------------

    private void StartFade(float targetAlpha)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeOverlay(targetAlpha));
    }

    private IEnumerator FadeOverlay(float targetAlpha)
    {
        while (!Mathf.Approximately(currentAlpha, targetAlpha))
        {
            currentAlpha = Mathf.MoveTowards(
                currentAlpha,
                targetAlpha,
                Time.deltaTime * fadeSpeed
            );

            SetOverlayAlpha(currentAlpha);
            yield return null;
        }

        currentAlpha = targetAlpha;
        SetOverlayAlpha(currentAlpha);
    }

    private void SetOverlayAlpha(float alpha)
    {
        if (!overlayRenderer) return;

        overlayRenderer.GetPropertyBlock(propertyBlock);

        Color col;

        if (overlayRenderer.sharedMaterial.HasProperty(BaseColorID))
            col = overlayRenderer.sharedMaterial.GetColor(BaseColorID);
        else
            col = overlayRenderer.sharedMaterial.GetColor(ColorID);

        col.a = alpha;

        propertyBlock.SetColor(BaseColorID, col);
        propertyBlock.SetColor(ColorID, col);

        overlayRenderer.SetPropertyBlock(propertyBlock);
    }

    // ---------------- BLOCKER ----------------

    private IEnumerator FadeAndDestroyBlocker()
    {
        if (mapBlocker == null) yield break;

        Renderer r = mapBlocker.GetComponent<Renderer>();
        if (r == null)
        {
            mapBlocker.SetActive(false);
            Destroy(mapBlocker);
            yield break;
        }

        MaterialPropertyBlock mpb = new MaterialPropertyBlock();

        float alpha = 1f;
        Color col = Color.black;

        while (alpha > 0f)
        {
            alpha = Mathf.MoveTowards(alpha, 0f, Time.deltaTime * blockerFadeSpeed);

            col.a = alpha;

            mpb.SetColor(BaseColorID, col);
            mpb.SetColor(ColorID, col);

            r.SetPropertyBlock(mpb);

            yield return null;
        }

        mapBlocker.SetActive(false);
        Destroy(mapBlocker);
    }

    // ---------------- UTIL ----------------

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player") ||
               other.GetComponent<PlayerMovement>() != null;
    }

    public bool UseRoomCameraSystem
    {
        get => useRoomCameraSystem;
        set => useRoomCameraSystem = value;
    }
}