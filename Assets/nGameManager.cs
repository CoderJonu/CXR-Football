using UnityEngine;
using TMPro;

public class nGameManager : MonoBehaviour
{
    [Header("UI Text Displays")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI goalsLeftText;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI accuracyText;

    [Header("Match Settings")]
    public GameObject ballPrefab;
    public Transform spawnPoint; // Assign TrainingSpawnPoint here
    public float matchDuration = 300f;
    public int goalsToWin = 3;

    private float timeRemaining;
    private int goalsRemaining;
    private int shotsTaken = 0;
    private int goalsScored = 0;

    private bool isGameOver = false;
    private bool isSpawningBall = false;
    private bool isRoomActive = false;
    private float lastGoalTime = -999f;

    private GameObject currentBall;

    void Start()
    {
        timeRemaining = matchDuration;
        goalsRemaining = goalsToWin;

        if (spawnPoint != null)
        {
            Debug.Log("Training Spawn Position: " + spawnPoint.position);
        }

        nBall existingBall = Object.FindFirstObjectByType<nBall>();

        if (existingBall != null)
        {
            currentBall = existingBall.gameObject;
            ConfigureActiveBall(currentBall);
        }

        SetGameplayUIVisible(false);
        UpdateUIDisplays();
    }

    void Update()
    {
        if (!isRoomActive) return;
        if (isGameOver) return;

        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            UpdateUIDisplays();
        }
        else
        {
            timeRemaining = 0;
            EndGame(false);
        }
    }

    public void GoalScored(GameObject scoredBall)
    {
        EnsureChallengeRunning();
        if (Time.time - lastGoalTime < 0.5f) return;
        if (isGameOver || isSpawningBall) return;

        lastGoalTime = Time.time;

        goalsScored++;
        goalsRemaining--;

        Debug.Log("Goals Remaining: " + goalsRemaining);
        Debug.Log("Goals Scored: " + goalsScored);

        UpdateUIDisplays();

        if (goalsRemaining <= 0)
        {
            RemoveBall(scoredBall);
            EndGame(true);
        }
        else
        {
            ReplaceOrResetBall(scoredBall, 0.5f);
        }
    }

