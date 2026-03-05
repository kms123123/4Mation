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
    /// 턴 히스토리 (스택). 한 턴 무르기용.
    /// </summary>
    private Stack<PlacementRecord> placementHistory = new Stack<PlacementRecord>();

    private struct PlacementRecord
    {
        public int Player;
        public GridCell Cell;
    }

    /// <summary>
    /// 4목을 이룬 모든 그리드 셀 리스트. 승리 시에만 채워짐.
    /// </summary>
    public IReadOnlyList<GridCell> WinningCells { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        UIManager.Instance?.UpdateTurn(currentPlayer, pieceCount);
    }

    private void Start()
    {
        UpdateSelectableCells();
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

        var previousTop = placementHistory.Count > 0 ? placementHistory.Peek().Cell : null;
        placementHistory.Push(new PlacementRecord { Player = currentPlayer, Cell = cell });
        if (previousTop != null) previousTop.SetPutIndicator(false);
        cell.SetPutIndicator(true);

        var winningLine = BoardManager.Instance.GetWinningLine(cell, currentPlayer);
        if (winningLine != null)
        {
            gameOver = true;
            WinningCells = winningLine;
            foreach (var c in winningLine)
                c.SetHighlight(true);

            UIManager.Instance?.ShowVictory(currentPlayer);
            UpdateSelectableCells();
            return;
        }

        if (pieceCount >= PiecesPerPlayer * 2)
        {
            gameOver = true;
            UIManager.Instance?.ShowDraw();
            UpdateSelectableCells();
            return;
        }

        UIManager.Instance?.ClearStatus();

        currentPlayer = currentPlayer == 1 ? 2 : 1;
        UIManager.Instance?.UpdateTurn(currentPlayer, pieceCount);
        UpdateSelectableCells();
    }

    /// <summary>
    /// 놓을 수 있는 셀에만 canSelectImage 활성화
    /// </summary>
    private void UpdateSelectableCells()
    {
        if (BoardManager.Instance == null || BoardManager.Instance.Cells == null) return;

        var validList = gameOver
            ? new List<GridCell>()
            : BoardManager.Instance.GetValidPlacementCells(isFirstTurn, lastPlacedCell, currentPlayer);
        var validSet = new HashSet<GridCell>(validList);

        for (int r = 0; r < BoardManager.Instance.BoardSize; r++)
        {
            for (int c = 0; c < BoardManager.Instance.BoardSize; c++)
            {
                var cell = BoardManager.Instance.Cells[r, c];
                cell.SetCanSelect(validSet.Contains(cell));
            }
        }
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

        placementHistory.Clear();

        if (BoardManager.Instance != null && BoardManager.Instance.Cells != null)
        {
            for (int r = 0; r < BoardManager.Instance.BoardSize; r++)
            {
                for (int c = 0; c < BoardManager.Instance.BoardSize; c++)
                {
                    var cell = BoardManager.Instance.Cells[r, c];
                    cell.SetHighlight(false);
                    cell.SetPutIndicator(false);
                    cell.SetCanSelect(false);
                    cell.ClearPiece();
                }
            }
        }

        UIManager.Instance?.ResetForNewGame();
        UpdateSelectableCells();
    }
}
