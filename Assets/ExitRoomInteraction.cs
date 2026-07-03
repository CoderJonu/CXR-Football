using UnityEngine;

public class ExitRoomInteraction : MonoBehaviour
{
    public GameObject uiPrompt;         // Drag your "Press E to Exit" text here
    public Transform lobbySpawnPoint;   // Drag an empty GameObject placed in the lobby here
    public GameObject player;            // Drag your main player here

    [Header("UI Canvas Management")]
    public GameObject lobbyCanvas;       // Drag your main Lobby UI Canvas here
    public GameObject currentRoomCanvas; // Drag THIS room's specific Canvas here to turn it off

    [Header("Lobby Room Labels")]
    public GameObject[] lobbyRoomLabels;

    private bool isPlayerNearby = false;
    private CharacterController charController;

    void Start()
    {
        if (player != null)
        {
            charController = player.GetComponent<CharacterController>();
        }
    }

    void Update()
    {
        // If the player is near the exit zone and presses E
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.E))
        {
            ExitRoom();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            if (uiPrompt != null) uiPrompt.SetActive(true);
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
            if (uiPrompt != null) uiPrompt.SetActive(false);
        }
    }

    void ExitRoom()
    {
        if (uiPrompt != null) uiPrompt.SetActive(false);
        isPlayerNearby = false;

        // 1. Safe Teleportation back to the Lobby
        if (charController != null) charController.enabled = false;
        player.transform.position = lobbySpawnPoint.position;
        if (charController != null) charController.enabled = true;

        // 2. UI Reset: Hide the room gameplay screen, show the lobby text labels again
        if (currentRoomCanvas != null) currentRoomCanvas.SetActive(false);
        if (lobbyCanvas != null) lobbyCanvas.SetActive(true);
        RestoreLobbyRoomLabels();

        // 3. Reset the internal room tracking states inside your Player script
        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.InitializeRoomFunctionality(0); // 0 sets them back to Lobby/Freeplay mode
        }

        DoorInteraction[] roomDoors = Object.FindObjectsByType<DoorInteraction>(FindObjectsSortMode.None);
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
