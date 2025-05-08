using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq; // Required for LINQ operations like Where and All

[System.Serializable]
public struct MissionObjective // Match UIManager's definition (struct)
{
    public string objectiveId; // Unique ID for the objective
    public string description; // Text for UI display
    public float requiredProgress = 1f; // Target value for completion (e.g., 1 for boolean, >1 for count)
    public bool isOptional = false; // If true, mission can succeed even if this isn't completed

    [HideInInspector] public float currentProgress; // Initialized by Reset
    [HideInInspector] public bool isCompleted; // Initialized by Reset

    // Reset state for mission start
    public void Reset()
    {
        currentProgress = 0f;
        isCompleted = false;
    }
}

/// <summary>
/// Manages the specific objectives and state for the active training mission within a zone.
/// Tracks progress, checks completion conditions, and reports the final result back to the GameManager.
/// </summary>
public class MissionManager : MonoBehaviour
{
    [Header("Mission Configuration")]
    [SerializeField] private TrainingZoneType trainingZoneType; // Identifies this mission zone
    [SerializeField] private string missionTitle = "Training Mission";
    [Tooltip("List of objectives for this mission.")]
    [SerializeField] private List<MissionObjective> missionObjectives = new List<MissionObjective>();

    private enum MissionState
    {
        NotStarted,
        InProgress,
        Succeeded,
        Failed
    }
    private MissionState currentState = MissionState.NotStarted;

    // Cached references
    private UIManager uiManager;
    private GameManager gameManager;

    private void Start()
    {
        // Cache singleton references for efficiency and null checking
        uiManager = UIManager.Instance;
        gameManager = GameManager.Instance;

        if (uiManager == null)
        {
            Debug.LogError($"MissionManager ({gameObject.name}): UIManager instance not found in the scene! UI updates will fail.");
        }
        if (gameManager == null)
        {
            Debug.LogError($"MissionManager ({gameObject.name}): GameManager instance not found! Cannot report mission completion.");
        }
    }

    /// <summary>
    /// Initializes and starts the mission. Resets objectives and updates the UI.
    /// Should be called when the player begins the training exercise for this zone.
    /// </summary>
    public void StartMission()
    {
        if (currentState != MissionState.NotStarted)
        {
            Debug.LogWarning($"MissionManager ({gameObject.name}): Attempting to start mission '{missionTitle}' which is already {currentState}.");
            return;
        }
         if (missionObjectives == null || missionObjectives.Count == 0)
        {
            Debug.LogError($"MissionManager ({gameObject.name}): Cannot start mission '{missionTitle}', no objectives are defined. Please configure objectives in the Inspector.");
            return;
        }
        if (gameManager == null || uiManager == null)
        {
             Debug.LogError($"MissionManager ({gameObject.name}): Cannot start mission '{missionTitle}' due to missing GameManager or UIManager references.");
             return;
        }


        Debug.Log($"MissionManager ({gameObject.name}): Starting mission '{missionTitle}' for zone {trainingZoneType}.");
        currentState = MissionState.InProgress;

        // Reset state of all objectives using index access for structs
        for (int i = 0; i < missionObjectives.Count; i++)
        {
            MissionObjective currentObjective = missionObjectives[i]; // Get a copy
            currentObjective.Reset(); // Modify the copy
            missionObjectives[i] = currentObjective; // Write the modified copy back
        }

        // Initialize UI
        uiManager.ShowMissionUI(missionTitle);
        UpdateUIObjectives(); // Display initial state
    }

    /// <summary>
    /// Updates the progress for a specific mission objective based on its ID.
    /// Clamps progress between 0 and the required value. Automatically completes the objective if progress reaches the requirement.
    /// </summary>
    /// <param name="objectiveId">The unique identifier of the objective to update.</param>
    /// <param name="progress">The new absolute progress value.</param>
    public void UpdateObjectiveProgress(string objectiveId, float progress)
    {
        if (currentState != MissionState.InProgress)
        {
            return;
        }

        int objectiveIndex = FindObjectiveIndexById(objectiveId);
        if (objectiveIndex == -1)
        {
            Debug.LogWarning($"MissionManager ({gameObject.name}): Objective with ID '{objectiveId}' not found. Cannot update progress.");
            return;
        }

        // Get a copy to check/modify
        MissionObjective objective = missionObjectives[objectiveIndex];

        if (objective.isCompleted)
        {
            return; // Ignore updates for already completed objectives
        }

        // Update progress on the copy first to calculate clamped value
        objective.currentProgress = Mathf.Clamp(progress, 0f, objective.requiredProgress);

        // Check completion based on the copy's potential new state
        bool completesObjective = objective.currentProgress >= objective.requiredProgress;

        // Write the modified copy back to the list BEFORE potentially calling ObjectiveCompleted
        missionObjectives[objectiveIndex] = objective;

        // If this update completes the objective, call ObjectiveCompleted
        if (completesObjective)
        {
            // ObjectiveCompleted will find the index again and set isCompleted = true, then update UI/CheckMission.
            ObjectiveCompleted(objectiveId);
        }
        else
        {
            // If the objective wasn't completed by this update, just refresh the UI
            UpdateUIObjectives();
        }
    }