    public void RegisterShot(GameObject shotBall)
    {
        EnsureChallengeRunning();
        if (isGameOver || shotBall == null)
            return;

        nBall trackedBall = shotBall.GetComponent<nBall>();

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
            Vector3 spawnPos = spawnPoint != null
                ? spawnPoint.position
                : Vector3.zero;

            currentBall = Instantiate(
                ballPrefab,
                spawnPos,
                Quaternion.identity
            );

            ConfigureActiveBall(currentBall);
            NotifyBallTrackers();
        }
    }

    public void RespawnNewBall(GameObject oldBall)
    {
        EnsureChallengeRunning();
        if (isGameOver || isSpawningBall) return;

        ReplaceOrResetBall(oldBall, 0.2f);
    }

    public void BeginRoomTwoChallenge()
    {
        isRoomActive = true;
        isGameOver = false;
        isSpawningBall = false;
        lastGoalTime = -999f;
        timeRemaining = matchDuration;
        goalsRemaining = goalsToWin;
        shotsTaken = 0;
        goalsScored = 0;

        CancelInvoke(nameof(SpawnFreshBall));
        SetGameplayUIVisible(true);
        UpdateUIDisplays();

        if (currentBall == null)
            SpawnFreshBall();
        else
            ResetExistingBallToSpawn(currentBall);

        Debug.Log("Room 2 challenge started.");
    }

    public void EndRoomTwoChallenge()
    {
        isRoomActive = false;
        CancelInvoke(nameof(SpawnFreshBall));
        SetGameplayUIVisible(false);
        Debug.Log("Room 2 challenge stopped.");
    }

    public void EnsureChallengeRunning()
    {
        if (!isRoomActive && !isGameOver)
        {
            isRoomActive = true;
            SetGameplayUIVisible(timerText != null || goalsLeftText != null || accuracyText != null);
            UpdateUIDisplays();
            Debug.Log("Room 2 challenge timer is now running.");
        }
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

    void ReplaceOrResetBall(GameObject ballToReplace, float delay)
    {
        if (ballPrefab != null)
        {
            RemoveBall(ballToReplace);
            isSpawningBall = true;
            Invoke(nameof(SpawnFreshBall), delay);
            return;
        }

        ResetExistingBallToSpawn(ballToReplace);
    }

    void ResetExistingBallToSpawn(GameObject ballToReset)
    {
        if (ballToReset == null)
            ballToReset = currentBall;

        if (ballToReset == null)
            return;

        currentBall = ballToReset;

        Rigidbody ballBody = ballToReset.GetComponent<Rigidbody>();
        if (ballBody != null)
        {
            ballBody.velocity = Vector3.zero;
            ballBody.angularVelocity = Vector3.zero;
        }

        nBall trackedBall = ballToReset.GetComponent<nBall>();
        if (trackedBall != null)
            trackedBall.HasRegisteredShot = false;

        ballToReset.transform.position = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        ballToReset.transform.rotation = Quaternion.identity;

        ConfigureActiveBall(ballToReset);
        NotifyBallTrackers();
    }

    void ConfigureActiveBall(GameObject activeBall)
    {
        if (activeBall == null)
            return;

        activeBall.name = "nBall";

        try
        {
            activeBall.tag = "nBall";
        }
        catch (UnityException)
        {
            Debug.LogWarning("Tag 'nBall' is missing in Project Settings. Add it so AI can track the room 2 ball by tag.");
        }
    }

    void NotifyBallTrackers()
    {
        GoalieAI[] goalies = Object.FindObjectsByType<GoalieAI>(FindObjectsSortMode.None);
        foreach (GoalieAI goalie in goalies)
        {
            goalie.ForceRefreshBallReference();
        }

        DefenderBoardAI[] defenders = Object.FindObjectsByType<DefenderBoardAI>(FindObjectsSortMode.None);
        foreach (DefenderBoardAI defender in defenders)
        {
            defender.ForceRefreshBallReference();
        }
    }

    void UpdateUIDisplays()
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);

        if (timerText != null)
            timerText.text = $"Time Left: {minutes:00}:{seconds:00}";

        if (goalsLeftText != null)
            goalsLeftText.text = "Goals Left: " + goalsRemaining;

        if (accuracyText != null)
        {
            float accuracy = shotsTaken > 0
                ? (goalsScored / (float)shotsTaken) * 100f
                : 0f;

            accuracyText.text =
                $"Shots Taken: {shotsTaken} Goals Scored: {goalsScored} Accuracy: {accuracy:0}%";
        }
    }

    void EndGame(bool playerWon)
    {
        isGameOver = true;

        CancelInvoke(nameof(SpawnFreshBall));

        RemoveBall(currentBall);
        UpdateUIDisplays();

        if (playerWon)
        {
            Debug.Log("YOU WON!");
        }
        else
        {
            Debug.Log("YOU LOSE!");
        }
    }

    void SetGameplayUIVisible(bool visible)
    {
        SetTextAndParentsVisible(timerText, visible);
        SetTextAndParentsVisible(goalsLeftText, visible);
        SetTextAndParentsVisible(accuracyText, visible);

        if (resultText != null)
            resultText.gameObject.SetActive(false);
    }

    void SetTextAndParentsVisible(TextMeshProUGUI text, bool visible)
    {
        if (text == null)
            return;

        text.gameObject.SetActive(visible);

        if (!visible)
            return;

        Transform current = text.transform;
        while (current != null)
        {
            current.gameObject.SetActive(true);

            if (current.GetComponent<Canvas>() != null)
                break;

            current = current.parent;
        }
    }
}
