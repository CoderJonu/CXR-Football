using UnityEngine;

public class DefenderBoardAI : MonoBehaviour
{
    [Header("Inspector Spawn Assignments (1 to 10)")]
    [Tooltip("Drag the matching empty marker from your Pattern A folder here")]
    public Transform spotPatternA;
    [Tooltip("Drag the matching empty marker from your Pattern B folder here")]
    public Transform spotPatternB;
    [Tooltip("Drag the matching empty marker from your Pattern C folder here")]
    public Transform spotPatternC;

    [Header("Creeping/Aggression Properties")]
    public float shiftSlideSpeed = 6f;       // How fast they move to a new formation line
    public float forwardCreepSpeed = 0.4f;   // Slow pressure speed approaching the player/ball
    public float maxCreepDistanceZ = 8f;     // Maximum distance allowed to march forward down the field

    [Header("Interception Rotation")]
    public float interceptionDistance = 3f;
    public float rotationSpeed = 540f;
    public float closeBallRotationOffset = 90f;

    private Transform currentMarkerTarget;
    private Transform ballTransform;
    private float calculatedZOffset = 0f;
    private float reFindTimer = 0f;

    void Start()
    {
        LocateBallReference();
        UpdateActivePatternTarget();

        if (currentMarkerTarget != null)
        {
            transform.position = currentMarkerTarget.position;
        }
    }

    public void UpdateActivePatternTarget()
    {
        // Fallback safety to force-link the manager if initialization loads out of order
        if (DefensiveSystemManager.Instance == null)
        {
            Object.FindFirstObjectByType<DefensiveSystemManager>();
        }

        if (DefensiveSystemManager.Instance == null) return;

        int activeIndex = DefensiveSystemManager.Instance.currentActivePattern;

        if (activeIndex == 0) currentMarkerTarget = spotPatternA;
        else if (activeIndex == 1) currentMarkerTarget = spotPatternB;
        else if (activeIndex == 2) currentMarkerTarget = spotPatternC;

        if (currentMarkerTarget == null)
            currentMarkerTarget = spotPatternA;

        calculatedZOffset = 0f;
    }

    public void ResetToActivePattern()
    {
        UpdateActivePatternTarget();
        ForceRefreshBallReference();

        if (currentMarkerTarget == null)
            return;

        transform.position = currentMarkerTarget.position;
        transform.rotation = currentMarkerTarget.rotation;
    }

    public void ForceRefreshBallReference()
    {
        ballTransform = null;
        LocateBallReference();
    }

    void Update()
    {
        // BUG FIX: Intelligently auto-find the new ball if it gets reset after a goal
        if (ballTransform == null)
        {
            reFindTimer += Time.deltaTime;
            if (reFindTimer >= 0.1f) // Scan rapidly every 0.1s to prevent layout bugs
            {
                LocateBallReference();
                reFindTimer = 0f;
            }
            return;
        }

        if (currentMarkerTarget == null) return;

        // 1. CREEPING MARCH MECHANIC: Slowly advance down the field towards the ball
        if (transform.position.z > ballTransform.position.z)
        {
            if (Mathf.Abs(calculatedZOffset) < maxCreepDistanceZ)
            {
                calculatedZOffset += forwardCreepSpeed * Time.deltaTime;
            }
        }

        // 2. POSITION CALCULATOR: Move towards the base anchor point + forward creep modifier
        Vector3 baseAnchor = currentMarkerTarget.position;
        Vector3 dynamicTargetPosition = new Vector3(baseAnchor.x, transform.position.y, baseAnchor.z - calculatedZOffset);

        transform.position = Vector3.MoveTowards(transform.position, dynamicTargetPosition, shiftSlideSpeed * Time.deltaTime);

        // 3. ROTATION: Track the ball, then turn sideways up close to block the shot lane
        Vector3 lookDirection = ballTransform.position - transform.position;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);

            if (lookDirection.magnitude <= interceptionDistance)
                targetRotation *= Quaternion.Euler(0f, closeBallRotationOffset, 0f);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    void LocateBallReference()
    {
        // VR SAFE FIX: Search for your ball using the exact name "nBall" or tag "nBall"
        GameObject foundBall = GameObject.Find("nBall");
        if (foundBall == null) foundBall = GameObject.FindWithTag("nBall");
        if (foundBall == null)
        {
            nBall activeBall = Object.FindFirstObjectByType<nBall>();
            if (activeBall != null)
                foundBall = activeBall.gameObject;
        }

        if (foundBall != null)
        {
            ballTransform = foundBall.transform;
        }
    }
}
