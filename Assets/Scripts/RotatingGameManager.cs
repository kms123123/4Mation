using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 4x4 회전 보드 게임의 턴 관리.
///
/// 게임 흐름:
///   1. 현재 플레이어가 빈 칸에 말 놓기
///   2. 보드 시계방향 회전 (외부링 12칸 + 내부링 4칸 각각 1칸씩)
///   3. 4목 달성 여부 확인
///   4. 이기거나 무승부가 아니면 다음 플레이어 턴
/// </summary>
public class RotatingGameManager : BaseGameManager
{
    public static RotatingGameManager Instance { get; private set; }

    // 4x4 보드 = 최대 16칸
    protected override int MaxPieceCount => 16;

    private struct RotatingPlacementRecord : IUndoRecord
    {
        public int Player { get; set; }
        public int[,] BoardSnapshot; // 말 놓기 직전 전체 보드 (4×4)
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
        if (RotatingBoardManager.Instance == null) return;

        if (cell.HasPiece)
        {
            UIManager.Instance?.SetStatus("이미 말이 놓인 칸입니다.");
            return;
        }

        // 무르기용: 말 놓기 직전 전체 보드 스냅샷 저장
        var board = RotatingBoardManager.Instance;
        var snapshot = new int[board.Size, board.Size];
        for (int r = 0; r < board.Size; r++)
            for (int c = 0; c < board.Size; c++)
                snapshot[r, c] = board.Cells[r, c].CurrentPlayer;

        placementHistory.Push(new RotatingPlacementRecord
        {
            Player = currentPlayer,
            BoardSnapshot = snapshot
        });

        // 1. 말 놓기
        cell.PlacePiece(currentPlayer);
        pieceCount++;
        UIManager.Instance?.SetUndoButtonInteractable(true);

        // 2. 보드 회전
        board.RotateBoard();

        // 3. 4목 확인 (현재 플레이어 우선)
        var (winner, line) = board.CheckWin(currentPlayer);
        if (winner != 0)
        {
            HandleWin(winner, line);
            return;
        }

        // 4. 무승부 (보드 가득 참)
        if (pieceCount >= MaxPieceCount)
        {
            HandleDraw();
            return;
        }

        // 5. 다음 턴
        UIManager.Instance?.ClearStatus();
        SwitchPlayer();
        UpdateSelectableCells();
    }

    protected override void UpdateSelectableCells()
    {
        var board = RotatingBoardManager.Instance;
        if (board?.Cells == null) return;

        var emptyCells = gameOver
            ? new List<GridCell>()
            : board.GetEmptyCells();
        var emptySet = new HashSet<GridCell>(emptyCells);

        for (int r = 0; r < board.Size; r++)
            for (int c = 0; c < board.Size; c++)
                board.Cells[r, c].SetCanSelect(emptySet.Contains(board.Cells[r, c]));
    }

    protected override void ApplyUndoRecord(IUndoRecord record)
    {
        var r = (RotatingPlacementRecord)record;
        var board = RotatingBoardManager.Instance;
        if (board?.Cells == null) return;

        // 스냅샷으로 전체 보드 복원 (말 제거 + 회전 취소 동시 처리)
        for (int row = 0; row < board.Size; row++)
            for (int col = 0; col < board.Size; col++)
                board.Cells[row, col].SetPlayerValue(r.BoardSnapshot[row, col]);

        currentPlayer = r.Player;
    }

    protected override void ClearBoard()
    {
        var board = RotatingBoardManager.Instance;
        if (board?.Cells == null) return;
        for (int r = 0; r < board.Size; r++)
        {
            for (int c = 0; c < board.Size; c++)
            {
                var cell = board.Cells[r, c];
                cell.ClearPiece();
                cell.SetHighlight(false);
                cell.SetCanSelect(false);
            }
        }
    }

    protected override void ClearAllHighlights()
    {
        var board = RotatingBoardManager.Instance;
        if (board?.Cells == null) return;
        for (int r = 0; r < board.Size; r++)
            for (int c = 0; c < board.Size; c++)
                board.Cells[r, c].SetHighlight(false);
    }
}
