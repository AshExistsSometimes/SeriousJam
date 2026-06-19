using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    public Transform firePoint;
    public Transform aimMarker;
    public Camera cam;
    public RevolverUI revolverUI;

    [Header("Aim")]
    public float aimPlaneHeight = 0f;

    [Header("Ammo")]
    public List<BulletSO> availableAmmo = new();
    public List<BulletSO> currentAmmo = new();
    public int cylinderSize = 6;

    [Header("Fire")]
    public float fireCooldownTime = 0.2f;

    private float fireCooldown;
    private bool isReloading;

    private void Start()
    {
        FillCylinderInstant();
    }

    private void Update()
    {
        UpdateAim();
        HandleInput();
        fireCooldown -= Time.deltaTime;
    }

    // AIM 

    private void UpdateAim()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, new Vector3(0f, aimPlaneHeight, 0f));
        if (!plane.Raycast(ray, out float enter)) return;

        Vector3 aimPoint = ray.GetPoint(enter);
        if (aimMarker != null) aimMarker.position = aimPoint;

        Vector3 dir = aimPoint - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
            transform.forward = dir.normalized;
    }

    // INPUT 

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
            TryFire();

        // R always triggers reload if not already reloading
        if (Input.GetKeyDown(KeyCode.R) && !isReloading)
            StartCoroutine(ReloadRoutine());
    }

    // FIRE

    private void TryFire()
    {
        if (isReloading) return;

        // Empty cylinder - auto reload instead of firing
        if (currentAmmo.Count == 0)
        {
            StartCoroutine(ReloadRoutine());
            return;
        }

        if (fireCooldown > 0f) return;

        fireCooldown = fireCooldownTime;

        BulletSO bullet = currentAmmo[0];
        currentAmmo.RemoveAt(0);

        revolverUI.OnFire();
        Shoot(bullet, transform.forward);
    }

    // RELOAD

    private IEnumerator ReloadRoutine()
    {
        if (availableAmmo.Count == 0) yield break;

        isReloading = true;
        currentAmmo.Clear();

        yield return revolverUI.WaitForAnimation();
        revolverUI.ClearAllSlots();

        for (int i = 0; i < cylinderSize; i++)
        {
            BulletSO bullet = availableAmmo[Random.Range(0, availableAmmo.Count)];
            currentAmmo.Add(bullet);

            revolverUI.OnReloadStep(bullet, i);
            yield return revolverUI.WaitForAnimation();
        }

        isReloading = false;
    }

    // INSTANT FILL (startup)

    private void FillCylinderInstant()
    {
        if (availableAmmo.Count == 0) return;

        currentAmmo.Clear();
        for (int i = 0; i < cylinderSize; i++)
            currentAmmo.Add(availableAmmo[Random.Range(0, availableAmmo.Count)]);

        revolverUI.SyncInstant(currentAmmo);
    }

    // SHOOT

    private void Shoot(BulletSO bullet, Vector3 dir)
    {
        if (bullet == null || bullet.bulletPrefab == null) return;

        GameObject obj = Instantiate(
            bullet.bulletPrefab,
            firePoint.position,
            Quaternion.LookRotation(dir));

        obj.GetComponent<BulletProjectile>()?.Initialize(bullet, dir);
    }
}