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
public class RotatingGameManager : SingletonBehaviour<RotatingGameManager>
{
    private int currentPlayer = 1;
    private bool gameOver;
    private int pieceCount;

    // 4x4 보드 = 최대 16칸
    private const int MaxPieces = 16;

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
        if (RotatingBoardManager.Instance == null) return;

        if (cell.HasPiece)
        {
            UIManager.Instance?.SetStatus("이미 말이 놓인 칸입니다.");
            return;
        }

        // 1. 말 놓기
        cell.PlacePiece(currentPlayer);
        pieceCount++;

        // 2. 보드 회전
        RotatingBoardManager.Instance.RotateBoard();

        // 3. 4목 확인 (현재 플레이어 우선)
        var (winner, line) = RotatingBoardManager.Instance.CheckWin(currentPlayer);
        if (winner != 0)
        {
            gameOver = true;
            WinningCells = line;
            foreach (var c in line)
                c.SetHighlight(true);

            UIManager.Instance?.ShowVictory(winner);
            UpdateSelectableCells();
            return;
        }

        // 4. 무승부 (보드 가득 참)
        if (pieceCount >= MaxPieces)
        {
            gameOver = true;
            UIManager.Instance?.ShowDraw();
            UpdateSelectableCells();
            return;
        }

        // 5. 다음 턴
        UIManager.Instance?.ClearStatus();
        currentPlayer = currentPlayer == 1 ? 2 : 1;
        UIManager.Instance?.UpdateTurn(currentPlayer, pieceCount);
        UpdateSelectableCells();
    }

    /// <summary>
    /// 빈 칸에만 canSelectImage 활성화
    /// </summary>
    private void UpdateSelectableCells()
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

    /// <summary>
    /// 게임 재시작
    /// </summary>
    public void RestartGame()
    {
        gameOver = false;
        currentPlayer = 1;
        pieceCount = 0;
        WinningCells = null;

        var board = RotatingBoardManager.Instance;
        if (board?.Cells != null)
        {
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

        UIManager.Instance?.ResetForNewGame();
        UIManager.Instance?.UpdateTurn(currentPlayer, pieceCount);
        UpdateSelectableCells();
    }
}
