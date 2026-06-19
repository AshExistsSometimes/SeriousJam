using UnityEngine;

[CreateAssetMenu(menuName = "SpinToWin/BulletEffects/Freeze")]
public class FreezeEffect : BulletEffect
{
    public float slowAmount = 0.5f;
    public float duration = 2f;

    public override void OnHit(GameObject hit, Vector3 point, Vector3 direction)
    {
        Debug.Log(hit.name + " is frozen");
    }
}
