// Required Packages:
// - com.unity.ugui

using UnityEngine;
using UnityEngine.UI; // Required for interacting with UI elements like Text, InputField
using System;
using System.Collections; // Required for Coroutines
using System.Collections.Generic; // Required for List if used internally
using System.Linq; // Required for LINQ operations like string.Join

// Define potential system states. Adjust as needed for specific simulations.
public enum SystemState
{
    Offline,
    Initializing,
    Idle,
    Running,
    Processing,
    AwaitingInput,
    Error,
    Completed,
    // Add specific states relevant to Cyber Threat or Emergency Control if needed
    UnderAttack,
    ThreatNeutralized,
    EmergencyActive,
    ResponseSent
}

// Define a configuration structure or use a ScriptableObject (preferred for flexibility).
// For this example, we'll use a basic ScriptableObject stub.
// Create a separate SystemConfig.cs file for the full definition.
// --- Assumed SystemConfig ScriptableObject structure ---
// [CreateAssetMenu(fileName = "NewSystemConfig", menuName = "Dubai Police Academy/System Config")]
// public class SystemConfig : ScriptableObject
// {
//     public string systemName = "Default Interactive System";
//     public string initialMessage = "System Online. Standby for input.";
//     public List<string> validCommands; // Example: List of commands the system recognizes
//     // Add other configuration data: target objective IDs, success conditions, etc.
// }
// ---------------------------------------------------------


