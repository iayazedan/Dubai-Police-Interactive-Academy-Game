// --- VehicleControllerBase.cs ---
// Note: This file needs to be created or updated separately.
// Assuming it exists and needs the new methods added.

using UnityEngine;

/// <summary>
/// Base abstract class for all controllable vehicles in the simulation.
/// Defines common interface methods for interaction with systems like the Anomaly Manager.
/// </summary>
public abstract class VehicleControllerBase : MonoBehaviour
{
    // Existing methods and properties would go here...

    /// <summary>
    /// Modifies the vehicle's speed or acceleration capability.
    /// </summary>
    /// <param name="multiplier">Value typically from 0.0 (stopped) to 1.0 (normal speed).</param>
    public abstract void SetSpeedMultiplier(float multiplier);

    /// <summary>
    /// Enables or disables player control input processing for the vehicle.
    /// </summary>
    /// <param name="isEnabled">True to enable controls, false to disable.</param>
    public abstract void SetControlEnabled(bool isEnabled);

    // Concrete implementations (e.g., in HoverbikeController, CarController)
    // will override these abstract methods to perform the actual logic.
}


// --- SimulationAnomalyManager.cs ---
// Updated script with fixes applied.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// --- Supporting Definitions ---
// It's recommended to place these enums and the ScriptableObject class in their own separate files
// (e.g., AnomalyType.cs, VehicleAnomalyEffect.cs, AnomalyConfig.cs)
// For brevity here, they are included before the main class.

/// <summary>
/// Defines the different categories of simulation anomalies.
/// </summary>
public enum AnomalyType
{
    EquipmentFailure,    // e.g., Vehicle controls glitch
    EnvironmentalChange, // e.g., Sudden fog, unexpected obstacle
    UnexpectedEvent      // e.g., Simulated civilian emergency nearby
}

/// <summary>
/// Defines specific effects that can be applied to vehicles during an anomaly.
/// </summary>
public enum VehicleAnomalyEffect
{
    None,
    ReduceSpeed,    // Intensity controls percentage reduction
    DisableControls // Intensity might control duration indirectly or be unused
    // Add more as needed, e.g., IncreaseDrift, ForceSteer
}

/// <summary>
/// ScriptableObject defining the parameters for a specific simulation anomaly.
/// Create instances via Assets > Create > Dubai Police Academy > Anomaly Configuration.
/// </summary>
[CreateAssetMenu(fileName = "AnomalyConfig_", menuName = "Dubai Police Academy/Anomaly Configuration", order = 1)]
public class AnomalyConfig : ScriptableObject
{
    [Tooltip("The category of this anomaly.")]
    public AnomalyType anomalyType = AnomalyType.EquipmentFailure;

    [Tooltip("Editor description for identification.")]
    public string description = "Default Anomaly";

    [Tooltip("Duration of the effect in seconds. 0 means instant or requires manual reset.")]
    [Min(0f)] public float duration = 5.0f;

    [Tooltip("Intensity of the effect (e.g., 0.5 for 50% speed reduction). Context-dependent.")]
    [Range(0f, 1f)] public float intensity = 0.5f;

    [Header("Vehicle Specific Effects")]
    [Tooltip("Specific effect to apply to player vehicles.")]
    public VehicleAnomalyEffect vehicleEffect = VehicleAnomalyEffect.None;

    // Potential future additions can be uncommented and implemented as needed
    // [Header("Visual & Audio")]
    // public GameObject effectPrefab;
    // public AudioClip soundEffect;
    // public string messageForUI;
    //
    // [Header("Environment Specific Effects")]
    // public WeatherType targetWeather; // Define WeatherType enum if used
    // public GameObject obstacleToSpawn;
}
// --- End Supporting Definitions ---



/// <summary>
/// Manages the injection and effects of simulation anomalies during training missions.
/// It finds relevant game objects (like the player's vehicle) and applies effects based on AnomalyConfig settings.
/// </summary>
public class SimulationAnomalyManager : MonoBehaviour
{
    [Tooltip("List of possible anomaly configurations that can be triggered.")]
    [SerializeField] private List<AnomalyConfig> anomalies = new List<AnomalyConfig>();

