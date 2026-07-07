using UnityEngine;
using UnityEngine.InputSystem;

public class ExitRoomInteraction : MonoBehaviour
{
    public GameObject uiPrompt;         // Drag your "Press Trigger to Exit" text here
    public Transform lobbySpawnPoint;   // Drag an empty GameObject placed in the lobby here
    public GameObject player;            // Drag your main player here

    [Header("UI Canvas Management")]
    public GameObject lobbyCanvas;
    public GameObject currentRoomCanvas;

    [Header("Lobby Room Labels")]
    public GameObject[] lobbyRoomLabels;

    private bool isPlayerNearby = false;
    private CharacterController charController;

    // Left Controller Trigger
    private InputAction leftTrigger;

    void Awake()
    {
        leftTrigger = new InputAction(
            "LeftTrigger",
            InputActionType.Button,
            "<XRController>{LeftHand}/triggerPressed"
        );
    }

    void OnEnable()
    {
        leftTrigger.Enable();
    }

    void OnDisable()
    {
        leftTrigger.Disable();
    }

    void Start()
    {
        if (player != null)
        {
            charController = player.GetComponent<CharacterController>();
        }
    }

    void Update()
    {
        if (isPlayerNearby &&
            (Input.GetKeyDown(KeyCode.E) || leftTrigger.WasPressedThisFrame()))
        {
            ExitRoom();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;

            if (uiPrompt != null)
                uiPrompt.SetActive(true);
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;

            if (uiPrompt != null)
                uiPrompt.SetActive(false);
        }
    }

    void ExitRoom()
    {
        if (uiPrompt != null)
            uiPrompt.SetActive(false);

        isPlayerNearby = false;

        // Safe Teleportation
        if (charController != null)
            charController.enabled = false;

        player.transform.position = lobbySpawnPoint.position;

        if (charController != null)
            charController.enabled = true;

        // Restore UI
        if (currentRoomCanvas != null)
            currentRoomCanvas.SetActive(false);

        if (lobbyCanvas != null)
            lobbyCanvas.SetActive(true);

        RestoreLobbyRoomLabels();

        // Reset Player State
        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();

        if (playerMovement != null)
        {
            playerMovement.InitializeRoomFunctionality(0);
        }

        DoorInteraction[] roomDoors =
            Object.FindObjectsByType<DoorInteraction>(FindObjectsSortMode.None);

        foreach (DoorInteraction roomDoor in roomDoors)
        {
            roomDoor.ResetEntryState();
        }

        Debug.Log("Player exited the room and returned to the lobby safely!");
    }

    void RestoreLobbyRoomLabels()
    {
        if (lobbyRoomLabels == null)
            return;

        foreach (GameObject label in lobbyRoomLabels)
        {
            if (label != null)
                label.SetActive(true);
        }
    }
}