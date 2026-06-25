using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "SpinToWin/BulletEffects/No Effect")]
public class NoEffect : BulletEffect
{
    public override void OnHit(GameObject hit, Vector3 point, Vector3 direction)
    {

    }
}
