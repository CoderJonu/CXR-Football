using UnityEngine;
using TMPro;

public class nGameManager : MonoBehaviour
{
    [Header("UI Text Displays")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI goalsLeftText;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI accuracyText;
    public TextMeshProUGUI roomLabelText;

    [Header("Match Settings")]
    public GameObject ballPrefab;
    public Transform spawnPoint; // Assign TrainingSpawnPoint here
    public string roomTwoSpawnPointName = "Room2SpawnPoint";
    public float matchDuration = 300f;
    public int goalsToWin = 3;
    public bool keepRoomActiveAfterResult = true;

    private float timeRemaining;
    private int goalsRemaining;
    private int shotsTaken = 0;
    private int goalsScored = 0;

    private bool isGameOver = false;
    private bool isSpawningBall = false;
    private bool isRoomActive = false;
    private float lastGoalTime = -999f;

    private GameObject currentBall;
    private int lastScoredBallId = 0;

    void Start()
    {
        ResolveRoomTwoSpawnPoint();
        timeRemaining = matchDuration;
        goalsRemaining = goalsToWin;

        if (spawnPoint != null)
        {
            Debug.Log("Training Spawn Position: " + spawnPoint.position);
        }

        nBall existingBall = FindRoomTwoBall();

        if (existingBall != null)
        {
            currentBall = existingBall.gameObject;
            ConfigureActiveBall(currentBall);
        }

        EnsureGameplayTextReferences();
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

        if (scoredBall != null)
        {
            int scoredBallId = scoredBall.GetInstanceID();
            if (scoredBallId == lastScoredBallId)
                return;

            lastScoredBallId = scoredBallId;
        }

        lastGoalTime = Time.time;

        goalsScored++;
        goalsRemaining = Mathf.Max(0, goalsRemaining - 1);

        if (DefensiveSystemManager.Instance != null)
        {
            DefensiveSystemManager.Instance.CycleDefensivePattern();
        }
        else
        {
            Debug.LogWarning("DefensiveSystemManager is missing from the scene! Make sure it is attached to an empty GameObject.");
        }

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
            ResolveRoomTwoSpawnPoint();

            Vector3 spawnPos = spawnPoint != null
                ? spawnPoint.position
                : Vector3.zero;

            currentBall = Instantiate(
                ballPrefab,
                spawnPos,
                Quaternion.identity
            );

            ConfigureActiveBall(currentBall);
            lastScoredBallId = 0;
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
        ResolveRoomTwoSpawnPoint();
        isRoomActive = true;
        isGameOver = false;
        isSpawningBall = false;
        lastGoalTime = -999f;
        timeRemaining = matchDuration;
        goalsRemaining = goalsToWin;
        shotsTaken = 0;
        goalsScored = 0;

        CancelInvoke(nameof(SpawnFreshBall));
        EnsureGameplayTextReferences();
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
            EnsureGameplayTextReferences();
            SetGameplayUIVisible(true);
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
        ResolveRoomTwoSpawnPoint();

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
        lastScoredBallId = 0;
        NotifyBallTrackers();
    }

    void ConfigureActiveBall(GameObject activeBall)
    {
        if (activeBall == null)
            return;

        activeBall.name = "nBall";

        ball roomOneBall = activeBall.GetComponent<ball>();
        if (roomOneBall != null)
            roomOneBall.enabled = false;

        nBall roomTwoBall = activeBall.GetComponent<nBall>();
        if (roomTwoBall == null)
            roomTwoBall = activeBall.AddComponent<nBall>();

        roomTwoBall.enabled = true;
        roomTwoBall.HasRegisteredShot = false;

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

        if (roomLabelText != null)
            roomLabelText.text = "Room 2 Challenge";

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
                $"Kicks: {shotsTaken}  Goals: {goalsScored}  Accuracy: {accuracy:0}%";
        }
    }

    void ResolveRoomTwoSpawnPoint()
    {
        if (spawnPoint != null && spawnPoint.name == roomTwoSpawnPointName)
            return;

        GameObject namedSpawnPoint = GameObject.Find(roomTwoSpawnPointName);
        if (namedSpawnPoint != null)
        {
            spawnPoint = namedSpawnPoint.transform;
            return;
        }

        if (spawnPoint == null)
            Debug.LogWarning("Room 2 spawn point is missing. Create/assign a Transform named " + roomTwoSpawnPointName + ".");
    }

    nBall FindRoomTwoBall()
    {
        ResolveRoomTwoSpawnPoint();

        nBall[] roomTwoBalls = Object.FindObjectsByType<nBall>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (roomTwoBalls.Length == 0)
            return null;

        if (spawnPoint == null)
            return roomTwoBalls[0];

        nBall nearestBall = null;
        float nearestDistance = float.MaxValue;

        foreach (nBall candidate in roomTwoBalls)
        {
            float distance = Vector3.SqrMagnitude(candidate.transform.position - spawnPoint.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestBall = candidate;
            }
        }

        return nearestBall;
    }

    void EndGame(bool playerWon)
    {
        isGameOver = true;
        isRoomActive = false;

        CancelInvoke(nameof(SpawnFreshBall));

        RemoveBall(currentBall);
        UpdateUIDisplays();

        if (playerWon)
        {
            Debug.Log("YOU WON!\nPress E to Exit");
            ShowResult(BuildResultMessage("YOU WON!\nPress E to Exit"));
        }
        else
        {
            Debug.Log("YOU LOSE!\nPress E to Exit");
            ShowResult(BuildResultMessage("YOU LOSE!\nPress E to Exit"));
        }

        if (keepRoomActiveAfterResult)
            ResetDefendersToActivePattern();
    }