    /// <summary>
    /// Explicitly marks a specific objective as completed by its ID.
    /// Ensures progress is set to maximum and triggers UI update and mission completion checks.
    /// </summary>
    /// <param name="objectiveId">The unique identifier of the objective to mark as complete.</param>
    public void ObjectiveCompleted(string objectiveId)
    {
        if (currentState != MissionState.InProgress)
        {
            return;
        }

        int objectiveIndex = FindObjectiveIndexById(objectiveId);
        if (objectiveIndex == -1)
        {
            Debug.LogWarning($"MissionManager ({gameObject.name}): Objective with ID '{objectiveId}' not found. Cannot mark as complete.");
            return;
        }

        // Get a copy to check/modify
        MissionObjective objective = missionObjectives[objectiveIndex];

        if (!objective.isCompleted)
        {
            objective.isCompleted = true;
            objective.currentProgress = objective.requiredProgress;
            Debug.Log($"MissionManager ({gameObject.name}): Objective '{objective.description}' (ID: {objectiveId}) completed.");

            // Write the modified copy back to the list
            missionObjectives[objectiveIndex] = objective;

            UpdateUIObjectives();
            CheckMissionCompletion();
        }
    }

    /// <summary>
    /// Immediately sets the mission state to Failed and reports this outcome to the GameManager.
    /// </summary>
    public void FailMission()
    {
        if (currentState == MissionState.Succeeded || currentState == MissionState.Failed)
        {
            Debug.LogWarning($"MissionManager ({gameObject.name}): Mission '{missionTitle}' is already finished ({currentState}). Cannot fail again.");
            return;
        }
         if (gameManager == null)
        {
             Debug.LogError($"MissionManager ({gameObject.name}): Cannot report mission failure for '{missionTitle}', GameManager reference is missing.");
             currentState = MissionState.Failed; // Set state locally anyway
             return;
        }

        Debug.Log($"MissionManager ({gameObject.name}): Mission '{missionTitle}' Failed.");
        currentState = MissionState.Failed;

        gameManager.MissionCompleted(trainingZoneType, false);
    }

    /// <summary>
    /// Attempts to conclude the mission successfully.
    /// Checks if all mandatory objectives are completed. If so, sets state to Succeeded and reports to GameManager.
    /// Can be called explicitly (e.g., reaching an end trigger) or internally by CheckMissionCompletion.
    /// </summary>
    public void CompleteMission()
    {
         if (currentState == MissionState.Succeeded || currentState == MissionState.Failed)
        {
            Debug.LogWarning($"MissionManager ({gameObject.name}): Mission '{missionTitle}' is already finished ({currentState}). Cannot complete again.");
            return;
        }
         if (currentState == MissionState.NotStarted)
        {
             Debug.LogWarning($"MissionManager ({gameObject.name}): Cannot complete mission '{missionTitle}', it has not been started.");
             return;
        }
        if (gameManager == null)
        {
             Debug.LogError($"MissionManager ({gameObject.name}): Cannot report mission success for '{missionTitle}', GameManager reference is missing.");
             return; // Cannot report, so don't change state
        }


        if (AreAllRequiredObjectivesComplete())
        {
            Debug.Log($"MissionManager ({gameObject.name}): Mission '{missionTitle}' Successfully Completed.");
            currentState = MissionState.Succeeded;

            gameManager.MissionCompleted(trainingZoneType, true);
        }
        else
        {
            Debug.LogWarning($"MissionManager ({gameObject.name}): CompleteMission called for '{missionTitle}', but not all required objectives are met. Mission remains InProgress.");
        }
    }


    /// <summary>
    /// Gets the current state of all defined mission objectives.
    /// </summary>
    /// <returns>An array containing copies of the current MissionObjective states.</returns>
    public MissionObjective[] GetCurrentObjectives()
    {
        if (missionObjectives == null)
        {
            return Array.Empty<MissionObjective>();
        }
        // ToArray() creates a new array containing copies of the struct elements.
        return missionObjectives.ToArray();
    }

    /// <summary>
    /// Finds the index of a mission objective within the internal list by its unique ID.
    /// </summary>
    /// <param name="objectiveId">The ID of the objective to find.</param>
    /// <returns>The zero-based index of the objective, or -1 if not found.</returns>
    private int FindObjectiveIndexById(string objectiveId)
    {
        if (string.IsNullOrEmpty(objectiveId) || missionObjectives == null)
        {
            return -1; // Not found
        }
        // Use List<T>.FindIndex for efficient searching
        return missionObjectives.FindIndex(obj => obj.objectiveId == objectiveId);
    }

    /// <summary>
    /// Checks if all objectives marked as non-optional (`isOptional == false`) are completed.
    /// </summary>
    /// <returns>True if all mandatory objectives are complete, false otherwise.</returns>
    private bool AreAllRequiredObjectivesComplete()
    {
        if (missionObjectives == null || missionObjectives.Count == 0)
        {
             Debug.LogWarning($"MissionManager ({gameObject.name}): Checking completion for '{missionTitle}', but no objectives are defined. Assuming complete.");
            return true;
        }
        // Use LINQ Where and All to check if all non-optional objectives have isCompleted == true
        // This works correctly with structs as it iterates over copies for the check.
        return missionObjectives.Where(obj => !obj.isOptional).All(obj => obj.isCompleted);
    }

    /// <summary>
    /// Checks if the conditions for mission success are met (all required objectives complete).
    /// If conditions are met and the mission is InProgress, it calls CompleteMission to finalize.
    /// </summary>
    private void CheckMissionCompletion()
    {
        if (currentState != MissionState.InProgress)
        {
            return;
        }

        if (AreAllRequiredObjectivesComplete())
        {
             CompleteMission();
        }
    }

    /// <summary>
    /// Refreshes the mission objectives displayed in the UI via the UIManager.
    /// </summary>
    private void UpdateUIObjectives()
    {
        if (uiManager != null && currentState == MissionState.InProgress)
        {
            uiManager.UpdateMissionObjectives(GetCurrentObjectives());
        }
    }
}