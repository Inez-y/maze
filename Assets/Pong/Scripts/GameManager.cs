using UnityEngine;
using UnityEngine.SceneManagement;

public class Game : MonoBehaviour
{
    public UIManager uim;
    public int[] score = {0, 0};
    public int pointsToWin = 3;

    private void Start()
    {
        EventManager.PointScored += UpdateScore;
        EventManager.GameEnd += OnGameEnd;
    }

    private void OnDestroy()
    {
        EventManager.PointScored -= UpdateScore;
        EventManager.GameEnd -= OnGameEnd;
    }

    private void UpdateScore(int winner)
    {
        score[1 - winner]++;
        uim.UpdateScore(score);

        if (score[0] == pointsToWin)
            EventManager.TriggerGameEnd(0);
        else if (score[1] == pointsToWin)
            EventManager.TriggerGameEnd(1);
    }

    private void OnGameEnd(int winner)
    {
        StartCoroutine(DelayedSceneSwap());
    }

    private System.Collections.IEnumerator DelayedSceneSwap()
    {
        yield return new WaitForSeconds(3f);
        SceneManager.LoadScene("maze");
    }
}
