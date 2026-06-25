using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "SpinToWin/BulletEffects/MultiDirection")]
public class MultiDirectionEffect : BulletEffect
{
    public int directionCount = 3;
    public float directionSpread = 15f;

    public override void OnSpawn(BulletProjectile projectile)
    {
        projectile.StartCoroutine(Spawn(projectile));
    }

    private IEnumerator Spawn(BulletProjectile projectile)
    {
        // Wait 1 frame so we escape the spawn stack
        yield return null;

        BulletSO data = projectile.GetBulletData();
        if (data == null) yield break;

        Vector3 baseDir = projectile.transform.forward;
        Vector3 origin = projectile.transform.position;

        for (int i = 0; i < directionCount; i++)
        {
            float angle = (i - (directionCount - 1) * 0.5f) * directionSpread;
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * baseDir;

            GameObject obj = Object.Instantiate(
                data.bulletPrefab,
                origin,
                Quaternion.LookRotation(dir)
            );

            obj.GetComponent<BulletProjectile>()
                ?.Initialize(data, dir, projectile.GetHitMask(), false);
        }
    }

    public override void OnHit(GameObject hit, Vector3 point, Vector3 direction) { }
}