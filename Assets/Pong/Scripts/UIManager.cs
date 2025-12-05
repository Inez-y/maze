using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public TMP_Text player0score;
    public TMP_Text player1score;
    public TMP_Text gameOver;
    public GameObject opponentMenu;

    public void Start()
    {
        EventManager.GameStart += HideOpponentMenu;
        EventManager.GameEnd += ShowGameOver;
    }

    public void HideOpponentMenu(PaddleType pt) => opponentMenu.SetActive(false);

    public void UpdateScore(int[] score)
    {
        player0score.text = "Player One: " + score[0].ToString();
        player1score.text = "Player Two: " + score[1].ToString();
    }

    public void ShowGameOver(int winner)
    {
        gameOver.text = "Game Over! Winner: " + (winner == 0 ? "Player One" : "Player Two");
        gameOver.gameObject.SetActive(true);
    }

    public void OnDestroy()
    {
        EventManager.GameStart -= HideOpponentMenu;
        EventManager.GameEnd -= ShowGameOver;
    }
}
