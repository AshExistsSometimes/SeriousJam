using UnityEngine;

[CreateAssetMenu(menuName = "SpinToWin/BulletEffects/Burn")]
public class BurnEffect : BulletEffect
{
    public float burnDamage = 2f;
    public int burnTicks = 5;
    public float tickRate = 0.5f;

    public override void OnHit(GameObject hit, Vector3 point, Vector3 direction)
    {
        BaseEnemy enemy = hit.GetComponent<BaseEnemy>();
        if (enemy == null) return;

        enemy.ApplyBurn(burnDamage, burnTicks, tickRate);
    }
}