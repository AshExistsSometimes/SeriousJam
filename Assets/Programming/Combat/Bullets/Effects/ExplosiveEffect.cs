using UnityEngine;

[CreateAssetMenu(menuName = "SpinToWin/BulletEffects/Explosive")]
public class ExplosiveEffect : BulletEffect
{
    public float radius = 3f;
    public float damage = 25f;

    public override void OnHit(GameObject hit, Vector3 point, Vector3 direction)
    {
        Collider[] hits = Physics.OverlapSphere(point, radius);

        foreach (var col in hits)
        {
            // placeholder damage hook
            Debug.Log("Explosion hit: " + col.name);
        }
    }
}
