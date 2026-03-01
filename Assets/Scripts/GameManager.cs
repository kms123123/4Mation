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
            boardManager = Object.FindObjectOfType<BoardManager>();

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

        var winningLine = GetWinningLine(cell, currentPlayer);
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
    /// 4목을 이룬 모든 그리드 셀 리스트 반환. 없으면 null.
    /// 가로, 세로, 대각선(\) , 대각선(/) 4방향 검사.
    /// </summary>
    private List<GridCell> GetWinningLine(GridCell cell, int player)
    {
        if (cell.CurrentPlayer != player) return null;
        List<GridCell> winningLine = new List<GridCell>();

        // 가로 (동-서)
        var horizontal = GetLineInBothDirections(cell, player, c => c.East, c => c.West);
        if (horizontal.Count >= 4) winningLine.AddRange(horizontal);

        // 세로 (북-남)
        var vertical = GetLineInBothDirections(cell, player, c => c.North, c => c.South);
        if (vertical.Count >= 4) winningLine.AddRange(vertical);

        // 대각선 \ (북동-남서)
        var diag1 = GetLineInBothDirections(cell, player, c => c.NorthEast, c => c.SouthWest);
        if (diag1.Count >= 4) winningLine.AddRange(diag1);

        // 대각선 / (북서-남동)
        var diag2 = GetLineInBothDirections(cell, player, c => c.NorthWest, c => c.SouthEast);
        if (diag2.Count >= 4) winningLine.AddRange(diag2);

        return winningLine.Count >= 4 ? winningLine : null;
    }

    /// <summary>
    /// 한 방향으로 연속된 셀 리스트 수집 (현재 셀 제외)
    /// </summary>
    private List<GridCell> GetCellsInDirection(GridCell start, int player, System.Func<GridCell, GridCell> getNext)
    {
        var list = new List<GridCell>();
        var current = getNext(start);
        while (current != null && current.CurrentPlayer == player)
        {
            list.Add(current);
            current = getNext(current);
        }
        return list;
    }

    /// <summary>
    /// 양방향 셀 수집 후 현재 셀 포함해 한 줄로 합침. [반대방향...] + [현재] + [방향...]
    /// </summary>
    private List<GridCell> GetLineInBothDirections(GridCell cell, int player,
        System.Func<GridCell, GridCell> dir1, System.Func<GridCell, GridCell> dir2)
    {
        var reverse = GetCellsInDirection(cell, player, dir1);
        var forward = GetCellsInDirection(cell, player, dir2);
        reverse.Reverse();
        var line = new List<GridCell>();
        line.AddRange(reverse);
        line.Add(cell);
        line.AddRange(forward);
        return line;
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
