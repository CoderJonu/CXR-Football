using UnityEngine;
using TMPro; // Use this if you're using TextMeshPro

public class GoalDetector : MonoBehaviour
{
    public GameObject goalTextUI; // Drag your 'Goal!' text object here in the Inspector
    private GameManager gameManager;
    private nGameManager roomTwoGameManager;

    void Start()
    {
        gameManager = Object.FindFirstObjectByType<GameManager>();
        roomTwoGameManager = Object.FindFirstObjectByType<nGameManager>();

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
            Debug.Log("The Ball tag was detected!");

            if (scoredBall.GetComponent<nBall>() != null)
            {
                if (roomTwoGameManager == null)
                    roomTwoGameManager = Object.FindFirstObjectByType<nGameManager>();

                if (roomTwoGameManager != null)
                    roomTwoGameManager.GoalScored(scoredBall);
            }
            else if (gameManager != null)
            {
                gameManager.GoalScored(scoredBall);
            }

            if (goalTextUI != null)
            {
                goalTextUI.SetActive(true);
                Invoke(nameof(HideGoalText), 3f);
            }
        }
    }

    GameObject GetScoredBall(Collider other)
    {
        GameObject attachedObject = other.attachedRigidbody != null
            ? other.attachedRigidbody.gameObject
            : other.gameObject;

        if (attachedObject.GetComponent<ball>() != null || attachedObject.GetComponent<nBall>() != null)
            return attachedObject;

        ball regularBall = other.GetComponentInParent<ball>();
        if (regularBall != null)
            return regularBall.gameObject;

        nBall roomTwoBall = other.GetComponentInParent<nBall>();
        if (roomTwoBall != null)
            return roomTwoBall.gameObject;

        if (HasTag(other.gameObject, "Ball") || HasTag(other.gameObject, "nBall"))
            return attachedObject;

        return null;
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
