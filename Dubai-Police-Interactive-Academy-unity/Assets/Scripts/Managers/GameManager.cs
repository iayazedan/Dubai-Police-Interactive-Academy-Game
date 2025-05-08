using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks; // For async scene loading

/// <summary>
/// Defines the different types of training zones available in the academy.
/// </summary>
public enum TrainingZoneType
{
    HoverBike,
    SmartPatrol,
    K9,
    DroneSurveillance,
    CyberThreat,
    EmergencyResponse
}

/// <summary>
/// Represents the current high-level state of the game.
/// </summary>
public enum GameState
{
    MainMenu,
    Hub,
    Loading,
    InTraining,
    Graduation
}

/// <summary>
/// Manages the overall game state, progression, scene loading, and persistence of player data (like earned badges).
/// It acts as the central orchestrator for high-level game flow.
/// </summary>
public class GameManager : MonoBehaviour
{
    #region Singleton
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManager>();
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject("GameManager");
                    _instance = singletonObject.AddComponent<GameManager>();
                    Debug.Log("GameManager instance created.");
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("Duplicate GameManager instance found. Destroying the newer one.");
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeManager();
    }
    #endregion

    #region Properties and Fields
    [Header("Scene Configuration")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string hubSceneName = "AcademyHub";
    [SerializeField] private string graduationSceneName = "GraduationScene";
    [SerializeField] private List<ZoneSceneMapping> trainingZoneScenes;

    [Header("Dependencies")]
    [SerializeField] private UIManager uiManager; // Assign in Inspector or find dynamically

    private GameState _currentState = GameState.MainMenu;
    public GameState CurrentState => _currentState;

    private TrainingZoneType _currentTrainingZone; // Track which zone is being loaded/played
    private Dictionary<TrainingZoneType, bool> _earnedBadges = new Dictionary<TrainingZoneType, bool>();

    // Events for loose coupling
    public static event Action<GameState> OnGameStateChanged;
    public static event Action<TrainingZoneType> OnBadgeEarned;
    #endregion

    #region Initialization and Persistence
    private void InitializeManager()
    {
        // Initialize badge dictionary for all possible zones
        foreach (TrainingZoneType zoneType in Enum.GetValues(typeof(TrainingZoneType)))
        {
            _earnedBadges[zoneType] = false;
        }
        LoadBadges();

        // Attempt to find UIManager if not assigned
        if (uiManager == null)
        {
            uiManager = FindObjectOfType<UIManager>();
            if (uiManager == null)
            {
                Debug.LogError("GameManager could not find UIManager in the scene!");
            }
        }
    }

    private void LoadBadges()
    {
        bool changed = false;
        foreach (TrainingZoneType zoneType in Enum.GetValues(typeof(TrainingZoneType)))
        {
            string key = $"Badge_{zoneType}";
            if (PlayerPrefs.HasKey(key))
            {
                bool earned = PlayerPrefs.GetInt(key, 0) == 1;
                if (_earnedBadges[zoneType] != earned)
                {
                    _earnedBadges[zoneType] = earned;
                    changed = true;
                }
            }
        }
        Debug.Log($"Loaded badge data. Earned count: {GetEarnedBadgeCount()}");
        if (changed)
        {
            // If loaded data differs, potentially notify relevant systems
            // e.g., update badge board if already in Hub
        }
    }

    private void SaveBadges()
    {
        foreach (var kvp in _earnedBadges)
        {
            string key = $"Badge_{kvp.Key}";
            PlayerPrefs.SetInt(key, kvp.Value ? 1 : 0);
        }
        PlayerPrefs.Save();
        Debug.Log("Badge data saved.");
    }
    #endregion

    #region Game Flow Control
    /// <summary>
    /// Starts the game from the main menu, loading the central hub.
    /// </summary>
    public void StartGame()
    {
        LoadHub();
    }

    /// <summary>
    /// Loads the central hub scene.
    /// </summary>
    public async void LoadHub()
    {
        await LoadSceneAsync(hubSceneName, GameState.Hub);
    }

    /// <summary>
    /// Initiates loading of a specific training zone scene.
    /// </summary>
    /// <param name="zone">The type of training zone to load.</param>
    public async void LoadTrainingZone(TrainingZoneType zone)
    {
        string sceneToLoad = GetSceneNameForZone(zone);
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError($"No scene mapping found for TrainingZoneType: {zone}");
            return;
        }

        _currentTrainingZone = zone;
        await LoadSceneAsync(sceneToLoad, GameState.InTraining);
    }

    /// <summary>
    /// Loads the graduation scene. Typically called after all badges are earned.
    /// </summary>
    public async void LoadGraduation()
    {
        await LoadSceneAsync(graduationSceneName, GameState.Graduation);
    }

    /// <summary>
    /// Quits the application.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Quitting application...");
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    /// <summary>
    /// Handles the asynchronous loading of a scene and updates the game state.
    /// </summary>
    /// <param name="sceneName">Name of the scene to load.</param>
    /// <param name="targetState">The game state to transition to after loading.</param>
    private async Task LoadSceneAsync(string sceneName, GameState targetState)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("Attempted to load a scene with an empty name.");
            return;
        }

        SetGameState(GameState.Loading);

        if (uiManager != null)
        {
            uiManager.ShowLoadingScreen(true);
        }
        else {
             Debug.LogWarning("UIManager reference is missing. Cannot show loading screen.");
        }

        try
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            asyncLoad.allowSceneActivation = false; // Prevent activation until ready

            // Wait until the scene is almost loaded
            while (asyncLoad.progress < 0.9f)
            {
                // Update loading progress UI if needed here
                await Task.Yield(); // Wait for the next frame
            }

            // Scene is loaded, allow activation
            asyncLoad.allowSceneActivation = true;

            // Wait for the scene activation to complete
            while (!asyncLoad.isDone)
            {
                await Task.Yield();
            }

            // Scene loaded successfully
            SetGameState(targetState);

            // Need to potentially re-acquire UIManager if it's scene-specific
            if (uiManager == null || uiManager.gameObject.scene.name != sceneName)
            {
                uiManager = FindObjectOfType<UIManager>();
                 if (uiManager == null && targetState != GameState.MainMenu) // Main menu might not have one initially
                 {
                    Debug.LogWarning($"UIManager not found after loading scene: {sceneName}");
                 }
            }


            // Hide loading screen after a short delay or when UI is ready
            if (uiManager != null)
            {
                 // Small delay to ensure scene is fully initialized visually
                 await Task.Delay(100);
                 uiManager.ShowLoadingScreen(false);

                // Update UI based on the new state
                 switch (targetState)
                 {
                    case GameState.Hub:
                         uiManager.ShowHubUI();
                         UpdateBadgeBoardInHub(); // Ensure board reflects current status
                         break;
                    case GameState.InTraining:
                        // MissionManager in the loaded scene should handle its UI via UIManager
                        // Example: uiManager.ShowMissionUI("Training Zone Title");
                        break;
                    case GameState.Graduation:
                         uiManager.ShowEndScreen(GetEarnedBadgeCount());
                         break;
                    case GameState.MainMenu:
                         uiManager.ShowMainMenu();
                         break;
                 }
            }

        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load scene '{sceneName}': {e.Message}");
            // Consider loading a safe fallback scene like the main menu or hub
            SetGameState(GameState.Hub); // Revert to Hub state on failure?
            if (uiManager != null) uiManager.ShowLoadingScreen(false);
        }
    }

     private void SetGameState(GameState newState)
    {
        if (_currentState == newState) return;

        _currentState = newState;
        Debug.Log($"Game state changed to: {newState}");
        OnGameStateChanged?.Invoke(newState);
    }
    #endregion

    #region Mission and Progression
    /// <summary>
    /// Callback received from MissionManager when a training mission concludes.
    /// </summary>
    /// <param name="zone">The zone where the mission took place.</param>
    /// <param name="success">Whether the mission was completed successfully.</param>
    public void MissionCompleted(TrainingZoneType zone, bool success)
    {
        Debug.Log($"Mission completed in zone {zone}. Success: {success}");
        if (success)
        {
            EarnBadge(zone);
        }

        // Always return to the Hub after a mission attempt
        LoadHub();
    }

    /// <summary>
    /// Awards a badge for the specified training zone.
    /// </summary>
    /// <param name="zone">The zone for which the badge is earned.</param>
    public void EarnBadge(TrainingZoneType zone)
    {
        if (!_earnedBadges.ContainsKey(zone))
        {
             Debug.LogWarning($"Attempted to earn badge for invalid zone: {zone}");
             return;
        }

        if (!_earnedBadges[zone])
        {
            _earnedBadges[zone] = true;
            Debug.Log($"Badge earned for zone: {zone}");
            SaveBadges(); // Persist the new badge immediately
            OnBadgeEarned?.Invoke(zone);
            UpdateBadgeBoardInHub(); // Update visual display if currently in Hub
            CheckGraduationStatus(); // Check if graduation is now possible
        }
        else
        {
            Debug.Log($"Badge for zone {zone} was already earned.");
        }
    }

    /// <summary>
    /// Checks if the player has earned the badge for a specific zone.
    /// </summary>
    /// <param name="zone">The zone to check.</param>
    /// <returns>True if the badge is earned, false otherwise.</returns>
    public bool HasBadge(TrainingZoneType zone)
    {
        return _earnedBadges.TryGetValue(zone, out bool earned) && earned;
    }

    /// <summary>
    /// Gets the total number of badges earned by the player.
    /// </summary>
    /// <returns>The count of earned badges.</returns>
    public int GetEarnedBadgeCount()
    {
        return _earnedBadges.Count(kvp => kvp.Value);
    }

    /// <summary>
    /// Checks if the player has earned all badges and might trigger graduation readiness.
    /// </summary>
    public void CheckGraduationStatus()
    {
        int totalPossibleBadges = Enum.GetValues(typeof(TrainingZoneType)).Length;
        int earnedCount = GetEarnedBadgeCount();

        Debug.Log($"Checking graduation status: {earnedCount}/{totalPossibleBadges} badges earned.");

        if (earnedCount >= totalPossibleBadges)
        {
            Debug.Log("All badges earned! Graduation is possible.");
            // Optionally, trigger a UI notification or enable interaction for graduation
            // For now, we just log it. Graduation can be triggered explicitly via LoadGraduation().
        }
    }

     /// <summary>
    /// Helper method to update the Badge Display Board if the player is currently in the Hub scene.
    /// </summary>
    private void UpdateBadgeBoardInHub()
    {
        if (CurrentState == GameState.Hub)
        {
            BadgeDisplayBoard board = FindObjectOfType<BadgeDisplayBoard>();
            if (board != null)
            {
                bool[] earnedStatus = Enum.GetValues(typeof(TrainingZoneType))
                                          .Cast<TrainingZoneType>()
                                          .Select(zone => HasBadge(zone))
                                          .ToArray();
                board.UpdateDisplay(earnedStatus);
            }
            else
            {
                 Debug.LogWarning("Could not find BadgeDisplayBoard in the Hub scene to update.");
            }

             // Also update UIManager's representation if it has one
            if (uiManager != null)
            {
                 bool[] earnedStatus = Enum.GetValues(typeof(TrainingZoneType))
                                          .Cast<TrainingZoneType>()
                                          .Select(zone => HasBadge(zone))
                                          .ToArray();
                // Assuming UIManager has a method like this; adjust if needed
                 uiManager.UpdateBadgeBoard(earnedStatus);
            }
        }
    }
    #endregion

    #region Utility Methods
    /// <summary>
    /// Gets the scene name associated with a specific training zone type.
    /// </summary>
    /// <param name="zone">The training zone type.</param>
    /// <returns>The scene name, or null if not found.</returns>
    private string GetSceneNameForZone(TrainingZoneType zone)
    {
        foreach (var mapping in trainingZoneScenes)
        {
            if (mapping.zoneType == zone)
            {
                return mapping.sceneName;
            }
        }
        return null; // Not found
    }
    #endregion

    #region Scene Mapping Struct
    /// <summary>
    /// Helper struct to map TrainingZoneType enums to scene names in the Inspector.
    /// </summary>
    [Serializable]
    public struct ZoneSceneMapping
    {
        public TrainingZoneType zoneType;
        public string sceneName; // Use string for flexibility, ensure these scenes are in Build Settings
    }
    #endregion

     #if UNITY_EDITOR
     // Optional: Add a context menu item for easy testing/resetting
     [UnityEditor.MenuItem("Dubai Police Academy/Reset Player Badges")]
     private static void ResetBadgesEditor()
     {
         Debug.Log("Resetting all player badges (Editor Only)...");
         foreach (TrainingZoneType zoneType in Enum.GetValues(typeof(TrainingZoneType)))
         {
             string key = $"Badge_{zoneType}";
             PlayerPrefs.DeleteKey(key);
         }
         PlayerPrefs.Save();
         Debug.Log("Badges reset. Restart the game for changes to fully apply if running.");
         // If GameManager instance exists, update its state too
         if (_instance != null)
         {
             _instance.InitializeManager(); // Re-initialize to clear loaded badges
              _instance.UpdateBadgeBoardInHub(); // Update board if in hub
         }
     }
     #endif
}