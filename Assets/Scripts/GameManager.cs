using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 턴제 말 놓기 게임 로직.
/// 두 플레이어가 번갈아가며 빈 칸에 말을 놓습니다.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private Text turnText;
    [SerializeField] private Text statusText;

    private int currentPlayer = 1; // 1 또는 2
    private bool gameOver;

    /// <summary>
    /// 4목을 이룬 모든 그리드 셀 리스트. 승리 시에만 채워짐.
    /// </summary>
    public IReadOnlyList<GridCell> WinningCells { get; private set; }

    private void Awake()
    {
        if (boardManager == null)
            boardManager = GetComponent<BoardManager>();
        if (boardManager == null)
            boardManager = FindAnyObjectByType<BoardManager>();

        UpdateTurnUI();
    }

    public void OnCellClicked(GridCell cell)
    {
        if (gameOver) return;
        if (boardManager == null) return;
        if (cell == null) return;

        if (cell.HasPiece)
        {
            if (statusText != null)
                statusText.text = "이미 말이 있는 칸입니다.";
            return;
        }

        cell.PlacePiece(currentPlayer);

        var winningLine = boardManager.GetWinningLine(cell, currentPlayer);
        if (winningLine != null)
        {
            gameOver = true;
            WinningCells = winningLine;
            foreach (var c in winningLine)
                c.SetHighlight(true);

            if (statusText != null)
                statusText.text = $"플레이어 {currentPlayer} 승리!";
            if (turnText != null)
                turnText.text = $"플레이어 {currentPlayer} 승리!";
            return;
        }

        if (statusText != null)
            statusText.text = "";

        currentPlayer = currentPlayer == 1 ? 2 : 1;
        UpdateTurnUI();
    }

    private void UpdateTurnUI()
    {
        if (turnText != null)
            turnText.text = $"플레이어 {currentPlayer}의 턴";
    }

    /// <summary>
    /// 게임 재시작
    /// </summary>
    public void RestartGame()
    {
        gameOver = false;
        currentPlayer = 1;
        WinningCells = null;

        if (boardManager != null && boardManager.Cells != null)
        {
            for (int r = 0; r < boardManager.BoardSize; r++)
            {
                for (int c = 0; c < boardManager.BoardSize; c++)
                {
                    var cell = boardManager.Cells[r, c];
                    cell.SetHighlight(false);
                    cell.ClearPiece();
                }
            }
        }

        UpdateTurnUI();
        if (statusText != null)
            statusText.text = "";
    }
}
