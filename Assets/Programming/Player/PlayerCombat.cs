using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    public bool CombatPaused;

    [Header("References")]
    public Transform firePoint;
    public Transform aimMarker;
    public Camera cam;
    public RevolverUI revolverUI;
    public PlayerModelSpin modelSpin;
    public PlayerMovement playerMovement;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [Space]
    [SerializeField] private AudioClip GunshotSFX;// can be set by bullet later
    private AudioClip DefaultGunshotAudio;
    public Vector2 GunPitchVariation = new Vector2(0.95f, 1.05f);

    [Header("Aim")]
    public float aimPlaneHeight = 0f;

    [Header("Ammo")]
    public List<BulletSO> AmmoPool = new();
    public List<BulletSO> currentAmmo = new();
    public int cylinderSize = 6;

    [Header("Fire")]
    public float fireCooldownTime = 0.2f;

    [Header("Spin Dash")]
    public float spinDashCooldown = 1f;
    [Tooltip("Radius around player that damages enemies during dash")]
    public float dashAttackRadius = 1.5f;
    [Tooltip("Damage dealt to each enemy hit during dash")]
    public float dashDamage = 25f;
    [Tooltip("Speed enemies are knocked away from the player")]
    public float dashKnockbackSpeed = 12f;
    [Tooltip("Layer mask for enemies so the overlap sphere only hits them")]
    public LayerMask enemyLayer;

    [Header("Bullet LayerMask")]
    [Tooltip("Set to Enemy + Wall layers so bullets collide correctly")]
    public LayerMask bulletHitMask;

    private float fireCooldown;
    private float dashCooldownTimer;
    private bool isReloading;
    private bool isSpinDashing;

    // Tracks which enemies have already been hit this dash so we don't
    // damage the same enemy multiple times per dash
    private HashSet<BaseEnemy> hitThisDash = new();

    private void Start()
    {
        FillCylinderInstant();

        DefaultGunshotAudio = GunshotSFX;
    }

    private void Update()
    {
        if (CombatPaused)
            return;


        UpdateAim();
        HandleInput();
        fireCooldown -= Time.deltaTime;
        dashCooldownTimer -= Time.deltaTime;

        if (isSpinDashing)
            CheckDashHits();
    }

    // ?? AIM ???????????????????????????????????????????????????????????????????

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

    // ?? INPUT ?????????????????????????????????????????????????????????????????

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
            TryFire();

        if (Input.GetKeyDown(KeyCode.R) && !isReloading && !isSpinDashing)
            StartCoroutine(ReloadRoutine());

        bool dashPressed = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.LeftShift);
        if (dashPressed && !isSpinDashing && dashCooldownTimer <= 0f)
            StartCoroutine(SpinDashRoutine());
    }

    // ?? FIRE ??????????????????????????????????????????????????????????????????

    private void TryFire()
    {
        if (isReloading || isSpinDashing) return;

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

    // ?? SPIN DASH ?????????????????????????????????????????????????????????????

    private IEnumerator SpinDashRoutine()
    {
        isSpinDashing = true;
        dashCooldownTimer = spinDashCooldown;
        hitThisDash.Clear();

        Vector3 dashDir = transform.forward;
        dashDir.y = 0f;
        dashDir.Normalize();

        modelSpin?.TriggerSpin();
        playerMovement?.StartDash(dashDir);

        if (modelSpin != null)
            yield return modelSpin.WaitForSpin();
        else
            yield return new WaitForSeconds(0.6f);

        isSpinDashing = false;
        hitThisDash.Clear();
    }

    // Runs every frame while dashing — hits any enemy inside dashAttackRadius
    private void CheckDashHits()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, dashAttackRadius, enemyLayer);

        foreach (Collider col in hits)
        {
            BaseEnemy enemy = col.GetComponent<BaseEnemy>();
            if (enemy == null || hitThisDash.Contains(enemy)) continue;

            hitThisDash.Add(enemy);

            // Damage
            enemy.TakeDamage(dashDamage);

            // Knockback away from player
            Vector3 knockDir = (col.transform.position - transform.position);
            knockDir.y = 0f;
            knockDir.Normalize();
            enemy.ApplyKnockback(knockDir, dashKnockbackSpeed);
        }
    }

    // ?? RELOAD ????????????????????????????????????????????????????????????????

    private IEnumerator ReloadRoutine()
    {
        if (AmmoPool.Count == 0) yield break;

        isReloading = true;
        currentAmmo.Clear();

        yield return revolverUI.WaitForAnimation();
        revolverUI.ClearAllSlots();

        for (int i = 0; i < cylinderSize; i++)
        {
            BulletSO bullet = AmmoPool[Random.Range(0, AmmoPool.Count)];
            currentAmmo.Add(bullet);

            revolverUI.OnReloadStep(bullet, i);
            yield return revolverUI.WaitForAnimation();
        }

        isReloading = false;
    }

    // ?? INSTANT FILL (startup) ????????????????????????????????????????????????

    private void FillCylinderInstant()
    {
        if (AmmoPool.Count == 0) return;

        currentAmmo.Clear();
        for (int i = 0; i < cylinderSize; i++)
            currentAmmo.Add(AmmoPool[Random.Range(0, AmmoPool.Count)]);

        revolverUI.SyncInstant(currentAmmo);
    }

    // ?? SHOOT ?????????????????????????????????????????????????????????????????

    private void Shoot(BulletSO bullet, Vector3 dir)
    {
        if (bullet == null || bullet.bulletPrefab == null) return;

        GameObject obj = Instantiate(
            bullet.bulletPrefab,
            firePoint.position,
            Quaternion.LookRotation(dir));

        obj.GetComponent<BulletProjectile>()?.Initialize(bullet, dir, bulletHitMask, true);

        if (bullet.BulletSFX == null)
        {
            GunshotSFX = DefaultGunshotAudio;
        }
        else
        {
            GunshotSFX = bullet.BulletSFX;
        }

            audioSource.pitch = Random.Range(GunPitchVariation.x, GunPitchVariation.y);
            audioSource.PlayOneShot(GunshotSFX);
    }

    // ?? GIZMOS ????????????????????????????????????????????????????????????????

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, dashAttackRadius);
    }
}