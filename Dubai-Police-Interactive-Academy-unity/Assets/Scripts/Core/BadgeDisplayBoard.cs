using UnityEngine;
using System; // Required for System.Enum

// Assuming TrainingZoneType is defined globally or in an accessible scope like GameManager.
// Example: public enum TrainingZoneType { Zone1, Zone2, Zone3 }

/// <summary>
/// Manages the visual representation of earned badges on a physical display board
/// object in the central hub. Updates the appearance of badge icons based on
/// which training zones the player has completed.
/// </summary>
public class BadgeDisplayBoard : MonoBehaviour
{
    [Header("Badge Visuals")]
    [Tooltip("Array of GameObjects representing the physical badge icons on the board. Order must match TrainingZoneType enum.")]
    [SerializeField] private GameObject[] badgeIcons;

    [Tooltip("Material applied to a badge icon when it has been earned.")]
    [SerializeField] private Material earnedMaterial;

    [Tooltip("Material applied to a badge icon when it has not been earned.")]
    [SerializeField] private Material unearnedMaterial;

    private bool _isInitialized = false;

    void Start()
    {
        InitializeBoard();
    }

    /// <summary>
    /// Performs initial validation of required components and references.
    /// Also sets the initial state of the board (likely all unearned).
    /// </summary>
    private void InitializeBoard()
    {
        if (_isInitialized) return;

        bool validationError = false;

        if (earnedMaterial == null)
        {
            Debug.LogError("[BadgeDisplayBoard] Earned Material is not assigned.", this);
            validationError = true;
        }

        if (unearnedMaterial == null)
        {
            Debug.LogError("[BadgeDisplayBoard] Unearned Material is not assigned.", this);
            validationError = true;
        }

        if (badgeIcons == null || badgeIcons.Length == 0)
        {
            Debug.LogError("[BadgeDisplayBoard] Badge Icons array is not assigned or is empty.", this);
            validationError = true;
        }
        else
        {
            // Check if the number of icons matches the number of enum values
            int enumCount = Enum.GetValues(typeof(TrainingZoneType)).Length;
            if (badgeIcons.Length != enumCount)
            {
                Debug.LogError($"[BadgeDisplayBoard] Mismatch Error: The number of assigned Badge Icons ({badgeIcons.Length}) does not match the number of TrainingZoneType enum values ({enumCount}). Ensure the array size and order in the Inspector matches the enum definition.", this);
                validationError = true;
            }
            else
            {
                // Check individual icons and their renderers only if counts match
                for (int i = 0; i < badgeIcons.Length; i++)
                {
                    if (badgeIcons[i] == null)
                    {
                        Debug.LogWarning($"[BadgeDisplayBoard] Badge icon at index {i} is null.", this);
                        // This specific icon won't update, but don't stop initialization for others.
                    }
                    else if (badgeIcons[i].GetComponent<Renderer>() == null)
                    {
                        Debug.LogWarning($"[BadgeDisplayBoard] Badge icon '{badgeIcons[i].name}' at index {i} does not have a Renderer component.", badgeIcons[i]);
                        // Allow continuation, but material swapping won't work for this icon.
                    }
                }
            }
        }


        if (!validationError)
        {
            _isInitialized = true;
            // Set initial state assuming no badges are earned yet.
            bool[] initialStates = new bool[badgeIcons.Length]; // All false by default
            UpdateDisplay(initialStates); // Update visuals to initial state
        }
        else
        {
            Debug.LogError("[BadgeDisplayBoard] Initialization failed due to validation errors. Board may not function correctly.", this);
            // Consider disabling the component if critical errors occurred
            // this.enabled = false;
        }
    }


    /// <summary>
    /// Updates the visual appearance (materials) of the badge icons on the board.
    /// </summary>
    /// <param name="earnedBadges">A boolean array indicating the earned status for each badge. The order must match the `badgeIcons` array and the `TrainingZoneType` enum.</param>
    public void UpdateDisplay(bool[] earnedBadges)
    {
        // Re-check initialization state in case Start hasn't run or failed.
        if (!_isInitialized)
        {
             // Attempt initialization again if needed, or log error if it failed previously.
             InitializeBoard();
             if (!_isInitialized) // Check again after attempt
             {
                Debug.LogError("[BadgeDisplayBoard] Cannot UpdateDisplay: Board is not initialized due to setup errors.", this);
                return;
             }
        }

        // Validate input array against the configured icons
        if (earnedBadges == null)
        {
            Debug.LogError("[BadgeDisplayBoard] Cannot UpdateDisplay: Input 'earnedBadges' array is null.", this);
            return;
        }

        // This specific check might seem redundant now due to the InitializeBoard check,
        // but it protects against external calls with incorrectly sized arrays after initialization.
        if (earnedBadges.Length != badgeIcons.Length)
        {
            Debug.LogError($"[BadgeDisplayBoard] Mismatch between input earnedBadges count ({earnedBadges.Length}) and configured badgeIcons count ({badgeIcons.Length}). Update failed. This could indicate an issue with how the earnedBadges array was generated.", this);
            return;
        }

        // Apply materials based on earned status
        for (int i = 0; i < badgeIcons.Length; i++)
        {
            // Check if the specific icon GameObject is assigned and has a renderer
            if (badgeIcons[i] == null)
            {
                // Warning was logged during Initialize, skip this index
                continue;
            }

            Renderer iconRenderer = badgeIcons[i].GetComponent<Renderer>();
            if (iconRenderer == null)
            {
                // Warning was logged during Initialize, skip this index
                continue;
            }

            // Set the material based on the earned status
            // Material null checks happened in Initialize
            iconRenderer.material = earnedBadges[i] ? earnedMaterial : unearnedMaterial;
        }
    }
}