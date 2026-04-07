using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 턴제 말 놓기 게임 로직.
/// 두 플레이어가 번갈아가며 빈 칸에 말을 놓습니다.
/// </summary>
public class GameManager : BaseGameManager
{
    public static GameManager Instance { get; private set; }

    private bool isFirstTurn = true;
    private GridCell lastPlacedCell;

    private const int PiecesPerPlayer = 24;
    protected override int MaxPieceCount => PiecesPerPlayer * 2;

    private struct PlacementRecord : IUndoRecord
    {
        public int Player { get; set; }
        public GridCell Cell;
        public bool WasFirstTurn;
        public GridCell PreviousLastCell;
    }

    protected override void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        base.Awake();
    }

    public override void OnCellClicked(GridCell cell)
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

        // 무르기 복원용으로 변경 전 상태를 먼저 기록
        var previousTop = placementHistory.Count > 0 ? ((PlacementRecord)placementHistory.Peek()).Cell : null;
        placementHistory.Push(new PlacementRecord
        {
            Player = currentPlayer,
            Cell = cell,
            WasFirstTurn = isFirstTurn,
            PreviousLastCell = lastPlacedCell
        });

        cell.PlacePiece(currentPlayer);
        lastPlacedCell = cell;
        isFirstTurn = false;
        pieceCount++;

        if (previousTop != null) previousTop.SetPutIndicator(false);
        cell.SetPutIndicator(true);
        UIManager.Instance?.SetUndoButtonInteractable(true);

        var winningLine = BoardManager.Instance.GetWinningLine(cell, currentPlayer);
        if (winningLine != null)
        {
            HandleWin(currentPlayer, winningLine);
            return;
        }

        if (pieceCount >= MaxPieceCount)
        {
            HandleDraw();
            return;
        }

        UIManager.Instance?.ClearStatus();
        SwitchPlayer();
        UpdateSelectableCells();
    }

    protected override void UpdateSelectableCells()
    {
        if (BoardManager.Instance == null || BoardManager.Instance.Cells == null) return;

        var validList = gameOver
            ? new List<GridCell>()
            : BoardManager.Instance.GetValidPlacementCells(isFirstTurn, lastPlacedCell, currentPlayer);
        var validSet = new HashSet<GridCell>(validList);

        for (int r = 0; r < BoardManager.Instance.BoardSize; r++)
            for (int c = 0; c < BoardManager.Instance.BoardSize; c++)
                BoardManager.Instance.Cells[r, c].SetCanSelect(validSet.Contains(BoardManager.Instance.Cells[r, c]));
    }

    protected override void ApplyUndoRecord(IUndoRecord record)
    {
        var rec = (PlacementRecord)record;
        rec.Cell.ClearPiece(); // putIndicator도 자동으로 꺼짐
        currentPlayer = rec.Player;
        isFirstTurn = rec.WasFirstTurn;
        lastPlacedCell = rec.PreviousLastCell;

        if (placementHistory.Count > 0)
            ((PlacementRecord)placementHistory.Peek()).Cell.SetPutIndicator(true);
    }

    protected override void ClearBoard()
    {
        if (BoardManager.Instance == null || BoardManager.Instance.Cells == null) return;
        for (int r = 0; r < BoardManager.Instance.BoardSize; r++)
        {
            for (int c = 0; c < BoardManager.Instance.BoardSize; c++)
            {
                var cell = BoardManager.Instance.Cells[r, c];
                cell.ClearPiece();
                cell.SetHighlight(false);
                cell.SetCanSelect(false);
            }
        }
    }

    protected override void ClearAllHighlights()
    {
        if (BoardManager.Instance == null || BoardManager.Instance.Cells == null) return;
        for (int r = 0; r < BoardManager.Instance.BoardSize; r++)
            for (int c = 0; c < BoardManager.Instance.BoardSize; c++)
                BoardManager.Instance.Cells[r, c].SetHighlight(false);
    }

    public override void RestartGame()
    {
        isFirstTurn = true;
        lastPlacedCell = null;
        base.RestartGame();
    }
}
