using UnityEngine;
using System.Collections.Generic;

/// <summary>   
/// 턴제 말 놓기 게임 로직.
/// 두 플레이어가 번갈아가며 빈 칸에 말을 놓습니다.
/// </summary>
public class GameManager : SingletonBehaviour<GameManager>
{
    private int currentPlayer = 1; // 1 또는 2
    private bool gameOver;
    private bool isFirstTurn = true;
    private GridCell lastPlacedCell;
    private int pieceCount;

    private const int PiecesPerPlayer = 24;

    /// <summary>
    /// 4목을 이룬 모든 그리드 셀 리스트. 승리 시에만 채워짐.
    /// </summary>
    public IReadOnlyList<GridCell> WinningCells { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        UIManager.Instance?.UpdateTurn(currentPlayer, pieceCount);
    }

    public void OnCellClicked(GridCell cell)
    {
        if (gameOver) return;
        if (BoardManager.Instance == null) return;
        if (cell == null) return;

        if (cell.HasPiece)
        {
            UIManager.Instance?.SetStatus("This cell already has a piece.");
            return;
        }

        if (!BoardManager.Instance.IsValidPlacement(cell, isFirstTurn, lastPlacedCell, currentPlayer))
        {
            UIManager.Instance?.SetStatus("Invalid placement.");
            return;
        }

        cell.PlacePiece(currentPlayer);
        lastPlacedCell = cell;
        isFirstTurn = false;
        pieceCount++;

        var winningLine = BoardManager.Instance.GetWinningLine(cell, currentPlayer);
        if (winningLine != null)
        {
            gameOver = true;
            WinningCells = winningLine;
            foreach (var c in winningLine)
                c.SetHighlight(true);

            UIManager.Instance?.ShowVictory(currentPlayer);
            return;
        }

        if (pieceCount >= PiecesPerPlayer * 2)
        {
            gameOver = true;
            UIManager.Instance?.ShowDraw();
            return;
        }

        UIManager.Instance?.ClearStatus();

        currentPlayer = currentPlayer == 1 ? 2 : 1;
        UIManager.Instance?.UpdateTurn(currentPlayer, pieceCount);
    }

    /// <summary>
    /// 게임 재시작
    /// </summary>
    public void RestartGame()
    {
        gameOver = false;
        currentPlayer = 1;
        WinningCells = null;
        isFirstTurn = true;
        lastPlacedCell = null;
        pieceCount = 0;

        if (BoardManager.Instance != null && BoardManager.Instance.Cells != null)
        {
            for (int r = 0; r < BoardManager.Instance.BoardSize; r++)
            {
                for (int c = 0; c < BoardManager.Instance.BoardSize; c++)
                {
                    var cell = BoardManager.Instance.Cells[r, c];
                    cell.SetHighlight(false);
                    cell.ClearPiece();
                }
            }
        }

        UIManager.Instance?.ResetForNewGame();
    }
}
