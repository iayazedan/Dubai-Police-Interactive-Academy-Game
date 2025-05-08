// Required Packages:
// - com.unity.ui
// - com.unity.textmeshpro

using UnityEngine;
using UnityEngine.UI; // Required for basic UI elements like Image, Button
using TMPro; // Required for TextMeshPro elements
using System;
using System.Collections.Generic; // Required for List
using System.Linq; // Required for Linq operations like Cast, Select, ToArray

// Adjust this based on the actual implementation in MissionManager.cs.
[System.Serializable]
public class MissionObjective // Ensure consistency with MissionManager
{
    public string objectiveId; // Unique ID for the objective
    public string description;
    public float currentProgress; // Value between 0 and 1, or current count
    public float requiredProgress; // Target value for completion
    public bool isCompleted;
}


/// <summary>
/// Controls the visibility and content of all UI panels based on game state and events.
/// Manages UI elements for Main Menu, Loading, Hub, Mission Objectives, Badge display (UI version), and End Screen.
/// </summary>
public class UIManager : MonoBehaviour
{
    #region Singleton
    private static UIManager _instance;
    public static UIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<UIManager>();
                if (_instance == null)
                {
                    Debug.LogError("UIManager instance not found in the scene. Please ensure a UIManager component exists.");
                }
            }
            return _instance;
        }
    }
    #endregion

    #region UI Panel References
    [Header("Core UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject loadingScreenPanel;
    [SerializeField] private GameObject hubUIPanel;
    [SerializeField] private GameObject missionUIPanel;
    [SerializeField] private GameObject endScreenPanel;

    [Header("Mission UI Elements")]
    [SerializeField] private TextMeshProUGUI missionTitleText;
    [SerializeField] private RectTransform missionObjectivesContainer;
    [SerializeField] private GameObject missionObjectivePrefab; // Prefab for displaying a single objective

    [Header("Hub UI Elements")]
    [SerializeField] private GameObject badgeBoardUIParent; // Parent object holding UI badge representations
    [SerializeField] private Image[] uiBadgeIcons; // UI Images representing earned badges (ensure order matches TrainingZoneType)
    [SerializeField] private Sprite earnedBadgeSprite; // Sprite/Image for an earned badge
    [SerializeField] private Sprite unearnedBadgeSprite; // Sprite/Image for an unearned badge

    [Header("End Screen Elements")]
    [SerializeField] private TextMeshProUGUI endScreenBadgeCountText;

    // Internal list to manage instantiated objective UI elements
    private List<GameObject> _instantiatedObjectives = new List<GameObject>();
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // Singleton pattern enforcement
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("Duplicate UIManager instance found. Destroying the newer one.");
            Destroy(gameObject);
            return;
        }
        _instance = this;

        // Ensure critical references are set
        ValidateReferences();

        // Subscribe to GameManager events for automatic UI updates based on state
        // Ensure GameManager exists before subscribing
        if (GameManager.Instance != null)
        {
             GameManager.OnGameStateChanged += HandleGameStateChanged;
             // Initialize UI state based on current GameManager state
             HandleGameStateChanged(GameManager.Instance.CurrentState);
        }
         else
         {
              // If GameManager isn't ready, default to hiding all panels or showing a specific one
              HideAllPanels();
              // Or potentially show main menu if this UIManager is part of the initial scene
              if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
              Debug.LogWarning("UIManager Awake: GameManager instance not found. Initial UI state might be incomplete and event subscription failed.");
         }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        // Check if GameManager instance still exists before trying to unsubscribe
        if (GameManager.Instance != null)
        {
             GameManager.OnGameStateChanged -= HandleGameStateChanged;
        }


        if (_instance == this)
        {
            _instance = null;
        }
    }
    #endregion

    #region Panel Management
    /// <summary>
    /// Deactivates all major UI panels managed by this UIManager.
    /// </summary>
    private void HideAllPanels()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (hubUIPanel != null) hubUIPanel.SetActive(false);
        if (missionUIPanel != null) missionUIPanel.SetActive(false);
        if (endScreenPanel != null) endScreenPanel.SetActive(false);
        // Loading screen is handled separately by ShowLoadingScreen
    }

    /// <summary>
    /// Activates the Main Menu UI panel and deactivates others.
    /// </summary>
    public void ShowMainMenu()
    {
        HideAllPanels();
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("Main Menu Panel reference is not set in UIManager.");
        }
    }

    /// <summary>
    /// Activates the Hub UI panel and deactivates others.
    /// Optionally updates the badge display within the Hub UI.
    /// </summary>
    public void ShowHubUI()
    {
        HideAllPanels();
        if (hubUIPanel != null)
        {
            hubUIPanel.SetActive(true);
            // Optionally refresh badge display when showing Hub UI
             if (GameManager.Instance != null)
             {
                 // Ensure TrainingZoneType enum exists and is accessible
                 try
                 {
                     bool[] earnedStatus = System.Enum.GetValues(typeof(TrainingZoneType))
                                                 .Cast<TrainingZoneType>()
                                                 .Select(zone => GameManager.Instance.HasBadge(zone))
                                                 .ToArray();
                     UpdateBadgeBoard(earnedStatus);
                 }
                 catch (System.Exception ex)
                 {
                     Debug.LogError($"Error getting badge status for Hub UI: {ex.Message}. Is TrainingZoneType defined?");
                 }
             }
             else
             {
                 Debug.LogWarning("GameManager instance not available to update badge board in Hub UI.");
             }

        }
        else
        {
            Debug.LogError("Hub UI Panel reference is not set in UIManager.");
        }
    }

    /// <summary>
    /// Activates the Mission UI panel, sets the mission title, and deactivates others.
    /// </summary>
    /// <param name="missionTitle">The title to display for the current mission.</param>
    public void ShowMissionUI(string missionTitle)
    {
        HideAllPanels();
        if (missionUIPanel != null)
        {
            missionUIPanel.SetActive(true);
            if (missionTitleText != null)
            {
                missionTitleText.text = missionTitle;
            }
            else
            {
                Debug.LogWarning("Mission Title Text reference is not set in UIManager.");
            }
            // Clear previous objectives display
            ClearObjectiveList();
        }
        else
        {
            Debug.LogError("Mission UI Panel reference is not set in UIManager.");
        }
    }

    /// <summary>
    /// Activates or deactivates the Loading Screen UI panel.
    /// </summary>
    /// <param name="show">True to show the loading screen, false to hide it.</param>
    public void ShowLoadingScreen(bool show)
    {
        if (loadingScreenPanel != null)
        {
            // Don't hide other panels when showing loading screen
            loadingScreenPanel.SetActive(show);
        }
        else
        {
            Debug.LogError("Loading Screen Panel reference is not set in UIManager.");
        }
    }

    /// <summary>
    /// Activates the End Screen UI panel, displays the final badge count, and deactivates others.
    /// </summary>
    /// <param name="badgeCount">The total number of badges earned by the player.</param>
    public void ShowEndScreen(int badgeCount)
    {
        HideAllPanels();
        if (endScreenPanel != null)
        {
            endScreenPanel.SetActive(true);
            if (endScreenBadgeCountText != null)
            {
                int totalBadges = 0;
                // Ensure TrainingZoneType enum exists and is accessible
                try
                {
                    totalBadges = System.Enum.GetValues(typeof(TrainingZoneType)).Length;
                    endScreenBadgeCountText.text = $"Graduation Complete!\nBadges Earned: {badgeCount} / {totalBadges}";
                }
                catch(System.Exception ex)
                {
                    Debug.LogError($"Error getting total badge count for End Screen: {ex.Message}. Is TrainingZoneType defined?");
                    endScreenBadgeCountText.text = $"Graduation Complete!\nBadges Earned: {badgeCount}"; // Fallback text
                }
            }
            else
            {
                Debug.LogWarning("End Screen Badge Count Text reference is not set in UIManager.");
            }
        }
        else
        {
            Debug.LogError("End Screen Panel reference is not set in UIManager.");
        }
    }
    #endregion

    #region UI Content Updates
    /// <summary>
    /// Updates the Mission UI's objective list based on the provided data.
    /// </summary>
    /// <param name="objectives">An array of MissionObjective data.</param>
    public void UpdateMissionObjectives(MissionObjective[] objectives)
    {
        if (missionUIPanel == null || !missionUIPanel.activeInHierarchy)
        {
             // Don't update if the panel isn't visible or doesn't exist
             return;
        }

        if (missionObjectivesContainer == null || missionObjectivePrefab == null)
        {
            Debug.LogError("Mission Objectives Container or Prefab reference is not set in UIManager.");
            return;
        }

        if (objectives == null)
        {
            Debug.LogWarning("UpdateMissionObjectives called with null objectives array.");
            ClearObjectiveList(); // Clear existing if objectives are now null
            return;
        }


        // Clear existing objective UI elements
        ClearObjectiveList();

        // Instantiate and populate new objective UI elements
        foreach (var objective in objectives)
        {
            if (objective == null) // Add null check for individual objectives if using class
            {
                Debug.LogWarning("Encountered a null objective in the objectives array.");
                continue;
            }

            GameObject objectiveInstance = Instantiate(missionObjectivePrefab, missionObjectivesContainer);
            TextMeshProUGUI objectiveText = objectiveInstance.GetComponentInChildren<TextMeshProUGUI>(); // Find text component within prefab

            if (objectiveText != null)
            {
                string progressText = "";
                // Basic progress display, can be customized
                if (objective.requiredProgress > 0) // Avoid division by zero or meaningless progress display
                {
                    // Use FloorToInt for count-based objectives, or format differently for 0-1 progress
                    progressText = $" ({Mathf.FloorToInt(objective.currentProgress)}/{Mathf.FloorToInt(objective.requiredProgress)})";
                    // Example for 0-1 progress display:
                    // progressText = $" ({(objective.currentProgress * 100):F0}%)";
                }

                string statusMarker = objective.isCompleted ? "<color=green>[DONE]</color> " : "[ ] "; // Use rich text for color
                objectiveText.text = $"{statusMarker}{objective.description}{progressText}";

                 // Optional: Change color or style for completed objectives using properties if needed
                 // objectiveText.color = objective.isCompleted ? Color.green : Color.white; // Alternative to rich text
            }
            else
            {
                Debug.LogWarning($"Mission Objective Prefab '{missionObjectivePrefab.name}' does not contain a TextMeshProUGUI component in its children.");
            }

            _instantiatedObjectives.Add(objectiveInstance);
            objectiveInstance.SetActive(true); // Ensure prefab is active if disabled by default
        }
    }

     /// <summary>
    /// Clears previously instantiated objective UI elements from the list.
    /// </summary>
    private void ClearObjectiveList()
    {
        foreach (var objUI in _instantiatedObjectives)
        {
            if (objUI != null) // Add null check before destroying
            {
                Destroy(objUI);
            }
        }
        _instantiatedObjectives.Clear();
    }


    /// <summary>
    /// Updates the visual representation of earned badges within the UI (e.g., icons on the Hub panel).
    /// </summary>
    /// <param name="earnedBadges">A boolean array indicating the earned status for each badge.</param>
    public void UpdateBadgeBoard(bool[] earnedBadges)
    {
        if (uiBadgeIcons == null || uiBadgeIcons.Length == 0)
        {
             // Only warn if the parent is active, otherwise it might be expected
             if(badgeBoardUIParent != null && badgeBoardUIParent.activeInHierarchy)
                Debug.LogWarning("UI Badge Icons array is not set or empty in UIManager, cannot update UI badge display.");
            return;
        }
         if (earnedBadgeSprite == null || unearnedBadgeSprite == null)
         {
             Debug.LogError("Earned or Unearned Badge Sprite reference is not set in UIManager.");
             return; // Cannot update without sprites
         }

        int expectedBadgeCount = 0;
        try
        {
             expectedBadgeCount = System.Enum.GetValues(typeof(TrainingZoneType)).Length;
        }
        catch (System.Exception ex)
        {
             Debug.LogError($"Error getting TrainingZoneType count for badge board: {ex.Message}. Is TrainingZoneType defined?");
             return; // Cannot proceed without knowing the expected count
        }


        if (earnedBadges == null) {
             Debug.LogError("UpdateBadgeBoard called with null earnedBadges array.");
             return;
        }

        if (earnedBadges.Length != expectedBadgeCount || uiBadgeIcons.Length != expectedBadgeCount)
        {
            Debug.LogError($"Mismatch in badge counts. Expected: {expectedBadgeCount}, Input Array: {earnedBadges.Length}, UI Icons: {uiBadgeIcons.Length}. Ensure arrays match TrainingZoneType enum size.");
            // Attempt to update up to the minimum length to avoid index out of bounds
            // return; // Or proceed cautiously
        }

        int count = Mathf.Min(earnedBadges.Length, uiBadgeIcons.Length); // Use the smaller count to prevent errors

        for (int i = 0; i < count; i++)
        {
            if (uiBadgeIcons[i] != null)
            {
                // Update Sprite based on earned status
                uiBadgeIcons[i].sprite = earnedBadges[i] ? earnedBadgeSprite : unearnedBadgeSprite;

                // Optional: Update color or other visual properties
                uiBadgeIcons[i].color = earnedBadges[i] ? Color.white : new Color(0.7f, 0.7f, 0.7f, 0.8f); // Example: Dim unearned badges
            }
            else
            {
                Debug.LogWarning($"UI Badge Icon at index {i} is null in the UIManager.");
            }
        }
    }
    #endregion

    #region Event Handlers
    /// <summary>
    /// Handles changes in the game state received from the GameManager.
    /// Activates the appropriate UI panel for the new state.
    /// </summary>
    /// <param name="newState">The new GameState.</param>
    private void HandleGameStateChanged(GameState newState)
    {
        // Hide loading screen unless the state is Loading
        if (newState != GameState.Loading && loadingScreenPanel != null && loadingScreenPanel.activeInHierarchy)
        {
            ShowLoadingScreen(false);
        }

        switch (newState)
        {
            case GameState.MainMenu:
                ShowMainMenu();
                break;
            case GameState.Hub:
                ShowHubUI();
                break;
            case GameState.InTraining:
                // MissionManager is expected to call ShowMissionUI when ready
                // Hide Hub UI explicitly if it shouldn't persist
                 if (hubUIPanel != null && hubUIPanel.activeInHierarchy) hubUIPanel.SetActive(false);
                 // Optionally show a generic "Mission Active" state or hide everything until MissionManager updates
                 HideAllPanels(); // Hide everything, MissionManager will show MissionUI later
                break;
            case GameState.Loading:
                // Ensure other panels are hidden before showing loading, except potentially persistent UI
                 // HideAllPanels(); // Optional: Decide if loading screen should obscure everything
                ShowLoadingScreen(true);
                break;
            case GameState.Graduation:
                 if(GameManager.Instance != null)
                 {
                    ShowEndScreen(GameManager.Instance.GetEarnedBadgeCount());
                 } else {
                     ShowEndScreen(0); // Show end screen, but count might be unavailable
                     Debug.LogWarning("Cannot get final badge count from GameManager during Graduation state change.");
                 }
                break;
            default:
                Debug.LogWarning($"UIManager received unhandled GameState: {newState}");
                HideAllPanels(); // Fallback: hide everything
                break;
        }
    }
    #endregion

    #region Validation
    /// <summary>
    /// Checks if essential UI panel references are assigned in the Inspector.
    /// </summary>
    private void ValidateReferences()
    {
        if (mainMenuPanel == null) Debug.LogWarning("UIManager: Main Menu Panel reference not set.");
        if (loadingScreenPanel == null) Debug.LogWarning("UIManager: Loading Screen Panel reference not set.");
        if (hubUIPanel == null) Debug.LogWarning("UIManager: Hub UI Panel reference not set.");
        if (missionUIPanel == null) Debug.LogWarning("UIManager: Mission UI Panel reference not set.");
        if (endScreenPanel == null) Debug.LogWarning("UIManager: End Screen Panel reference not set.");

        // Validate children only if parent exists
        if (missionUIPanel != null)
        {
             if (missionTitleText == null) Debug.LogWarning("UIManager: Mission Title Text reference not set.");
             if (missionObjectivesContainer == null) Debug.LogWarning("UIManager: Mission Objectives Container reference not set.");
             if (missionObjectivePrefab == null) Debug.LogWarning("UIManager: Mission Objective Prefab reference not set.");
             else if (missionObjectivePrefab.GetComponentInChildren<TextMeshProUGUI>() == null) Debug.LogWarning($"UIManager: Mission Objective Prefab '{missionObjectivePrefab.name}' is missing a TextMeshProUGUI component in its children.");
        }
         if (hubUIPanel != null)
        {
            if (badgeBoardUIParent == null) Debug.LogWarning("UIManager: Badge Board UI Parent reference not set.");
            else if (uiBadgeIcons == null || uiBadgeIcons.Length == 0) Debug.LogWarning("UIManager: UI Badge Icons array is not set or empty.");
            // Check sprites only if icons array is valid and non-empty
            if (uiBadgeIcons != null && uiBadgeIcons.Length > 0) {
                 if(earnedBadgeSprite == null) Debug.LogWarning("UIManager: Earned Badge Sprite reference not set.");
                 if(unearnedBadgeSprite == null) Debug.LogWarning("UIManager: Unearned Badge Sprite reference not set.");
                 // Check individual icon references
                 for(int i = 0; i < uiBadgeIcons.Length; ++i) {
                     if (uiBadgeIcons[i] == null) Debug.LogWarning($"UIManager: UI Badge Icon at index {i} is not assigned.");
                 }
            }
        }
         if (endScreenPanel != null)
        {
             if (endScreenBadgeCountText == null) Debug.LogWarning("UIManager: End Screen Badge Count Text reference not set.");
        }
    }
    #endregion

    // Placeholder types assumed to exist elsewhere (e.g., GameManager.cs, shared definitions)
    // Ensure these match the actual project definitions.
    #region Placeholder Types (Remove or replace with actual definitions)
    public enum GameState { MainMenu, Loading, Hub, InTraining, Graduation }
    public enum TrainingZoneType { Zone1, Zone2, Zone3 } // Example, replace with actual zones
    // public class GameManager { // Minimal definition for compilation
    //    public static GameManager Instance;
    //    public GameState CurrentState;
    //    public static event System.Action<GameState> OnGameStateChanged;
    //    public bool HasBadge(TrainingZoneType zone) { return false; }
    //    public int GetEarnedBadgeCount() { return 0; }
    //}
    #endregion
}