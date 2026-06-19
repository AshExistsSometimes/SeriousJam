using UnityEngine;

[CreateAssetMenu(menuName = "SpinToWin/BulletEffects/Burn")]
public class BurnEffect : BulletEffect
{
    public float burnDamage = 2f;
    public float duration = 3f;

    public override void OnHit(GameObject hit, Vector3 point, Vector3 direction)
    {
        Debug.Log(hit.name + " is burning");
    }
}