    void SetGameplayUIVisible(bool visible)
    {
        EnsureGameplayTextReferences();
        SetTextAndParentsVisible(roomLabelText, visible);
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

    string BuildResultMessage(string result)
    {
        float accuracy = shotsTaken > 0
            ? (goalsScored / (float)shotsTaken) * 100f
            : 0f;

        return string.Format(
            "{0}\nKicks: {1}  Goals: {2}/{3}  Accuracy: {4:0}%",
            result,
            shotsTaken,
            goalsScored,
            goalsToWin,
            accuracy
        );
    }

    void ShowResult(string message)
    {
        if (resultText == null || resultText.name.ToLower().Contains("goaltext"))
            resultText = CreateGameplayText("Room2ResultText", new Vector2(0f, -0.18f), new Vector2(1.6f, 0.35f), 0.12f, Color.yellow);

        if (resultText == null)
            return;

        resultText.text = message;
        resultText.alignment = TextAlignmentOptions.Center;
        resultText.gameObject.SetActive(true);
        SetTextAndParentsVisible(resultText, true);
    }

    void EnsureGameplayTextReferences()
    {
        Canvas canvas = GetRoomTwoCanvas();
        if (canvas == null)
            return;

        if (roomLabelText == null)
            roomLabelText = FindTextByName("Room", "Label", "Room 2") ?? CreateGameplayText("Room2LabelText", new Vector2(0f, 0.42f), new Vector2(1.4f, 0.16f), 0.09f, Color.black);

        if (timerText == null)
            timerText = CreateGameplayText("Room2TimerText", new Vector2(-0.36f, 0.3f), new Vector2(0.8f, 0.16f), 0.07f, Color.black);

        if (goalsLeftText == null)
            goalsLeftText = CreateGameplayText("Room2GoalsLeftText", new Vector2(0.36f, 0.3f), new Vector2(0.8f, 0.16f), 0.07f, Color.black);

        if (accuracyText == null)
            accuracyText = CreateGameplayText("Room2AccuracyText", new Vector2(0f, 0.16f), new Vector2(1.6f, 0.16f), 0.06f, Color.black);

        LayoutGameplayTextReferences();
    }

    TextMeshProUGUI CreateGameplayText(string objectName, Vector2 anchoredPosition, Vector2 size, float fontSize, Color color)
    {
        Canvas canvas = GetRoomTwoCanvas();
        if (canvas == null)
            return null;

        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(canvas.transform, false);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = fontSize;
        text.color = color;
        text.enableWordWrapping = true;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        return text;
    }

    Canvas GetRoomTwoCanvas()
    {
        Canvas labeledCanvas = FindCanvasContainingText("Room 2");
        if (labeledCanvas != null)
            return labeledCanvas;

        if (roomLabelText != null)
            return roomLabelText.GetComponentInParent<Canvas>(true);

        if (timerText != null)
            return timerText.GetComponentInParent<Canvas>(true);

        if (goalsLeftText != null)
            return goalsLeftText.GetComponentInParent<Canvas>(true);

        if (accuracyText != null)
            return accuracyText.GetComponentInParent<Canvas>(true);

        if (resultText != null)
            return resultText.GetComponentInParent<Canvas>(true);

        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            if (canvas.renderMode == RenderMode.WorldSpace)
                return canvas;
        }

        return null;
    }

    Canvas FindCanvasContainingText(string textValue)
    {
        TextMeshProUGUI[] texts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (TextMeshProUGUI text in texts)
        {
            if (text.text.Trim() != textValue)
                continue;

            Canvas canvas = text.GetComponentInParent<Canvas>(true);
            if (canvas != null)
                return canvas;
        }

        return null;
    }

    TextMeshProUGUI FindTextByName(params string[] nameParts)
    {
        TextMeshProUGUI[] texts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (TextMeshProUGUI text in texts)
        {
            string objectName = text.name.ToLower();
            bool matches = true;

            foreach (string namePart in nameParts)
            {
                if (!objectName.Contains(namePart.ToLower()))
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
                return text;
        }

        return null;
    }

    void ResetDefendersToActivePattern()
    {
        if (DefensiveSystemManager.Instance != null)
            DefensiveSystemManager.Instance.ResetBoardsToActivePattern();
    }

    void LayoutGameplayTextReferences()
    {
        ConfigureHudText(roomLabelText, new Vector2(0f, 0.28f), new Vector2(1.2f, 0.14f), 0.09f, TextAlignmentOptions.Center);
        ConfigureHudText(timerText, new Vector2(-0.28f, 0.12f), new Vector2(0.7f, 0.12f), 0.055f, TextAlignmentOptions.Center);
        ConfigureHudText(goalsLeftText, new Vector2(0.31f, 0.12f), new Vector2(0.72f, 0.12f), 0.055f, TextAlignmentOptions.Center);
        ConfigureHudText(accuracyText, new Vector2(0f, -0.04f), new Vector2(1.22f, 0.12f), 0.045f, TextAlignmentOptions.Center);
    }

    void ConfigureHudText(TextMeshProUGUI text, Vector2 anchoredPosition, Vector2 size, float fontSize, TextAlignmentOptions alignment)
    {
        if (text == null)
            return;

        text.alignment = alignment;
        text.fontSize = fontSize;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Overflow;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }
}
