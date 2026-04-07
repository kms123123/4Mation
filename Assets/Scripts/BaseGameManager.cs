using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// GameManager와 RotatingGameManager의 공통 베이스.
/// 턴 관리, 무르기(undo), 재시작, 승패 처리를 담당.
/// </summary>
public abstract class BaseGameManager : MonoBehaviour
{
    // ── 공유 상태 ─────────────────────────────────────────────────
    protected int currentPlayer = 1;
    protected bool gameOver;
    protected int pieceCount;
    public IReadOnlyList<GridCell> WinningCells { get; protected set; }

    protected Stack<IUndoRecord> placementHistory = new Stack<IUndoRecord>();

    // ── Undo 레코드 공통 인터페이스 ────────────────────────────────
    protected interface IUndoRecord
    {
        int Player { get; }
    }

    // ── Abstract (구체 클래스 구현 필수) ───────────────────────────
    public abstract void OnCellClicked(GridCell cell);
    protected abstract void UpdateSelectableCells();

    /// <summary>재시작 시 보드 전체 초기화 (말, 하이라이트, 인디케이터)</summary>
    protected abstract void ClearBoard();

    /// <summary>무르기 시 승리 하이라이트만 제거</summary>
    protected abstract void ClearAllHighlights();

    /// <summary>undo 레코드를 적용해 보드/상태 복원</summary>
    protected abstract void ApplyUndoRecord(IUndoRecord record);

    /// <summary>이 게임의 최대 말 수 (GameManager=48, RotatingGameManager=16)</summary>
    protected abstract int MaxPieceCount { get; }

    // ── 생명주기 ───────────────────────────────────────────────────
    protected virtual void Awake()
    {
        UIManager.Instance?.UpdateTurn(currentPlayer, pieceCount, MaxPieceCount);
    }

    protected virtual void Start()
    {
        UpdateSelectableCells();
    }

    // ── 공유 공개 메서드 ───────────────────────────────────────────

    /// <summary>
    /// 한 수 무르기: 마지막으로 놓은 말을 제거하고 이전 턴 상태로 복원
    /// </summary>
    public void UndoLastMove()
    {
        if (placementHistory.Count == 0) return;

        if (gameOver)
        {
            gameOver = false;
            WinningCells = null;
            ClearAllHighlights();
            UIManager.Instance?.HideGameOverOverlay();
        }

        var record = placementHistory.Pop();
        pieceCount--;

        ApplyUndoRecord(record);

        UIManager.Instance?.SetUndoButtonInteractable(placementHistory.Count > 0);
        UIManager.Instance?.UpdateTurn(currentPlayer, pieceCount, MaxPieceCount);
        UIManager.Instance?.ClearStatus();
        UpdateSelectableCells();
    }

    /// <summary>
    /// 게임 재시작: 공통 필드 초기화 후 구체 클래스 ClearBoard 호출
    /// </summary>
    public virtual void RestartGame()
    {
        gameOver = false;
        currentPlayer = 1;
        pieceCount = 0;
        WinningCells = null;
        placementHistory.Clear();

        ClearBoard();

        UIManager.Instance?.SetUndoButtonInteractable(false);
        UIManager.Instance?.ResetForNewGame();
        UIManager.Instance?.UpdateTurn(currentPlayer, pieceCount, MaxPieceCount);
        UpdateSelectableCells();
    }

    // ── Protected 헬퍼 ────────────────────────────────────────────

    /// <summary>현재 플레이어를 교체하고 UI 갱신</summary>
    protected void SwitchPlayer()
    {
        currentPlayer = currentPlayer == 1 ? 2 : 1;
        UIManager.Instance?.UpdateTurn(currentPlayer, pieceCount, MaxPieceCount);
    }

    /// <summary>승리 처리: 하이라이트, gameOver 플래그, UI</summary>
    protected void HandleWin(int winner, IReadOnlyList<GridCell> line)
    {
        gameOver = true;
        WinningCells = line;
        foreach (var c in line)
            c.SetHighlight(true);
        UIManager.Instance?.ShowVictory(winner);
        UpdateSelectableCells();
    }

    /// <summary>무승부 처리</summary>
    protected void HandleDraw()
    {
        gameOver = true;
        UIManager.Instance?.ShowDraw();
        UpdateSelectableCells();
    }
}