public class InteractiveSystemController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField]
    private Text mainDisplayText; // UI Text element to display messages and system output
    [SerializeField]
    private InputField inputField; // Optional: UI InputField for player text commands

    [Header("System Configuration")]
    [SerializeField]
    private SystemConfig currentConfig; // Assign the specific system configuration SO here

    private SystemState currentState = SystemState.Offline;
    private List<string> messageLog = new List<string>(); // Keep a log for display
    private const int MaxLogLines = 20; // Limit the number of lines shown

    /// <summary>
    /// Event triggered when a significant action or state change occurs within the system.
    /// The string parameter can provide context (e.g., "COMMAND_EXECUTED:scan", "OBJECTIVE_COMPLETE:neutralize_threat").
    /// </summary>
    public event Action<string> OnSystemEvent;

    void Awake()
    {
        // Basic validation
        if (mainDisplayText == null)
        {
            Debug.LogError($"{nameof(InteractiveSystemController)}: Main Display Text UI element is not assigned.", this);
        }
        if (inputField != null)
        {
            inputField.onSubmit.AddListener(ProcessPlayerInput); // Listen for Enter key submission
        }
        else
        {
            Debug.LogWarning($"{nameof(InteractiveSystemController)}: Input Field is not assigned. Input must be provided via ProcessPlayerInput() calls.", this);
        }
    }

    void OnDestroy()
    {
        // Clean up listeners
        if (inputField != null)
        {
            inputField.onSubmit.RemoveListener(ProcessPlayerInput);
        }
    }

    /// <summary>
    /// Initializes the interactive system with the given configuration.
    /// </summary>
    /// <param name="config">The ScriptableObject containing system settings.</param>
    public void InitializeSystem(SystemConfig config)
    {
        if (config == null)
        {
            Debug.LogError($"{nameof(InteractiveSystemController)}: Cannot initialize with a null SystemConfig.", this);
            UpdateSystemState(SystemState.Error);
            DisplayMessage("ERROR: System configuration missing.");
            return;
        }

        currentConfig = config;
        messageLog.Clear();
        UpdateSystemState(SystemState.Initializing);
        DisplayMessage($"Initializing {currentConfig.systemName}...");
        // Simulate initialization delay or process if needed
        // For now, transition directly to Idle/Running
        UpdateSystemState(SystemState.Idle);
        DisplayMessage(currentConfig.initialMessage);
        SetInputActive(true);
        OnSystemEvent?.Invoke($"SYSTEM_INITIALIZED:{currentConfig.systemName}");
    }

    /// <summary>
    /// Processes player input, typically from a UI InputField or external trigger.
    /// </summary>
    /// <param name="input">The command or data entered by the player.</param>
    public void ProcessPlayerInput(string input)
    {
        if (currentState == SystemState.Offline || currentState == SystemState.Initializing || currentState == SystemState.Error || currentConfig == null)
        {
            DisplayMessage("System is not ready to accept input.");
            ClearInputField();
            return;
        }

        if (string.IsNullOrWhiteSpace(input))
        {
            return; // Ignore empty input
        }

        string processedInput = input.Trim();
        DisplayMessage($"> {processedInput}"); // Echo input to the display

        // --- Input Processing Logic ---
        bool commandRecognized = false;
        string[] parts = processedInput.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            ClearInputField();
            return; // Should not happen due to IsNullOrWhiteSpace check, but safeguard
        }

        string command = parts[0].ToLower();
        string[] arguments = parts.Skip(1).ToArray(); // Get arguments if any

        // Check against valid commands defined in SystemConfig
        if (currentConfig != null && currentConfig.validCommands != null && currentConfig.validCommands.Contains(command))
        {
            commandRecognized = true;
            UpdateSystemState(SystemState.Processing); // Assume most commands involve processing
            OnSystemEvent?.Invoke($"COMMAND_EXECUTED:{command}");

            // --- Implement Actual Command Logic Here ---
            switch (command)
            {
                case "scan":
                    string target = arguments.Length > 0 ? string.Join(" ", arguments) : "area";
                    DisplayMessage($"Executing scan on {target}...");
                    // Example: Start a coroutine for a timed scan
                    StartCoroutine(SimulateCommandProcessing(command, 2.0f, $"Scan of {target} complete."));
                    break;

                case "report":
                    DisplayMessage("Generating system report...");
                    // Example: Directly generate report data (or start coroutine if complex)
                    // string reportData = GenerateReport(); // Assuming such a method exists
                    // DisplayMessage(reportData);
                    // UpdateSystemState(SystemState.Idle); // Update state after generation
                    StartCoroutine(SimulateCommandProcessing(command, 1.0f, "Report generated."));
                    break;

                case "analyze":
                     if (arguments.Length > 0)
                     {
                         string analysisTarget = string.Join(" ", arguments);
                         DisplayMessage($"Analyzing target: {analysisTarget}...");
                         StartCoroutine(SimulateCommandProcessing(command, 1.5f, $"Analysis of {analysisTarget} complete. Results available."));
                     }
                     else
                     {
                         DisplayMessage("Analyze command requires a target.");
                         UpdateSystemState(SystemState.Idle); // Go back to idle if invalid syntax
                     }
                    break;

                 case "status": // Example: Integrate status command
                    DisplayMessage($"Current System State: {currentState}");
                    UpdateSystemState(SystemState.Idle); // Status check is instant
                    break;

                // Add cases for all valid commands defined in SystemConfig
                default:
                    // This case might be reached if a command is in validCommands but not handled here
                    DisplayMessage($"Command '{command}' recognized but execution not implemented.");
                     UpdateSystemState(SystemState.Idle); // Revert to idle if not implemented
                    break;
            }
        }
        else if (command == "help") // Handle 'help' separately or as a standard command
        {
            commandRecognized = true;
            string helpText = "Available commands: ";
            if (currentConfig?.validCommands != null && currentConfig.validCommands.Count > 0)
            {
                helpText += string.Join(", ", currentConfig.validCommands);
            }
            else
            {
                helpText += "N/A (No commands configured)";
            }
             // Add help command if it's not in the main list
            if (currentConfig?.validCommands == null || !currentConfig.validCommands.Contains("help"))
            {
                 if (currentConfig?.validCommands?.Count > 0) helpText += ", ";
                 helpText += "help";
            }

            DisplayMessage(helpText);
            OnSystemEvent?.Invoke("COMMAND_HELP_REQUESTED");
        }


        if (!commandRecognized)
        {
            DisplayMessage($"Unknown command: '{processedInput}'. Type 'help' for available commands.");
            OnSystemEvent?.Invoke($"COMMAND_UNKNOWN:{processedInput}");
        }
        // --- End Input Processing ---

        ClearInputField();
        // Input activation is now handled by UpdateSystemState and Coroutine completion
    }


    /// <summary>
    /// Simulates a delay for command processing and updates state upon completion.
    /// </summary>
    private IEnumerator SimulateCommandProcessing(string commandName, float delay, string completionMessage)
    {
        // System is already in Processing state, input disabled via UpdateSystemState
        yield return new WaitForSeconds(delay);

        UpdateSystemState(SystemState.Idle); // Or AwaitingInput, Completed etc.
        DisplayMessage(completionMessage);
        // Potentially trigger another event based on command results
        OnSystemEvent?.Invoke($"COMMAND_COMPLETED:{commandName}");
        // SetInputActive is handled by UpdateSystemState transition
    }


    /// <summary>
    /// Displays a message on the system's main output UI. Manages a simple message log.
    /// </summary>
    /// <param name="message">The message string to display.</param>
    public void DisplayMessage(string message)
    {
        if (mainDisplayText == null) return;

        // Add message to log
        messageLog.Add(message);

        // Trim log if it exceeds max lines
        while (messageLog.Count > MaxLogLines)
        {
            messageLog.RemoveAt(0);
        }

        // Update the UI Text element
        mainDisplayText.text = string.Join("\n", messageLog);

        // Optional: Auto-scroll logic if using ScrollRect
        // (Requires setup with ScrollRect and ContentSizeFitter on the Text parent)
        // Example: Find ScrollRect parent and set verticalNormalizedPosition = 0f;
    }

    /// <summary>
    /// Updates the internal state of the system.
    /// </summary>
    /// <param name="newState">The state to transition to.</param>
    public void UpdateSystemState(SystemState newState)
    {
        if (currentState == newState) return;

        SystemState previousState = currentState;
        currentState = newState;
        // Optional: Add logic here that triggers when entering/exiting specific states
        // Debug.Log($"System state changed from {previousState} to {currentState}");

        OnSystemEvent?.Invoke($"SYSTEM_STATE_CHANGED:{currentState}");

         // Automatically disable input if system is busy or completed/errored
        bool isInputAllowed = currentState != SystemState.Processing &&
                              currentState != SystemState.Completed &&
                              currentState != SystemState.Error &&
                              currentState != SystemState.Offline &&
                              currentState != SystemState.Initializing;

        SetInputActive(isInputAllowed);
    }

    /// <summary>
    /// Gets the current state of the system.
    /// </summary>
    public SystemState GetCurrentState()
    {
        return currentState;
    }


    // --- Helper Methods ---

    private void ClearInputField()
    {
        if (inputField != null)
        {
            inputField.text = "";
            // Keep focus only if input is expected to remain active
            if (inputField.interactable)
            {
                 inputField.ActivateInputField();
            }
        }
    }

    private void SetInputActive(bool active)
    {
        if (inputField != null)
        {
            inputField.interactable = active;
            if (active && gameObject.activeInHierarchy) // Only activate if component is active
            {
                 // Ensure the input field gets focus when re-enabled
                 StartCoroutine(RefocusInputField());
            }
        }
    }

     // Coroutine to ensure input field activation happens after potential layout rebuilds
    private IEnumerator RefocusInputField()
    {
        yield return null; // Wait one frame
        if (inputField != null && inputField.interactable)
        {
             inputField.ActivateInputField();
             inputField.Select(); // Extra assurance it gets focus
        }
    }
}