using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("UI Text Displays")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI goalsLeftText;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI accuracyText;

    [Header("Match Settings")]
    public GameObject ballPrefab; // Put your clean ball asset template here
    public Vector3 kickOffPosition; // Coordinates where the ball spawns
    public bool freePlayMode = true;

    private const float MatchDuration = 240f; // 4 minutes total
    private const int GoalsToWin = 10;

    private float timeRemaining = MatchDuration;
    private int goalsRemaining = GoalsToWin;
    private int shotsTaken = 0;
    private int goalsScored = 0;
    private bool isGameOver = false;
    private bool isSpawningBall = false;
    private bool isSessionActive = false;
    private float lastGoalTime = -999f;
    private GameObject currentBall;

    void Start()
    {
        // Save the start position based on where you place this in the scene
        ball existingBall = Object.FindFirstObjectByType<ball>();
        if (existingBall != null)
        {
            currentBall = existingBall.gameObject;
            kickOffPosition = existingBall.transform.position;
        }

        if (resultText != null)
            resultText.gameObject.SetActive(false);

        SetSessionUIVisible(false);
    }

    void Update()
    {
        if (freePlayMode) return;
        if (isGameOver) return;

        // Run down the match timer clock
        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            UpdateUIDisplays();
        }
        else
        {
            timeRemaining = 0;
            EndGame(false); // Lost due to timeout
        }
    }

    public void GoalScored(GameObject scoredBall)
    {
        if (Time.time - lastGoalTime < 0.5f) return;
        if (isGameOver || isSpawningBall) return;

        lastGoalTime = Time.time;
        RemoveBall(scoredBall);

        goalsScored++;
        if (!freePlayMode)
            goalsRemaining--; // Reduce targets left

        UpdateUIDisplays();

        if (!freePlayMode && goalsRemaining <= 0)
        {
            EndGame(true); // Won the game
        }
        else
        {
            // Call the spawn loop for a fresh physical ball object
            isSpawningBall = true;
            Invoke(nameof(SpawnFreshBall), 0.5f);
        }
    }

    public void RegisterShot(GameObject shotBall)
    {
        if (isGameOver || shotBall == null)
            return;

        ball trackedBall = shotBall.GetComponent<ball>();
        if (trackedBall != null && trackedBall.HasRegisteredShot)
            return;

        if (trackedBall != null)
            trackedBall.HasRegisteredShot = true;

        shotsTaken++;
        UpdateUIDisplays();
    }

    void SpawnFreshBall()
    {
        isSpawningBall = false;

        if (ballPrefab != null && !isGameOver)
        {
            currentBall = Instantiate(ballPrefab, kickOffPosition, Quaternion.identity);
        }
    }

    public void RespawnNewBall(GameObject oldBall)
    {
        if (isGameOver || isSpawningBall) return;

        // Out of bounds handler: Destroy old and drop new
        RemoveBall(oldBall);
        isSpawningBall = true;
        Invoke(nameof(SpawnFreshBall), 0.2f);
    }

    public void BeginFreePlaySession()
    {
        isSessionActive = true;
        isGameOver = false;
        isSpawningBall = false;
        lastGoalTime = -999f;
        shotsTaken = 0;
        goalsScored = 0;
        goalsRemaining = GoalsToWin;
        timeRemaining = MatchDuration;

        CancelInvoke(nameof(SpawnFreshBall));

        if (currentBall == null)
            SpawnFreshBall();
        else
            ResetExistingBallToKickOff(currentBall);

        UpdateUIDisplays();
    }

    public void EndFreePlaySession()
    {
        CancelInvoke(nameof(SpawnFreshBall));
        isSpawningBall = false;
        isSessionActive = false;
        SetSessionUIVisible(false);
    }

    void RemoveBall(GameObject ballToRemove)
    {
        if (ballToRemove == null)
            ballToRemove = currentBall;

        if (ballToRemove != null)
        {
            if (ballToRemove == currentBall)
                currentBall = null;

            Destroy(ballToRemove);
        }
    }

    void ResetExistingBallToKickOff(GameObject ballToReset)
    {
        if (ballToReset == null)
            return;

        Rigidbody ballBody = ballToReset.GetComponent<Rigidbody>();
        if (ballBody != null)
        {
            ballBody.velocity = Vector3.zero;
            ballBody.angularVelocity = Vector3.zero;
        }

        ball trackedBall = ballToReset.GetComponent<ball>();
        if (trackedBall != null)
            trackedBall.HasRegisteredShot = false;

        ballToReset.transform.position = kickOffPosition;
        ballToReset.transform.rotation = Quaternion.identity;
        currentBall = ballToReset;
    }

    void UpdateUIDisplays()
    {
        // Format decimal time into a readable MM:SS layout
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);

        if (timerText != null)
        {
            timerText.gameObject.SetActive(!freePlayMode);
            if (!freePlayMode)
                timerText.text = string.Format("Time Left: {0:00}:{1:00}", minutes, seconds);
        }

        if (goalsLeftText != null)
        {
            goalsLeftText.gameObject.SetActive(!freePlayMode);
            if (!freePlayMode)
                goalsLeftText.text = "Goals Needed: " + goalsRemaining;
        }

        if (accuracyText == null && isSessionActive)
            accuracyText = CreateAccuracyText();

        if (accuracyText != null)
        {
            accuracyText.gameObject.SetActive(isSessionActive);

            float accuracy = shotsTaken > 0
                ? (goalsScored / (float)shotsTaken) * 100f
                : 0f;

            accuracyText.text = string.Format(
                "Shots Taken: {0}  Goals Scored: {1}  Accuracy: {2:0}%",
                shotsTaken,
                goalsScored,
                accuracy
            );
        }
    }

    void SetSessionUIVisible(bool visible)
    {
        if (timerText != null)
            timerText.gameObject.SetActive(visible && !freePlayMode);

        if (goalsLeftText != null)
            goalsLeftText.gameObject.SetActive(visible && !freePlayMode);

        if (accuracyText != null)
            accuracyText.gameObject.SetActive(visible);

        if (resultText != null)
            resultText.gameObject.SetActive(false);
    }

    void EndGame(bool playerWon)
    {
        isGameOver = true;
        CancelInvoke(nameof(SpawnFreshBall));
        RemoveBall(currentBall);

        if (playerWon)
        {
            Debug.Log("VICTORY! You scored 10 goals in time.");
            ShowResult(BuildResultMessage("YOU WON!"));
        }
        else
        {
            Debug.Log("GAME OVER! Time ran out.");
            ShowResult(BuildResultMessage("YOU LOSE!"));
        }
    }

    string BuildResultMessage(string result)
    {
        float accuracy = shotsTaken > 0
            ? (goalsScored / (float)shotsTaken) * 100f
            : 0f;

        return string.Format(
            "{0}\nShots: {1}  Goals: {2}  Accuracy: {3:0}%",
            result,
            shotsTaken,
            goalsScored,
            accuracy
        );
    }

    void ShowResult(string message)
    {
        if (resultText == null)
            resultText = CreateResultText();

        if (resultText != null)
        {
            resultText.text = message;
            resultText.gameObject.SetActive(true);
        }
    }

    TextMeshProUGUI CreateResultText()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
            return null;

        GameObject resultObject = new GameObject("ResultText");
        resultObject.transform.SetParent(canvas.transform, false);

        TextMeshProUGUI text = resultObject.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 0.18f;
        text.color = Color.yellow;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(1.1f, 0.35f);

        return text;
    }

    TextMeshProUGUI CreateAccuracyText()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
            return null;

        GameObject accuracyObject = new GameObject("AccuracyText");
        accuracyObject.transform.SetParent(canvas.transform, false);

        TextMeshProUGUI text = accuracyObject.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 0.07f;
        text.color = Color.black;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 0.18f);
        rect.sizeDelta = new Vector2(1.4f, 0.14f);

        return text;
    }
}
