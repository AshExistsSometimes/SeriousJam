using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int CurrentLevel = 1;

    public TMP_Text levelText;

    private void Awake()
    {
        Instance = this;

        NextLevelLogic();
    }

    public void ResetLevelCounter()
    {
        CurrentLevel = 1;
    }

    public void NextLevelLogic()
    {
        levelText.text = ("Level " + CurrentLevel);
    }
}
