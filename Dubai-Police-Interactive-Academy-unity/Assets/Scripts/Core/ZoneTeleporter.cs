using UnityEngine;
using UnityEngine.Serialization; // Recommended for FormerlySerializedAs if needed later

// Note: The IInteractable interface is assumed to be defined in its own file (e.g., IInteractable.cs)
// public interface IInteractable { void Interact(); } // Definition removed from here

/// <summary>
/// Placed at entrances to training zones in the hub. When the player interacts,
/// it requests the GameManager to load the corresponding training zone scene.
/// Requires a Collider component (set to Is Trigger) on the same GameObject
/// to be detected by the player's interaction system.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ZoneTeleporter : MonoBehaviour, IInteractable
{
    [Header("Teleporter Configuration")]
    [Tooltip("The specific training zone this teleporter leads to.")]
    [SerializeField] private TrainingZoneType zoneType; // Assuming TrainingZoneType is an enum defined elsewhere

    private bool _isInteractable = true; // Prevents spamming interaction if needed

    private void Start()
    {
        // Basic validation to ensure a collider exists and is a trigger
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError($"ZoneTeleporter on {gameObject.name} requires a Collider component.", this);
            _isInteractable = false; // Disable interaction if misconfigured
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning($"Collider on ZoneTeleporter {gameObject.name} is not set to 'Is Trigger'. Interaction might not work as expected depending on PlayerHubController implementation.", this);
            // Interaction might still work depending on how PlayerHubController checks, so don't disable.
        }
    }

    /// <summary>
    /// Called by the PlayerHubController when the player interacts with this object.
    /// Initiates the loading of the associated training zone scene via the GameManager.
    /// </summary>
    public void Interact()
    {
        if (!_isInteractable)
        {
            Debug.LogWarning($"Interaction attempt on disabled ZoneTeleporter: {gameObject.name}");
            return;
        }

        Debug.Log($"ZoneTeleporter interacted: Requesting load for zone {zoneType}");

        // Get the GameManager instance (Assuming a Singleton pattern)
        GameManager gameManager = GameManager.Instance; // Assuming GameManager is defined elsewhere

        if (gameManager != null)
        {
            // Prevent double-clicks during scene transition start
            _isInteractable = false;

            // Request the GameManager to load the scene
            gameManager.LoadTrainingZone(zoneType); // Assuming LoadTrainingZone exists in GameManager

            // Optionally re-enable interaction after a delay or upon returning to hub if needed,
            // but typically the object will be destroyed/reloaded with the scene change.
        }
        else
        {
            Debug.LogError("ZoneTeleporter could not find the GameManager instance. Cannot load training zone.", this);
            // Keep interactable true so player might try again if GameManager appears later? Or log more persistently.
        }
    }

    // Ensure interaction is re-enabled if the object persists and loading fails or is cancelled
    // This might be handled better by GameManager resetting state if loading fails.
    // For simplicity, assume scene transition handles disabling/destroying this object.
    // If this object needs to remain interactable after a failed load attempt, add logic here or in GameManager.
}

// Assuming TrainingZoneType enum is defined elsewhere, e.g.:
// public enum TrainingZoneType { Movement, Combat, Puzzle }

// Assuming GameManager class with Singleton and LoadTrainingZone method is defined elsewhere, e.g.:
// public class GameManager : MonoBehaviour
// {
//     public static GameManager Instance { get; private set; }
//     private void Awake() { if (Instance == null) Instance = this; else Destroy(gameObject); DontDestroyOnLoad(gameObject); }
//     public void LoadTrainingZone(TrainingZoneType zone) { /* Scene loading logic */ Debug.Log($"GameManager: Loading scene for {zone}"); }
// }