    // Cached reference to the active vehicle controller, if applicable to the current scene/anomaly
    private VehicleControllerBase currentVehicleController;
    private Coroutine activeVehicleAnomalyCoroutine = null; // Track running vehicle anomaly coroutine

    // References to other potential systems can be added and assigned here
    // private EnvironmentManager environmentManager;
    // private UIManager uiManager;
    // private AudioManager audioManager;

    /// <summary>
    /// Attempts to find and activate a configured anomaly matching the specified type.
    /// </summary>
    /// <param name="type">The type of anomaly to activate.</param>
    public void ActivateAnomaly(AnomalyType type)
    {
        AnomalyConfig config = anomalies.FirstOrDefault(a => a != null && a.anomalyType == type);

        if (config != null)
        {
            Debug.Log($"SimulationAnomalyManager: Activating anomaly '{config.description}' (Type: {type}).");
            TriggerAnomalyEffect(config);
        }
        else
        {
            Debug.LogWarning($"SimulationAnomalyManager: No AnomalyConfig found for type {type}. Cannot activate.");
        }
    }

    /// <summary>
    /// Triggers the specific effects defined in the provided AnomalyConfig.
    /// </summary>
    /// <param name="config">The configuration defining the anomaly's effects.</param>
    private void TriggerAnomalyEffect(AnomalyConfig config)
    {
        if (config == null)
        {
            Debug.LogError("SimulationAnomalyManager: TriggerAnomalyEffect called with null config.");
            return;
        }

        // --- Apply Vehicle Effects ---
        if (config.vehicleEffect != VehicleAnomalyEffect.None)
        {
            // Find the vehicle controller if not already cached or if it might have changed
            if (currentVehicleController == null)
            {
                 currentVehicleController = FindObjectOfType<VehicleControllerBase>();
            }

            if (currentVehicleController != null)
            {
                 // Stop any previous vehicle anomaly coroutine if a new one is starting
                if (activeVehicleAnomalyCoroutine != null)
                {
                    StopCoroutine(activeVehicleAnomalyCoroutine);
                    // Ensure previous effect is cleaned up before starting new one
                    // Reset all effects potentially modified by the previous anomaly
                    ResetVehicleEffect(currentVehicleController);
                    Debug.Log("SimulationAnomalyManager: Stopped previous vehicle anomaly effect and reset vehicle state.");
                    activeVehicleAnomalyCoroutine = null;
                }

                // Start the new effect coroutine
                activeVehicleAnomalyCoroutine = StartCoroutine(ApplyVehicleEffectCoroutine(config, currentVehicleController));
            }
            else
            {
                Debug.LogWarning($"SimulationAnomalyManager: Anomaly '{config.description}' targets vehicle, but no VehicleControllerBase found in scene.");
            }
        }

        // --- Apply Environmental Effects (Requires specific system integration) ---
        if (config.anomalyType == AnomalyType.EnvironmentalChange)
        {
            // Example: Integration with a hypothetical WeatherManager or EnvironmentController
            // FindObjectOfType<EnvironmentManager>()?.ApplyEnvironmentalChange(config);
            Debug.Log($"SimulationAnomalyManager: Environmental change anomaly '{config.description}' triggered. Needs integration with specific environment systems.");
        }

        // --- Apply Unexpected Event Effects (Requires specific system integration) ---
        if (config.anomalyType == AnomalyType.UnexpectedEvent)
        {
            // Example: Integration with a hypothetical EventManager or SpawnManager
            // FindObjectOfType<EventManager>()?.TriggerEvent(config);
            Debug.Log($"SimulationAnomalyManager: Unexpected event anomaly '{config.description}' triggered. Needs integration with event/spawning systems.");
        }

        // --- Notify UI (Requires specific system integration) ---
        // Example: Integration with a hypothetical UIManager
        // if (!string.IsNullOrEmpty(config.messageForUI)) {
        //     FindObjectOfType<UIManager>()?.ShowAnomalyNotification(config.messageForUI);
        // }

        // --- Play Sound (Requires specific system integration) ---
        // Example: Integration with a hypothetical AudioManager
        // if (config.soundEffect != null) {
        //     FindObjectOfType<AudioManager>()?.PlaySoundEffect(config.soundEffect);
        // }
    }

