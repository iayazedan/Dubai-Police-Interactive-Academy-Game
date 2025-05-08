using UnityEngine;
using System;

/// <summary>
/// An abstract base class providing common functionality and interface for
/// different vehicle types (Car, Hoverbike, Drone). Handles input mapping,
/// basic physics structure, and exposes methods for movement and control.
/// Specific vehicle implementations must inherit from this class.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public abstract class VehicleControllerBase : MonoBehaviour
{
    /// <summary>
    /// Event triggered when the vehicle contributes to objective progress.
    /// Parameters: objectiveId (string), progress (float).
    /// </summary>
    public event Action<string, float> OnObjectiveProgress;

    /// <summary>
    /// Reference to the Rigidbody component attached to the vehicle.
    /// </summary>
    protected Rigidbody vehicleRigidbody;

    /// <summary>
    /// Cached reference to the Transform component.
    /// </summary>
    protected Transform vehicleTransform;

    /// <summary>
    /// Stores the current movement input received.
    /// </summary>
    protected Vector2 currentMovementInput;

    /// <summary>
    /// Stores the current look/aiming input received.
    /// </summary>
    protected Vector2 currentLookInput;

    /// <summary>
    /// Ensures Rigidbody and Transform references are cached.
    /// Can be overridden by derived classes, but base.Awake() should be called.
    /// </summary>
    protected virtual void Awake()
    {
        vehicleRigidbody = GetComponent<Rigidbody>();
        vehicleTransform = transform;

        if (vehicleRigidbody == null)
        {
            Debug.LogError($"VehicleControllerBase on {gameObject.name} requires a Rigidbody component.", this);
        }
    }

    /// <summary>
    /// Called every fixed framerate frame. Use this for physics calculations.
    /// Calls the abstract FixedTick and ApplyPhysicsForces methods.
    /// </summary>
    protected virtual void FixedUpdate()
    {
        // Allow derived classes to perform pre-physics calculations
        FixedTick();

        // Apply physics forces based on input and state
        ApplyPhysicsForces();
    }

    /// <summary>
    /// Abstract method to be implemented by derived classes.
    /// Processes the raw input vectors provided externally.
    /// Derived classes should store or process this input for use in FixedTick/ApplyPhysicsForces.
    /// </summary>
    /// <param name="movementInput">Input vector for movement (e.g., WASD, Left Stick).</param>
    /// <param name="lookInput">Input vector for looking/aiming (e.g., Mouse Delta, Right Stick).</param>
    public abstract void HandleInput(Vector2 movementInput, Vector2 lookInput);

    /// <summary>
    /// Abstract method to be implemented by derived classes.
    /// Called every FixedUpdate, intended for non-force-based physics updates
    /// or state management related to physics (e.g., calculating target velocities, drag).
    /// </summary>
    public abstract void FixedTick();

    /// <summary>
    /// Abstract method to be implemented by derived classes.
    /// Called every FixedUpdate after FixedTick, intended specifically for
    /// applying forces or torques to the Rigidbody using vehicleRigidbody.AddForce, AddTorque, etc.
    /// Use appropriate ForceModes.
    /// </summary>
    public abstract void ApplyPhysicsForces();

    /// <summary>
    /// Protected method for derived classes to invoke the OnObjectiveProgress event.
    /// Ensures the event is only raised if there are subscribers.
    /// </summary>
    /// <param name="objectiveId">The unique identifier of the objective.</param>
    /// <param name="progress">The progress value (often normalized 0-1, but depends on objective).</param>
    protected void NotifyObjectiveProgress(string objectiveId, float progress)
    {
        OnObjectiveProgress?.Invoke(objectiveId, progress);
    }

    /// <summary>
    /// Resets the vehicle's velocity and angular velocity. Useful for teleporting or resetting state.
    /// Can be overridden if more complex reset logic is needed.
    /// </summary>
    public virtual void ResetVehicleState()
    {
        if (vehicleRigidbody != null)
        {
            vehicleRigidbody.velocity = Vector3.zero;
            vehicleRigidbody.angularVelocity = Vector3.zero;
        }
        currentMovementInput = Vector2.zero;
        currentLookInput = Vector2.zero;
    }
}