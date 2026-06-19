using UnityEngine;

[CreateAssetMenu(menuName = "SpinToWin/BulletEffects/MultiDirection")]
public class MultiDirectionEffect : BulletEffect
{
    public int directionCount = 2;

    public override void OnSpawn(BulletProjectile projectile)
    {
        Vector3 baseDir = projectile.transform.forward;

        float step = 360f / directionCount;

        for (int i = 0; i < directionCount; i++)
        {
            Vector3 dir = Quaternion.Euler(0f, step * i, 0f) * baseDir;

            projectile.transform.forward = dir;
        }
    }

    public override void OnHit(GameObject hit, Vector3 point, Vector3 direction)
    {
        // optional
    }
}