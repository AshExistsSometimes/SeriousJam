using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles switching between follow camera mode
/// and Binding Of Isaac style room camera mode.
/// </summary>
public class CameraManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CameraFollow cameraFollow;

    [Header("Zoom Switching")]
    [SerializeField] private float mouseWheelThreshold = 0.1f;

    [Header("Room Camera")]
    [SerializeField] private float roomMoveDuration = 0.4f;

    [SerializeField]
    private AnimationCurve roomMoveCurve =
        AnimationCurve.EaseInOut(0, 0, 1, 1);

    public CameraMode CurrentMode = CameraMode.Room;

    private Camera mainCam;

    public static CameraManager Instance;

    private List<RoomFogOfWar> rooms =
        new List<RoomFogOfWar>();

    private RoomFogOfWar currentRoom;

    private Coroutine moveRoutine;

    private void Awake()
    {
        Instance = this;

        mainCam = Camera.main;

        if (cameraFollow == null)
            cameraFollow = mainCam.GetComponent<CameraFollow>();
    }

    private void Update()
    {
        HandleZoomSwitch();

        if (CurrentMode == CameraMode.Room)
            UpdateRoomCamera();
    }

    /// <summary>
    /// Called whenever a new dungeon is generated.
    /// </summary>
    public void RefreshRooms()
    {
        rooms.Clear();

        rooms.AddRange(
            FindObjectsByType<RoomFogOfWar>(
                FindObjectsSortMode.None));
    }

    private void HandleZoomSwitch()
    {
        float wheel = Input.mouseScrollDelta.y;

        if (wheel > mouseWheelThreshold)
        {
            SetCameraMode(CameraMode.Follow);
        }
        else if (wheel < -mouseWheelThreshold)
        {
            SetCameraMode(CameraMode.Room);
        }
    }

    public void SetCameraMode(CameraMode mode)
    {
        CurrentMode = mode;

        bool roomMode = mode == CameraMode.Room;

        foreach (RoomFogOfWar room in rooms)
        {
            if (room != null)
                room.UseRoomCameraSystem = roomMode;
        }

        if (cameraFollow != null)
            cameraFollow.externalControl = roomMode;

        if (roomMode)
        {
            currentRoom = null;
            SnapToCurrentRoom();
        }
    }

    private void UpdateRoomCamera()
    {
        RoomFogOfWar activeRoom = null;

        foreach (RoomFogOfWar room in rooms)
        {
            if (room == null)
                continue;

            if (room.PlayerInside)
            {
                activeRoom = room;
                break;
            }
        }

        if (activeRoom == null)
            return;

        if (activeRoom == currentRoom)
            return;

        currentRoom = activeRoom;

        Transform target =
            currentRoom.RoomCameraPosition;

        if (target == null)
            return;

        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine =
            StartCoroutine(
                MoveCamera(target.position));
    }

    private IEnumerator MoveCamera(Vector3 target)
    {
        Vector3 start = mainCam.transform.position;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / roomMoveDuration;

            float curve =
                roomMoveCurve.Evaluate(t);

            mainCam.transform.position =
                Vector3.Lerp(
                    start,
                    target,
                    curve);

            yield return null;
        }

        mainCam.transform.position =
            target;
    }

    private void SnapToCurrentRoom()
    {
        RoomFogOfWar activeRoom = null;

        foreach (RoomFogOfWar room in rooms)
        {
            if (room != null && room.PlayerInside)
            {
                activeRoom = room;
                break;
            }
        }

        if (activeRoom == null)
            return;

        currentRoom = activeRoom;

        Transform target = currentRoom.RoomCameraPosition;

        if (target == null)
            return;

        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        moveRoutine = StartCoroutine(
            MoveCamera(target.position));
    }
}