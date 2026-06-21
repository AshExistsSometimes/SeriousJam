using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles main menu button functionality.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string GameplaySceneName = "Level";

    /// <summary>
    /// Loads the gameplay scene.
    /// </summary>
    public void PlayGame()
    {
        SceneManager.LoadScene(GameplaySceneName);
    }

    /// <summary>
    /// Closes the application.
    /// </summary>
    public void QuitToDesktop()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}