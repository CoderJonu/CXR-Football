using UnityEngine;

public class RoomOneGoalieAI : MonoBehaviour
{
    [Header("Target Tracking")]
    public Transform ballTransform;
    public Transform goalCenterMarker;

    [Header("Goal Orientation Check")]
    [Tooltip("Check this box TRUE if your Room 1 goalie needs to move Forward/Backward (Z) instead of Left/Right (X)")]
    public bool moveAlongZAxis = false;

    [Header("Movement Properties")]
    public float lateralSpeed = 6f;
    public float maxSlideDistance = 3f;
    public float saveRadius = 1.5f;

    private Vector3 startingPosition;
    private Rigidbody ballRb;
    private float reFindTimer = 0f;

    void Start()
    {
        startingPosition = transform.position;
        LocateBallReference();
    }

    public void ForceRefreshBallReference()
    {
        ballTransform = null;
        ballRb = null;
        LocateBallReference();
    }

    void Update()
    {
        // Auto-finds Room 1's ball if it gets reset or respawned
        if (ballTransform == null)
        {
            reFindTimer += Time.deltaTime;
            if (reFindTimer >= 0.1f)
            {
                LocateBallReference();
                reFindTimer = 0f;
            }
            return;
        }

        if (goalCenterMarker == null) return;

        Vector3 targetPosition = transform.position;

        if (moveAlongZAxis)
        {
            // If Room 1 is rotated sideways, move along the Z-axis
            float targetZ = ballTransform.position.z;
            float clampedZ = Mathf.Clamp(targetZ, goalCenterMarker.position.z - maxSlideDistance, goalCenterMarker.position.z + maxSlideDistance);
            targetPosition = new Vector3(transform.position.x, transform.position.y, clampedZ);
        }
        else
        {
            // Standard movement along the X-axis (matching Room 2)
            float targetX = ballTransform.position.x;
            float clampedX = Mathf.Clamp(targetX, goalCenterMarker.position.x - maxSlideDistance, goalCenterMarker.position.x + maxSlideDistance);
            targetPosition = new Vector3(clampedX, transform.position.y, transform.position.z);
        }

        // Smoothly slide the goalie to track the ball
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, lateralSpeed * Time.deltaTime);

        // Active Defending Save Radius check
        float distanceToBall = Vector3.Distance(transform.position, ballTransform.position);
        if (distanceToBall <= saveRadius && ballRb != null)
        {
            PerformSave();
        }
    }

    void LocateBallReference()
    {
        // STRICTLY searches for Room 1's ball name "Ball" or tag "Ball"
        GameObject foundBall = GameObject.Find("Ball");
        if (foundBall == null) foundBall = GameObject.FindWithTag("Ball");
        if (foundBall == null)
        {
            ball activeBall = Object.FindFirstObjectByType<ball>();
            if (activeBall != null && activeBall.enabled)
                foundBall = activeBall.gameObject;
        }

        if (foundBall != null)
        {
            ballTransform = foundBall.transform;
            ballRb = foundBall.GetComponent<Rigidbody>();
            Debug.Log("Room 1 Goalie: Successfully locked onto the active 'Ball' object.");
        }
    }

    void PerformSave()
    {
        if (ballRb == null) return;

        ballRb.velocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;

        Vector3 clearDirection = (ballTransform.position - transform.position).normalized;
        clearDirection.y = 0.2f;

        ballRb.AddForce(clearDirection * 8f, ForceMode.Impulse);
        Debug.Log("Room 1 Goalie Made a Save!");
    }
}
