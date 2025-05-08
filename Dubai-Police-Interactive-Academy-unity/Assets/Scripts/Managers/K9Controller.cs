// Required Packages:
// - com.unity.ai.navigation

using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections;

/// <summary>
/// Defines the possible actions the K9 can perform.
/// </summary>
public enum K9ActionType
{
    None,
    Search,
    Bark,
    Apprehend,
    // Add other specific actions as needed for the simulation
}

/// <summary>
/// Defines the possible states of the K9.
/// </summary>
public enum K9State
{
    Idle,
    Moving,
    PerformingAction
}

/// <summary>
/// Manages the behavior and state of the K9 dog character. Responds to player
/// commands, handles pathfinding, and performs actions like searching or
/// apprehending simulated targets.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class K9Controller : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField]
    [Tooltip("Stopping distance from the target destination.")]
    private float stoppingDistance = 0.5f;

    [Header("Action Settings")]
    [SerializeField]
    [Tooltip("Default duration for actions if not animation-driven. May vary per action type.")]
    private float defaultActionDuration = 2.0f;

    // --- Events ---
    /// <summary>
    /// Fired when the K9 successfully completes a requested action.
    /// </summary>
    public event Action OnActionCompleted;

    // --- Components ---
    private NavMeshAgent navMeshAgent;
    private Animator animator;

    // --- State ---
    private K9State currentState = K9State.Idle;
    private K9ActionType currentAction = K9ActionType.None;
    private Coroutine actionCoroutine = null;
    private Vector3 currentDestination;

    // --- Animator Parameters ---
    private static readonly int SpeedParam = Animator.StringToHash("Speed");
    private static readonly int ActionTriggerParam = Animator.StringToHash("ActionTrigger"); // A generic trigger
    private static readonly int ActionIndexParam = Animator.StringToHash("ActionIndex"); // Index to select specific action animation

    void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (navMeshAgent == null)
        {
            Debug.LogError("K9Controller requires a NavMeshAgent component.", this);
            enabled = false; // Disable script if component is missing
            return;
        }
        if (animator == null)
        {
            Debug.LogError("K9Controller requires an Animator component.", this);
            enabled = false; // Disable script if component is missing
            return;
        }

        // Initial setup
        navMeshAgent.stoppingDistance = stoppingDistance;
        navMeshAgent.isStopped = true; // Start stationary
    }

    void Update()
    {
        UpdateState();
        UpdateAnimator();
    }

    private void UpdateState()
    {
        // If moving, check if destination is reached
        if (currentState == K9State.Moving && !navMeshAgent.pathPending)
        {
            if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
            {
                if (!navMeshAgent.hasPath || navMeshAgent.velocity.sqrMagnitude == 0f)
                {
                    // Reached destination
                    StopMovement();
                    // Optionally, trigger an idle animation or specific arrival behavior
                }
            }
        }
    }

    private void UpdateAnimator()
    {
        // Update speed parameter based on NavMeshAgent velocity
        float speed = navMeshAgent.velocity.magnitude / navMeshAgent.speed; // Normalized speed
        animator.SetFloat(SpeedParam, speed);
    }

    /// <summary>
    /// Commands the K9 to move to the specified world position using NavMesh.
    /// </summary>
    /// <param name="position">The target world position.</param>
    public void SetDestination(Vector3 position)
    {
        if (currentState == K9State.PerformingAction)
        {
            Debug.LogWarning("K9 cannot move while performing an action. Action must complete or be cancelled first.", this);
            return;
        }

        // Check if the position is valid on the NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(position, out hit, 1.0f, NavMesh.AllAreas))
        {
            currentDestination = hit.position;
            navMeshAgent.SetDestination(currentDestination);
            navMeshAgent.isStopped = false;
            currentState = K9State.Moving;
            if (actionCoroutine != null)
            {
                StopCoroutine(actionCoroutine); // Cancel any pending action coroutine if moving overrides it
                actionCoroutine = null;
                currentAction = K9ActionType.None;
            }
        }
        else
        {
            Debug.LogWarning($"K9Controller: Could not find a valid NavMesh position near {position}.", this);
        }
    }

    /// <summary>
    /// Commands the K9 to perform a specific action.
    /// </summary>
    /// <param name="action">The type of action to perform.</param>
    public void PerformAction(K9ActionType action)
    {
        if (action == K9ActionType.None) return;

        if (currentState == K9State.PerformingAction)
        {
            Debug.LogWarning($"K9 is already performing action {currentAction}. New action request ignored.", this);
            return;
        }

        // Stop movement if currently moving
        if (currentState == K9State.Moving)
        {
            StopMovement();
        }

        currentState = K9State.PerformingAction;
        currentAction = action;
        navMeshAgent.isStopped = true; // Ensure K9 stays put during action

        // Trigger animation and start coroutine for duration/completion
        TriggerActionAnimation(action);
        if (actionCoroutine != null)
        {
            StopCoroutine(actionCoroutine);
        }
        actionCoroutine = StartCoroutine(ActionExecutionCoroutine(action));
    }

    /// <summary>
    /// Checks if the K9 is currently busy performing an action.
    /// </summary>
    /// <returns>True if performing an action, false otherwise.</returns>
    public bool IsPerformingAction()
    {
        return currentState == K9State.PerformingAction;
    }

    /// <summary>
    /// Stops the K9's current movement and sets state to Idle.
    /// </summary>
    private void StopMovement()
    {
        if (navMeshAgent.isOnNavMesh) // Check if agent is valid
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.ResetPath(); // Clear current path
        }
        currentState = K9State.Idle;
        currentDestination = transform.position; // Update destination to current spot
    }

    /// <summary>
    /// Triggers the appropriate animation for the given action.
    /// Assumes Animator has parameters set up for actions.
    /// </summary>
    /// <param name="action">The action being performed.</param>
    private void TriggerActionAnimation(K9ActionType action)
    {
        // Example: Use an integer index or separate triggers per action
        // This example uses an index and a single trigger
        animator.SetInteger(ActionIndexParam, (int)action);
        animator.SetTrigger(ActionTriggerParam);

        // Alternative: Use specific triggers like "TriggerSearch", "TriggerBark"
        // switch(action)
        // {
        //     case K9ActionType.Search: animator.SetTrigger("TriggerSearch"); break;
        //     case K9ActionType.Bark: animator.SetTrigger("TriggerBark"); break;
        //     // ... etc
        // }
    }

    /// <summary>
    /// Coroutine to manage the duration and completion callback of an action.
    /// </summary>
    /// <param name="action">The action being executed.</param>
    private IEnumerator ActionExecutionCoroutine(K9ActionType action)
    {
        // Determine duration - could be animation-driven or fixed
        // For simplicity, using a fixed duration here.
        // In a production scenario, you might wait for an animation state to finish
        // or use Animation Events.
        float duration = GetActionDuration(action);
        yield return new WaitForSeconds(duration);

        // Action completed
        currentState = K9State.Idle;
        currentAction = K9ActionType.None;
        actionCoroutine = null;

        // Notify listeners
        OnActionCompleted?.Invoke();
        Debug.Log($"K9 action {action} completed.", this);
    }

    /// <summary>
    /// Determines the duration for a specific K9 action.
    /// This could be expanded to query animation lengths or use specific values per action.
    /// </summary>
    /// <param name="action">The action type.</param>
    /// <returns>Duration in seconds.</returns>
    private float GetActionDuration(K9ActionType action)
    {
        // Example: provide different durations per action if needed
        switch (action)
        {
            case K9ActionType.Search:
                return 3.0f; // Example duration
            case K9ActionType.Bark:
                return 1.5f; // Example duration
            case K9ActionType.Apprehend:
                return 4.0f; // Example duration
            default:
                return defaultActionDuration;
        }
    }

    // Optional: Add methods to cancel actions or query current state/action if needed
    // public void CancelCurrentAction() { ... }
    // public K9State GetCurrentState() { return currentState; }
    // public K9ActionType GetCurrentAction() { return currentAction; }
}