using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 씬에 배치된 7x7 보드의 각 셀 8방향 이웃 연결, 클릭 처리, 4목 판정.
/// 보드는 씬에서 직접 디자인하고, boardContainer에 할당.
/// </summary>
public class BoardManager : SingletonBehaviour<BoardManager>
{
    [Header("보드 설정")]
    [SerializeField] private int boardSize = 7;

    [Header("씬에서 디자인한 보드")]
    [Tooltip("7x7 GridCell들이 자식으로 있는 부모 (GridLayoutGroup 권장)")]
    [SerializeField] private RectTransform boardContainer;

    [Tooltip("체크 시 자식 순서대로 Row, Col 자동 할당 (0,0)→(0,1)→...→(6,6)")]
    [SerializeField] private bool autoAssignRowCol = true;

    private GridCell[,] cells;

    public int BoardSize => boardSize;
    public GridCell[,] Cells => cells;

    public GridCell GetCell(int row, int col)
    {
        if (cells == null || row < 0 || row >= boardSize || col < 0 || col >= boardSize)
            return null;
        return cells[row, col];
    }

    protected override void Awake()
    {
        base.Awake();
        if (boardContainer == null)
        {
            Debug.LogError("BoardManager: boardContainer를 할당해주세요. 씬에서 보드를 디자인한 뒤 부모를 드래그하세요.");
            return;
        }

        InitializeFromExistingCells();
    }

    /// <summary>
    /// 씬에 배치된 셀들로 초기화 및 8방향 이웃 연결
    /// </summary>
    private void InitializeFromExistingCells()
    {
        var existingCells = boardContainer.GetComponentsInChildren<GridCell>(true);
        if (existingCells.Length != boardSize * boardSize)
        {
            Debug.LogError($"BoardManager: {boardSize}x{boardSize} = {boardSize * boardSize}개의 셀이 필요합니다. 현재: {existingCells.Length}");
            return;
        }

        cells = new GridCell[boardSize, boardSize];

        for (int i = 0; i < existingCells.Length; i++)
        {
            var cell = existingCells[i];
            if (autoAssignRowCol)
            {
                cell.Row = i / boardSize;
                cell.Col = i % boardSize;
            }
            cell.Initialize(cell.Row, cell.Col, this);
            cells[cell.Row, cell.Col] = cell;
        }

        ConnectAllNeighbors();
    }

    /// <summary>
    /// 모든 셀의 8방향 이웃 연결
    /// </summary>
    private void ConnectAllNeighbors()
    {
        for (int r = 0; r < boardSize; r++)
        {
            for (int c = 0; c < boardSize; c++)
            {
                var cell = cells[r, c];
                cell.North = GetCell(r - 1, c);
                cell.South = GetCell(r + 1, c);
                cell.East = GetCell(r, c + 1);
                cell.West = GetCell(r, c - 1);
                cell.NorthEast = GetCell(r - 1, c + 1);
                cell.NorthWest = GetCell(r - 1, c - 1);
                cell.SouthEast = GetCell(r + 1, c + 1);
                cell.SouthWest = GetCell(r + 1, c - 1);
            }
        }
    }

    public void OnCellClicked(GridCell cell)
    {
        GameManager.Instance?.OnCellClicked(cell);
    }

    /// <summary>
    /// 말 놓기 규칙: 첫 턴은 아무데나, 이후엔 상대가 막 놓은 위치 주변 8칸만.
    /// 주변 8칸이 전부 막혀있으면 상대 모든 말의 주변 8칸 중 하나에 놓을 수 있음.
    /// </summary>
    public bool IsValidPlacement(GridCell cell, bool isFirstTurn, GridCell lastPlacedCell, int currentPlayer)
    {
        if (isFirstTurn) return true;
        if (lastPlacedCell == null) return false;

        int opponent = currentPlayer == 1 ? 2 : 1;
        var neighbors = lastPlacedCell.GetNeighbors();

        bool anyNeighborFree = false;
        bool cellIsInNeighbors = false;
        foreach (var n in neighbors)
        {
            if (n == null) continue;
            if (n == cell) cellIsInNeighbors = true;
            if (!n.HasPiece) anyNeighborFree = true;
        }

        if (anyNeighborFree)
            return cellIsInNeighbors;

        for (int r = 0; r < boardSize; r++)
        {
            for (int c = 0; c < boardSize; c++)
            {
                var other = cells[r, c];
                if (other.CurrentPlayer != opponent) continue;

                foreach (var n in other.GetNeighbors())
                {
                    if (n != null && n == cell)
                        return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// 4목을 이룬 모든 그리드 셀 리스트 반환. 없으면 null.
    /// 가로, 세로, 대각선(\) , 대각선(/) 4방향 검사.
    /// </summary>
    public List<GridCell> GetWinningLine(GridCell cell, int player)
    {
        if (cell.CurrentPlayer != player) return null;
        var winningLine = new List<GridCell>();

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
}
