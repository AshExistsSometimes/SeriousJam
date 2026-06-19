using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int CurrentLevel = 1;

    private void Awake()
    {
        Instance = this;
    }

    public void ResetLevelCounter()
    {
        CurrentLevel = 1;
    }
}
