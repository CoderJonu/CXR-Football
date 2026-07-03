using UnityEngine;
using UnityEngine.InputSystem;
using TMPro; // Added for TextMeshPro UI Support

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Components")]
    public CharacterController controller;
    public Animator anim;

    [Header("Foot Hardware Tracking")]
    public Transform rightFootController;
    public float kickVelocityThreshold = 1.8f;

    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float rotationSpeed = 150f;
    public float turnAcceleration = 360f;
    public float gravity = -9.81f;

    [Header("Bot Breaking Friction")]
    public float brakingFriction = 15f;
    public float accelerationRate = 10f;

    [Header("Football Striking Settings")]
    public float maxKickPower = 12.0f;
    public float kickUpwardForce = 3.5f;
    public float softDribblePower = 3.0f;

    [Header("Input Settings")]
    public bool invertJoystickForward = false;
    public float joystickDeadzone = 0.15f;

    [Header("Room Functionality Layout")]
    public GameObject roomTwoCanvas;       // Drag your Room 2 Overlay UI Canvas here
    public TextMeshProUGUI timerText;      // Drag your UI Timer Text object here
    public TextMeshProUGUI goalCounterText; // Drag your UI Goal Text object here
    public float roomTwoTimeLimit = 60f;   // Set allowed time limit
    public int targetGoalsToWin = 5;       // Required goals to finish

    private Vector3 currentVelocity;
    private Vector3 verticalVelocity;
    private Vector3 lastFootPosition;
    private float physicalFootSpeed;
    private float currentTurnSpeed;
    private GameManager gameManager;
    private nGameManager roomTwoGameManager;

    // Room tracking systems
    private int activeRoom = 0;            // 0 = Lobby/Freeplay, 1 = Room 1, 2 = Room 2
    private float currentRoomTimer;
    private bool isTimerRunning = false;
    private int currentGoalsScored = 0;

    public InputAction rawRightThumbstick;
    public InputAction rawRightTrigger;

    public bool IsPhysicallyMoving { get; private set; }
    public bool IsVrKicking { get; private set; }

    void Awake()
    {
        rawRightThumbstick = new InputAction(
            "RightStick",
            InputActionType.Value,
            "<XRController>{RightHand}/thumbstick"
        );

        rawRightTrigger = new InputAction(
            "RightTrigger",
            InputActionType.Button,
            "<XRController>{RightHand}/triggerPressed"
        );
    }

    void OnEnable()
    {
        rawRightThumbstick.Enable();
        rawRightTrigger.Enable();
    }

    void OnDisable()
    {
        rawRightThumbstick.Disable();
        rawRightTrigger.Disable();
    }

    void Start()
    {
        if (controller == null || controller.gameObject != gameObject)
            controller = GetComponent<CharacterController>();

        PlayerController legacyController = GetComponent<PlayerController>();
        if (legacyController != null)
            legacyController.enabled = false;

        if (anim == null)
            anim = GetComponent<Animator>();

        if (rightFootController != null)
            lastFootPosition = rightFootController.position;

        gameManager = Object.FindFirstObjectByType<GameManager>();
        roomTwoGameManager = Object.FindFirstObjectByType<nGameManager>();

        // Ensure Room 2 UI features are hidden when spawning at startup
        if (roomTwoCanvas != null)
            roomTwoCanvas.SetActive(false);
    }

    void Update()
    {
        // --- ROOM TIMER LOOP INTEGRATION ---
        if (activeRoom == 2 && isTimerRunning && roomTwoGameManager == null)
        {
            if (currentRoomTimer > 0)
            {
                currentRoomTimer -= Time.deltaTime;
                UpdateRoomTwoDisplay();
            }
            else
            {
                currentRoomTimer = 0;
                isTimerRunning = false;
                if (timerText != null) timerText.text = "Time's Up!";
                OnRoomTwoTimeExpired();
            }
        }

        // --- ORIGINAL MOVEMENT MECHANICS LOOP ---
        if (rightFootController != null && Time.deltaTime > 0f)
        {
            Vector3 footDisplacement = rightFootController.position - lastFootPosition;
            physicalFootSpeed = footDisplacement.magnitude / Time.deltaTime;
            lastFootPosition = rightFootController.position;
        }

        Vector2 rightJoystick = rawRightThumbstick.ReadValue<Vector2>();
        IsVrKicking = rawRightTrigger.IsPressed();

        float forwardInput = ApplyDeadzone(invertJoystickForward ? -rightJoystick.y : rightJoystick.y);
        float turnInput = ApplyDeadzone(rightJoystick.x);

        // Keyboard fallback
        if (Input.GetKey(KeyCode.W)) forwardInput = 1f;
        if (Input.GetKey(KeyCode.S)) forwardInput = -1f;
        if (Input.GetKey(KeyCode.A)) turnInput = -1f;
        if (Input.GetKey(KeyCode.D)) turnInput = 1f;

        IsPhysicallyMoving = Mathf.Abs(forwardInput) > 0f;

        if (anim != null)
        {
            anim.SetBool("isRunning", IsPhysicallyMoving);

            if (IsVrKicking || Input.GetKeyDown(KeyCode.Space))
                anim.SetTrigger("isKicking");
        }

        float targetTurnSpeed = turnInput * rotationSpeed;
        currentTurnSpeed = Mathf.MoveTowards(
            currentTurnSpeed,
            targetTurnSpeed,
            turnAcceleration * Time.deltaTime
        );
        transform.Rotate(0f, currentTurnSpeed * Time.deltaTime, 0f);

        Vector3 targetDirection = transform.forward * forwardInput;

        if (targetDirection.magnitude > 0.05f)
        {
            currentVelocity = Vector3.MoveTowards(
                currentVelocity,
                targetDirection.normalized * walkSpeed,
                accelerationRate * Time.deltaTime
            );
        }
        else
        {
            currentVelocity = Vector3.MoveTowards(
                currentVelocity,
                Vector3.zero,
                brakingFriction * Time.deltaTime
            );
        }

        controller.Move(currentVelocity * Time.deltaTime);

        if (controller.isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f;
        }

        verticalVelocity.y += gravity * Time.deltaTime;
        controller.Move(verticalVelocity * Time.deltaTime);
    }

    float ApplyDeadzone(float value)
    {
        return Mathf.Abs(value) < joystickDeadzone ? 0f : value;
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!hit.gameObject.name.ToLower().Contains("ball") && hit.collider.attachedRigidbody == null)
            return;

        Rigidbody ballBody = hit.collider.attachedRigidbody;

        if (ballBody == null || ballBody.isKinematic)
            return;

        ballBody.velocity = Vector3.zero;
        ballBody.angularVelocity = Vector3.zero;

        Vector3 pushDir = transform.forward;
        bool isIntentionalKick = IsVrKicking
            || Input.GetKey(KeyCode.Space)
            || physicalFootSpeed > kickVelocityThreshold;

        if (isIntentionalKick)
        {
            if (gameManager != null && ballBody.GetComponent<ball>() != null)
                gameManager.RegisterShot(ballBody.gameObject);

            if (roomTwoGameManager != null && ballBody.GetComponent<nBall>() != null)
                roomTwoGameManager.RegisterShot(ballBody.gameObject);

            pushDir.y = kickUpwardForce / maxKickPower;

            float calculatedPower = Mathf.Clamp(
                physicalFootSpeed * 2.5f,
                softDribblePower,
                maxKickPower
            );

            if (IsVrKicking || Input.GetKey(KeyCode.Space))
                calculatedPower = maxKickPower;

            ballBody.AddForce(pushDir.normalized * calculatedPower, ForceMode.Impulse);
            Debug.Log("VR Kick Executed!");
        }
        else
        {
            pushDir.y = 0f;
            ballBody.AddForce(pushDir.normalized * softDribblePower, ForceMode.Impulse);
        }
    }

    // --- NEW CORE ROOM INTERFACE FUNCTIONS ---

    // Triggered directly from your working DoorInteraction layout code setup
    public void InitializeRoomFunctionality(int roomNumber)
    {
        activeRoom = roomNumber;

        if (activeRoom == 1)
        {
            isTimerRunning = false;
            if (roomTwoCanvas != null) roomTwoCanvas.SetActive(false);
            if (roomTwoGameManager != null) roomTwoGameManager.EndRoomTwoChallenge();
            if (gameManager != null) gameManager.BeginFreePlaySession();
            Debug.Log("Entered Room 1: Free play state initiated.");
        }
        else if (activeRoom == 2)
        {
            currentGoalsScored = 0;
            currentRoomTimer = roomTwoTimeLimit;
            isTimerRunning = roomTwoGameManager == null;

            if (roomTwoCanvas != null)
                roomTwoCanvas.SetActive(true);

            if (gameManager != null) gameManager.EndFreePlaySession();

            if (roomTwoGameManager != null)
                roomTwoGameManager.BeginRoomTwoChallenge();
            else
                UpdateRoomTwoDisplay();

            Debug.Log("Entered Room 2: Countdown and goal systems initialized.");
        }
        else
        {
            isTimerRunning = false;
            currentGoalsScored = 0;
            currentRoomTimer = 0f;

            if (roomTwoCanvas != null)
                roomTwoCanvas.SetActive(false);

            if (roomTwoGameManager != null)
                roomTwoGameManager.EndRoomTwoChallenge();

            if (gameManager != null)
                gameManager.EndFreePlaySession();

            Debug.Log("Returned to lobby/freeplay state.");
        }
    }

    // Call this public method whenever your scoring logic detects a goal event!
    public void IncreaseRoomTwoGoalCount()
    {
        if (roomTwoGameManager != null) return;
        if (activeRoom != 2 || !isTimerRunning) return;

        currentGoalsScored++;
        UpdateRoomTwoDisplay();

        if (currentGoalsScored >= targetGoalsToWin)
        {
            isTimerRunning = false;
            OnRoomTwoChallengeCompleted();
        }
    }

    void UpdateRoomTwoDisplay()
    {
        if (timerText != null)
            timerText.text = "Time Left: " + Mathf.CeilToInt(currentRoomTimer) + "s";

        if (goalCounterText != null)
            goalCounterText.text = "Goals: " + currentGoalsScored + " / " + targetGoalsToWin;
    }

    void OnRoomTwoTimeExpired()
    {
        Debug.Log("Challenge Failed! Time limit reached.");
        // Add failure conditions here (e.g., reset room ball positions, popup panel)
    }

    void OnRoomTwoChallengeCompleted()
    {
        Debug.Log("Challenge Successful! Target goals secured.");
        if (goalCounterText != null) goalCounterText.text = "Victory!";
        // Add completion features here (e.g., unlock exit door barriers, play audio)
    }
}