    /// <summary>
    /// Coroutine to apply a timed effect to a vehicle.
    /// </summary>
    private IEnumerator ApplyVehicleEffectCoroutine(AnomalyConfig config, VehicleControllerBase vehicle)
    {
        if (vehicle == null)
        {
             Debug.LogError("SimulationAnomalyManager: ApplyVehicleEffectCoroutine called with null vehicle.");
             activeVehicleAnomalyCoroutine = null; // Ensure coroutine state is cleared
             yield break;
        }

        Debug.Log($"SimulationAnomalyManager: Applying effect '{config.vehicleEffect}' to {vehicle.gameObject.name} for {config.duration} seconds.");

        // Apply initial effect based on type
        switch (config.vehicleEffect)
        {
            case VehicleAnomalyEffect.ReduceSpeed:
                vehicle.SetSpeedMultiplier(Mathf.Clamp01(1.0f - config.intensity));
                break;
            case VehicleAnomalyEffect.DisableControls:
                vehicle.SetControlEnabled(false);
                break;
            // Add cases for other VehicleAnomalyEffect types
        }

        // Wait for the specified duration
        if (config.duration > 0)
        {
            yield return new WaitForSeconds(config.duration);
        }
        else // Duration 0 might mean indefinite until manually cleared or another anomaly overrides
        {
           Debug.Log($"SimulationAnomalyManager: Vehicle effect '{config.vehicleEffect}' has 0 duration, effect persists until manually reset or overridden.");
           // Effect applied, coroutine's timed portion is done.
           activeVehicleAnomalyCoroutine = null; // Mark timed portion as finished
           yield break; // Exit coroutine, effect remains active
        }


        // --- Effect Reset ---
        // This part is reached only if duration > 0
        Debug.Log($"SimulationAnomalyManager: Resetting effect '{config.vehicleEffect}' on {vehicle.gameObject.name} after duration.");
        ResetVehicleEffect(vehicle, config.vehicleEffect); // Pass the specific effect being reset

        activeVehicleAnomalyCoroutine = null; // Mark coroutine as fully finished
    }

    /// <summary>
    /// Resets specific or all effects applied to the vehicle.
    /// </summary>
    /// <param name="vehicle">The vehicle controller to reset.</param>
    /// <param name="effectToReset">Optional: The specific effect type to reset. If None, resets all known effects.</param>
    private void ResetVehicleEffect(VehicleControllerBase vehicle, VehicleAnomalyEffect effectToReset = VehicleAnomalyEffect.None)
    {
         if (vehicle == null) return;

        // Reset specific effects based on the parameter or reset all known modified states
        if (effectToReset == VehicleAnomalyEffect.ReduceSpeed || effectToReset == VehicleAnomalyEffect.None)
        {
            vehicle.SetSpeedMultiplier(1.0f);
        }
        if (effectToReset == VehicleAnomalyEffect.DisableControls || effectToReset == VehicleAnomalyEffect.None)
        {
            vehicle.SetControlEnabled(true);
        }

         // Log which effect was specifically reset, or indicate a general reset
         if (effectToReset != VehicleAnomalyEffect.None)
         {
              Debug.Log($"SimulationAnomalyManager: Reset specific vehicle effect '{effectToReset}' on {vehicle.gameObject.name}.");
         } else {
              Debug.Log($"SimulationAnomalyManager: Resetting all known vehicle effects on {vehicle.gameObject.name}.");
         }
    }

    /// <summary>
    /// Called when the MonoBehaviour is destroyed; ensures coroutines are stopped and effects reset.
    /// </summary>
    private void OnDestroy()
    {
        // Stop any running coroutine when the manager is destroyed
        if (activeVehicleAnomalyCoroutine != null)
        {
            StopCoroutine(activeVehicleAnomalyCoroutine);
             activeVehicleAnomalyCoroutine = null; // Clear reference

            // Attempt to reset the vehicle effect if the controller still exists
            if(currentVehicleController != null)
            {
                Debug.Log($"SimulationAnomalyManager: Resetting vehicle effects on {currentVehicleController.gameObject.name} during OnDestroy.");
                // Reset all possible effects on cleanup
                ResetVehicleEffect(currentVehicleController);
            }
        }
    }

    // --- Potential Helper Methods ---
    // private Vector3 CalculateSpawnPosition() { /* Logic to find a suitable spawn point */ return Vector3.zero; }
}