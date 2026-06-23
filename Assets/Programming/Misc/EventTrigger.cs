using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EventTrigger : MonoBehaviour
{
    public UnityEvent TriggerEntered;

    public LevelManager levelManager;

    public bool LoadNextLevelOntrigger = false;

    private void Awake()
    {
        levelManager = FindFirstObjectByType<LevelManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            TriggerEntered.Invoke();

            if (LoadNextLevelOntrigger)
            {
                levelManager.ProgressToNextLevel();
            }           
        }
    }
}
