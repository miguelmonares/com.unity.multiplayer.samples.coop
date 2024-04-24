using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.BossRoom.Gameplay.UI;
using Unity.BossRoom.Gameplay.GameState;
using Playtest;

public class AutoStartGame : MonoBehaviour
{
    private static bool autoStartInitiated = false;

    void Awake()
    {
        if (!autoStartInitiated)
        {
            DontDestroyOnLoad(gameObject);
            autoStartInitiated = true;
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Check if the loaded scene is the main menu or character selection screen
        if (scene.name == "MainMenu") // Replace "MainMenuSceneName" with your main menu scene name
        {
            Invoke("PressHostGameButton", 1f); // Delay to ensure UI is initialized
        }
        else if (scene.name == "CharSelect") // Replace "CharacterSelectionSceneName" with your character selection scene name
        {
            // Here, you would automate character selection and game start
            // This part is highly specific to your game's logic
            Invoke("AutomateCharacterSelectionAndStartGame", 1f); // Delay to ensure UI is initialized
        }
        // Add more conditions if there are more screens to navigate through
    }

    private void PressHostGameButton()
    {
        // Find the IPHostingUI component and call OnCreateClick
        var ipHostingUI = FindObjectOfType<IPHostingUI>();
        if (ipHostingUI != null)
        {
            ipHostingUI.OnCreateClick();
        }
    }

    [PlaytestAction("tessst")]
    private void AutomateCharacterSelectionAndStartGame()
    {
        ClientCharSelectState.Instance.OnPlayerClickedSeat(1);
        ClientCharSelectState.Instance.OnPlayerClickedReady();
    }
}
