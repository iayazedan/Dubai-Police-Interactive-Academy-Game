# Dubai Police Interactive Academy - Unity Scripts

## Folder Structure

This package follows Unity's recommended folder structure:

- Assets/
  - Scripts/
    - Core/          # Core gameplay scripts
    - Managers/      # Game management scripts
    - UI/            # User interface scripts
    - Data/          # Data models and structures
    - Utilities/     # Helper functions and utilities

## Script References

All scripts in this project can reference each other regardless of their folder location. The folder structure is for organization only and doesn't affect script visibility.

## Scripts Overview

### Table of Contents

1. [Game Manager](#game-manager)
2. [Player Hub Controller](#player-hub-controller)
3. [Zone Teleporter](#zone-teleporter)
4. [Mission Manager](#mission-manager)
5. [UI Manager](#ui-manager)
6. [Vehicle Controller Base](#vehicle-controller-base)
7. [K9 Controller](#k9-controller)
8. [Interactive System Controller](#interactive-system-controller)
9. [Simulation Anomaly Manager](#simulation-anomaly-manager)
10. [Badge Display Board](#badge-display-board)

## Script Details

### Game Manager

**Filename:** `GameManager.cs`

**Description:**
This script interacts with several other components:
- **UIManager:** It holds a direct reference to a `UIManager` script (assigned in the inspector or found dynamically) to control the display of loading screens during scene transitions and to show appropriate UI elements (Hub UI, Graduation UI, Main Menu UI) based on the current `GameState`. It also attempts to call a method on the UIManager to update a badge display.
- **MissionManager (or Zone-Specific Scripts):** It expects training zone scenes to contain systems (implicitly referenced, e.g., a `MissionManager`) that will call the `MissionCompleted` method on the GameManager instance when a training objective is finished. This is the primary way training outcomes influence game progression.
- **BadgeDisplayBoard:** When in the Hub state, it searches for a `BadgeDisplayBoard` component in the scene and calls its `UpdateDisplay` method to visualize which badges the player has earned.
- **Other Scripts (via Events):** Other scripts can subscribe to the `OnGameStateChanged` and `OnBadgeEarned` events exposed by the GameManager to react to significant state changes or player achievements in a loosely coupled manner.

**Implementation Details:**
- **Singleton Pattern:** The script implements a basic thread-safe singleton pattern with `DontDestroyOnLoad` to persist across scenes.
- **Asynchronous Operations:** It uses `System.Threading.Tasks` and `async`/`await` keywords in conjunction with `SceneManager.LoadSceneAsync` to perform non-blocking scene loading, allowing for control over the loading process (like waiting for 90% completion before activation).
- **PlayerPrefs for Persistence:** Earned badge status is saved and loaded using Unity's `PlayerPrefs`, which stores data in a simple key-value format.
- **State Machine:** The `GameState` enum and the `SetGameState` method function as a simple state machine, providing clear modes for the game.
- **Event System:** Utilizes C# events (`static event Action`) for broadcasting state changes and badge acquisitions, promoting decoupling between the GameManager and components that need to react to these events.
- **Inspector Configuration:** Scene names and the list of training zone scene mappings are configured directly in the Unity Inspector using public/serialized fields and a serializable struct `ZoneSceneMapping`.
- **Dynamic Dependency Finding:** If the `UIManager` is not assigned in the inspector, the script attempts to find it using `FindObjectOfType`, including re-finding it after scene loads, though relying solely on `FindObjectOfType` after scene load can be fragile if the manager isn't immediately available or located in the root.
- **Editor Utility:** Includes an editor-only context menu item (`UNITY_EDITOR` define) to reset badge data stored in PlayerPrefs for testing purposes.

---

### Player Hub Controller

**Filename:** `PlayerHubController.cs`

**Description:**
This script primarily interacts with:
- The `IInteractable` interface: It detects objects implementing this interface using raycasts and calls their `Interact()` method when the player triggers the interaction input. This allows different in-game objects (like teleporters, buttons, etc.) to define their interaction logic.
- The Unity Input System (`PlayerInput` component): It receives input values (move, look, interact) through public methods (`OnMove`, `OnLook`, `OnInteract`) that are hooked up to actions in an Input Actions Asset.
- The `CharacterController` component: It uses this component for physics-based movement and collision detection.
- The main camera: It controls the camera's pitch rotation based on player look input.
- Other scripts (commented out): There are commented-out sections showing potential interaction with a `UIManager` script to display interaction prompts, although this specific implementation is not active.

**Implementation Details:**
Critical implementation details include:
- Use of `UnityEngine.InputSystem` for input handling via `On...` callback methods tied to a `PlayerInput` component.
- Utilizes `RequireComponent` to ensure `CharacterController` and `PlayerInput` components are attached.
- Employs a `CharacterController` for movement and collision, applying calculated movement and velocity (including gravity).
- Simulates gravity and checks for ground contact using `Physics.CheckSphere`.
- Handles camera look by adjusting the local pitch of the camera transform and player body yaw rotation.
- Detects interactable objects using `Physics.Raycast` from the camera's forward direction, checking for components implementing `IInteractable`.
- Locks and hides the cursor (`Cursor.lockState = CursorLockMode.Locked`).
- Includes optional Gizmos for visualizing the interaction ray and ground check sphere in the editor.

---

### Zone Teleporter

**Filename:** `ZoneTeleporter.cs`

**Description:**
The script implements the `IInteractable` interface, which is assumed to be used by a player interaction controller (e.g., `PlayerHubController`) to identify and trigger interaction. When the `Interact()` method is called by the player controller, the script interacts with a singleton `GameManager.Instance` to request the loading of the corresponding training zone scene by calling its `LoadTrainingZone` method.

**Implementation Details:**
Requires a `Collider` component on the same GameObject, which should be set to 'Is Trigger'. It uses a serialized field (`zoneType`) to configure which training zone it leads to. It includes basic validation in `Start()` and uses an internal flag (`_isInteractable`) to prevent multiple interaction calls once a scene load is initiated. It assumes the existence of a `GameManager` class with a static `Instance` property and a `LoadTrainingZone(TrainingZoneType)` method.

---

### Mission Manager

**Filename:** `MissionManager.cs`

**Description:**
The script interacts primarily with two other singleton scripts: `UIManager` and `GameManager`. It obtains references to these scripts in its `Start` method using `UIManager.Instance` and `GameManager.Instance`. 
- It communicates with the `UIManager` to display the mission title (`uiManager.ShowMissionUI`) and to update the list and status of objectives shown to the player (`uiManager.UpdateMissionObjectives`). 
- It communicates with the `GameManager` to report the final outcome of the mission, calling `gameManager.MissionCompleted()` with the zone type and the success status (true/false). 
External scripts (or game systems) are expected to trigger mission events by calling this script's public methods, such as `StartMission`, `UpdateObjectiveProgress`, `ObjectiveCompleted`, or `FailMission`, usually in response to player actions or in-game events.

**Implementation Details:**
Key implementation details include:
- It uses a serializable `MissionObjective` class/struct to define objective parameters (ID, description, required progress, optional) and runtime state (current progress, completed status). This allows objectives to be configured in the Unity Inspector.
- A `MissionState` enum (`NotStarted`, `InProgress`, `Succeeded`, `Failed`) tracks the overall state of the mission.
- References to `UIManager` and `GameManager` are cached during `Start`.
- Objective progress is managed via the `UpdateObjectiveProgress` method, which clamps the value and checks for completion.
- `ObjectiveCompleted` provides a direct way to mark an objective as finished.
- LINQ (`FirstOrDefault`, `Where`, `All`) is used internally for finding objectives by ID and checking the completion status of required objectives.
- The `CheckMissionCompletion` helper method is called after any objective completion to see if the mission success conditions have been met, potentially triggering `CompleteMission`.
- Methods include checks to ensure they are called only when the mission is in the appropriate state (`InProgress`, `NotStarted`) and verify the existence of required references (`UIManager`, `GameManager`).
- The `GetCurrentObjectives` method returns a copy of the objectives array to prevent external modification of the internal state.

---

### UI Manager

**Filename:** `UIManager.cs`

**Description:**
This script interacts primarily with the `GameManager` by subscribing to its `OnGameStateChanged` static event. When the game state changes (e.g., from `MainMenu` to `Hub`), the `HandleGameStateChanged` method is triggered, which then calls the relevant `Show` function (like `ShowHubUI`). It also queries the `GameManager` instance (e.g., `GameManager.Instance.HasBadge`, `GameManager.Instance.GetEarnedBadgeCount`) to get data needed for updating UI elements like the badge board or end screen count. The script is designed to receive data from other systems, such as mission data (`MissionObjective[]`) which would likely come from a `MissionManager` or similar script, to populate the mission objectives list.

**Implementation Details:**
Key implementation details include:
- **Singleton Pattern:** It uses a static `Instance` property to provide global access, ensuring only one UIManager exists.
- **Event Subscription:** It subscribes to the static `GameManager.OnGameStateChanged` event in `Awake` and unsubscribes in `OnDestroy` to manage UI panel visibility automatically based on game flow.
- **Inspector References:** Relies heavily on `[SerializeField]` to link UI panel GameObjects, TextMeshProUGUI elements, Images, and a prefab (`missionObjectivePrefab`) via the Unity Inspector.
- **Dynamic UI Instantiation:** It dynamically creates UI GameObjects for mission objectives using `Instantiate(missionObjectivePrefab, missionObjectivesContainer)` and manages them in a `List<_instantiatedObjectives>`, clearing previous ones before updating.
- **Badge Board Logic:** Updates an array of `Image` components based on a boolean array indicating earned badges, assuming the array order corresponds to a `TrainingZoneType` enum.
- **Reference Validation:** Includes a `ValidateReferences` method called in `Awake` to check if essential Inspector fields have been assigned, logging warnings if they are missing.
- **Panel Hiding:** The `HideAllPanels` helper ensures only one major panel is typically active at a time.

---

### Vehicle Controller Base

**Filename:** `VehicleControllerBase.cs`

**Description:**
This script is designed to be a base class for specific vehicle controller implementations. Derived classes (e.g., `CarController`, `HoverbikeController`) inherit from `VehicleControllerBase` and provide the actual logic for processing input and applying forces by implementing the abstract methods (`HandleInput`, `FixedTick`, `ApplyPhysicsForces`). External systems, such as an input manager script, would interact with derived vehicle controllers by calling their `HandleInput` method. Other game systems, like an objective manager, can subscribe to the `OnObjectiveProgress` event (exposed by this base class) to receive notifications when a vehicle contributes to an objective, typically triggered by a derived class calling the protected `NotifyObjectiveProgress` method.

**Implementation Details:**
This is an `abstract` class and cannot be added directly to a GameObject; it must be inherited. It uses `[RequireComponent(typeof(Rigidbody))]` to enforce the presence of a Rigidbody component on any GameObject using a derived script, preventing errors. Physics updates are handled in `FixedUpdate`, which is appropriate for applying forces to a Rigidbody. The core vehicle logic is implemented in the abstract methods `HandleInput`, `FixedTick`, and `ApplyPhysicsForces`, which must be overridden by derived classes. Essential component references (`vehicleRigidbody`, `vehicleTransform`) are cached in `Awake` for performance. An event (`OnObjectiveProgress`) using C# `Action` provides a decoupled way for derived classes to communicate state changes to external systems.

---

### K9 Controller

**Filename:** `K9Controller.cs`

**Description:**
This script primarily interacts with other scripts by being controlled externally. An external manager or player controller would call its public methods (`SetDestination`, `PerformAction`) to direct the K9's behavior. It relies heavily on standard Unity components: `NavMeshAgent` for pathfinding and movement, and `Animator` for driving animations. It also provides an event (`OnActionCompleted`) that other scripts can subscribe to, allowing them to be notified when the K9 finishes a commanded action.

**Implementation Details:**
Key details include the use of `[RequireComponent(typeof(NavMeshAgent))]` and `[RequireComponent(typeof(Animator))]` to ensure necessary components are present. It defines K9 behavior using two enums: `K9ActionType` for specific tasks and `K9State` for overall status (Idle, Moving, PerformingAction). Movement uses `NavMeshAgent.SetDestination` and monitors `remainingDistance` and `velocity` to detect arrival. Actions are handled by setting animation parameters (`ActionTrigger`, `ActionIndex`) and controlled via a coroutine (`ActionExecutionCoroutine`) which includes a configurable or action-specific duration (`GetActionDuration`). The script also uses `NavMesh.SamplePosition` to validate destination points on the NavMesh.

---

### Interactive System Controller

**Filename:** `InteractiveSystemController.cs`

**Description:**
The script primarily interacts with Unity UI elements (`Text` and `InputField`) to provide visual output and receive text-based input. It depends on a `SystemConfig` ScriptableObject (defined elsewhere) to load system-specific settings like name, initial message, and potentially valid commands. It exposes a public `OnSystemEvent` Action event, allowing other scripts or game managers to subscribe and react to significant internal events such as state changes, command execution, or objective completion.

**Implementation Details:**
The script utilizes an `enum` (`SystemState`) to represent the system's operational status. It maintains a `List<string>` as a simple message log, limiting its size for display. Input from a UI InputField is captured using the `onSubmit` event listener. Command processing logic within `ProcessPlayerInput` is currently a placeholder and should be expanded or replaced to handle specific commands based on the system's requirements and configuration. Input field interactivity is automatically enabled/disabled based on the current `SystemState`. Error handling includes checks for missing UI elements and configuration.

---

### Simulation Anomaly Manager

**Filename:** `SimulationAnomalyManager.cs`

**Description:**
This script relies on AnomalyConfig ScriptableObjects (created via 'Assets > Create > Dubai Police Academy > Anomaly Configuration') to define the types and parameters of available anomalies. It interacts with game objects that have a VehicleControllerBase component by finding them in the scene and attempting to call specific methods (which need to be implemented on the VehicleControllerBase or its derivatives) to apply and reset effects like speed reduction or control disablement. It also contains commented-out examples showing potential interactions with other systems like a WeatherManager, obstacle spawning logic, a UIManager, and an audio system.

**Implementation Details:**
Anomaly configurations are stored as ScriptableObjects (AnomalyConfig) allowing for easy creation and modification in the editor.
Timed effects (specifically vehicle effects) are managed using C# Coroutines (ApplyVehicleEffectCoroutine).
The script finds the target vehicle using FindObjectOfType<VehicleControllerBase>().
It requires specific methods (e.g., SetSpeedMultiplier, SetControlEnabled) to be implemented on the VehicleControllerBase or its inheriting classes for the vehicle effects to function correctly; current implementations are placeholders with warnings.
Uses System.Linq for filtering the list of anomalies.
Supporting enums (AnomalyType, VehicleAnomalyEffect) and the AnomalyConfig class are included within the same file for demonstration but are recommended to be placed in separate files in a real project structure.

---

### Badge Display Board

**Filename:** `BadgeDisplayBoard.cs`

**Description:**
This script is primarily a visual consumer. It does not directly interact with other scripts by calling their methods or accessing their data, other than standard Unity lifecycle events (`Start`). It expects to be called by another script (likely a game manager, progress tracking system, or save/load manager) that has the knowledge of which training zones/badges have been completed. The calling script would gather the earned status (perhaps from persistent data or a game state manager) and pass it as a `bool[]` array to the `UpdateDisplay` method of this `BadgeDisplayBoard` instance. The order of elements in the input `bool[]` must match the order of GameObjects in the `badgeIcons` array.

**Implementation Details:**
Key implementation details include the reliance on the order of the `badgeIcons` array matching the order of the input `earnedBadges` array (and potentially an internal `TrainingZoneType` enum not present in this script). It performs significant validation in `InitializeBoard` and `UpdateDisplay` to catch common setup errors like null references or array size mismatches. The visual state change is achieved by directly assigning a `Material` to the `sharedMaterial` property of the `Renderer` component on each badge icon GameObject. It requires that each GameObject in the `badgeIcons` array has a `Renderer` component. The `_isInitialized` flag prevents re-initialization and ensures `UpdateDisplay` is only called after successful setup.

---

## Usage

These scripts are designed to be used in a Unity project. Import the zip file into your Unity project using one of these methods:

1. Extract the zip and copy the Assets folder to your project, merging with your existing Assets folder
2. In Unity, go to Assets > Import Package > Custom Package and select the extracted folder
