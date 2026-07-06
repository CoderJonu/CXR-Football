using UnityEngine;

public class DefensiveSystemManager : MonoBehaviour
{
    // 0 = Pattern A, 1 = Pattern B, 2 = Pattern C
    public int currentActivePattern = 0;

    private static DefensiveSystemManager instance;
    public static DefensiveSystemManager Instance => instance;

    void Awake()
    {
        instance = this;
    }

    public void CycleDefensivePattern()
    {
        currentActivePattern = (currentActivePattern + 1) % 3;

        // Broadcast pattern shift instantly to all 10 outfield boards
        ResetBoardsToActivePattern();

        Debug.Log("Goal scored! Shifting defensive positions to Pattern Index: " + currentActivePattern);
    }

    public void ResetBoardsToActivePattern()
    {
        DefenderBoardAI[] allBoards = Object.FindObjectsByType<DefenderBoardAI>(FindObjectsSortMode.None);
        foreach (DefenderBoardAI board in allBoards)
        {
            board.ResetToActivePattern();
        }
    }
}
