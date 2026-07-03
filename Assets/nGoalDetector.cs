using UnityEngine;
using TMPro; // Use this if you're using TextMeshPro

public class nGoalDetector : MonoBehaviour
{
    public GameObject goalTextUI; // Drag your 'Goal!' text object here in the Inspector
    private GameManager regularGameManager;
    private nGameManager gameManager;

    void Start()
    {
        regularGameManager = Object.FindFirstObjectByType<GameManager>();
        gameManager = Object.FindFirstObjectByType<nGameManager>();
        Debug.Log("Manager Found: " + gameManager);

        // This makes sure the "Goal!" text is hidden the second the game runs
        if (goalTextUI != null)
        {
            goalTextUI.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // This will print the name of ANY object that touches the goal
        Debug.Log("Something hit the goal: " + other.gameObject.name);

        GameObject scoredBall = GetScoredBall(other);

        if (scoredBall != null)
        {
            Debug.Log("GOAL SCORED!");

            bool isRoomTwoBall = scoredBall.GetComponent<nBall>() != null;

            if (isRoomTwoBall)
            {
                // --- 1. UPDATE ROOM 2 GOAL COUNTER ON PLAYER CANVAS ---
                PlayerMovement player = Object.FindFirstObjectByType<PlayerMovement>();
                if (player != null)
                {
                    player.IncreaseRoomTwoGoalCount();
                }

                // --- 2. SWITCH 10 DEFENDER BOARDS TO THE NEXT BLUE LOCK PATTERN ---
                if (DefensiveSystemManager.Instance != null)
                {
                    DefensiveSystemManager.Instance.CycleDefensivePattern();
                }
                else
                {
                    Debug.LogWarning("DefensiveSystemManager is missing from the scene! Make sure it is attached to an empty GameObject.");
                }
            }

            // --- ORIGINAL GAME MANAGER CALLS ---
            if (isRoomTwoBall && gameManager == null)
                gameManager = Object.FindFirstObjectByType<nGameManager>();

            if (isRoomTwoBall && gameManager != null)
            {
                gameManager.GoalScored(scoredBall);

                Debug.Log("GoalScored() called successfully.");
            }
            else if (!isRoomTwoBall)
            {
                if (regularGameManager == null)
                    regularGameManager = Object.FindFirstObjectByType<GameManager>();

                if (regularGameManager != null)
                    regularGameManager.GoalScored(scoredBall);
            }

            // --- ORIGINAL CELEBRATION TEXT CONTROL ---
            if (goalTextUI != null)
            {
                Debug.Log("Displaying GOAL text.");

                goalTextUI.SetActive(true);
                Debug.Log("Goal text active state: " + goalTextUI.activeSelf);

                Invoke(nameof(HideGoalText), 5f);
            }
            else
            {
                Debug.LogWarning("goalTextUI is NOT assigned!");
            }
        }
    }

    GameObject GetScoredBall(Collider other)
    {
        if (HasTag(other.gameObject, "nBall"))
            return other.attachedRigidbody != null
                ? other.attachedRigidbody.gameObject
                : other.gameObject;

        if (other.attachedRigidbody != null && other.attachedRigidbody.GetComponent<nBall>() != null)
            return other.attachedRigidbody.gameObject;

        if (other.attachedRigidbody != null && other.attachedRigidbody.GetComponent<ball>() != null)
            return other.attachedRigidbody.gameObject;

        nBall ball = other.GetComponentInParent<nBall>();
        if (ball != null)
            return ball.gameObject;

        ball regularBall = other.GetComponentInParent<ball>();
        return regularBall != null ? regularBall.gameObject : null;
    }

    bool HasTag(GameObject target, string tagName)
    {
        try
        {
            return target.CompareTag(tagName);
        }
        catch (UnityException)
        {
            return false;
        }
    }

    void HideGoalText()
    {
        if (goalTextUI != null)
        {
            goalTextUI.SetActive(false);
        }
    }
}
