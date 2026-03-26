using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 4x4 회전 보드 게임 매니저.
/// 가장자리 12칸(외부링)과 중앙 4칸(내부링)이 각각 시계방향으로 회전함.
/// 회전은 매 말 놓기 이후 자동으로 발생.
/// </summary>
public class RotatingBoardManager : SingletonBehaviour<RotatingBoardManager>, IGridCellOwner
{
    private const int BoardSize = 4;

    [Header("씬에서 디자인한 보드")]
    [Tooltip("4x4 GridCell들이 자식으로 있는 부모 (GridLayoutGroup 권장)")]
    [SerializeField] private RectTransform boardContainer;

    [Tooltip("체크 시 자식 순서대로 Row, Col 자동 할당 (0,0)→(0,1)→...→(3,3)")]
    [SerializeField] private bool autoAssignRowCol = true;

    private GridCell[,] cells;

    public GridCell[,] Cells => cells;
    public int Size => BoardSize;

    // 외부링 (12칸) 시계방향 좌표 순서
    private static readonly (int r, int c)[] OuterRing =
    {
        (0, 0), (0, 1), (0, 2), (0, 3),
        (1, 3), (2, 3),
        (3, 3), (3, 2), (3, 1), (3, 0),
        (2, 0), (1, 0)
    };

    // 내부링 (4칸) 시계방향 좌표 순서
    private static readonly (int r, int c)[] InnerRing =
    {
        (1, 1), (1, 2),
        (2, 2), (2, 1)
    };

    protected override void Awake()
    {
        base.Awake();

        if (boardContainer == null)
        {
            Debug.LogError("RotatingBoardManager: boardContainer를 할당해주세요.");
            return;
        }

        InitializeBoard();
    }

    private void InitializeBoard()
    {
        var existingCells = boardContainer.GetComponentsInChildren<GridCell>(true);
        if (existingCells.Length != BoardSize * BoardSize)
        {
            Debug.LogError($"RotatingBoardManager: {BoardSize}x{BoardSize} = {BoardSize * BoardSize}개의 셀이 필요합니다. 현재: {existingCells.Length}");
            return;
        }

        cells = new GridCell[BoardSize, BoardSize];

        for (int i = 0; i < existingCells.Length; i++)
        {
            var cell = existingCells[i];
            if (autoAssignRowCol)
            {
                cell.Row = i / BoardSize;
                cell.Col = i % BoardSize;
            }
            cell.Initialize(cell.Row, cell.Col, this);
            cells[cell.Row, cell.Col] = cell;
        }

        ConnectNeighbors();
    }

    private void ConnectNeighbors()
    {
        for (int r = 0; r < BoardSize; r++)
        {
            for (int c = 0; c < BoardSize; c++)
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

    public GridCell GetCell(int r, int c)
    {
        if (r < 0 || r >= BoardSize || c < 0 || c >= BoardSize) return null;
        return cells[r, c];
    }

    public void OnCellClicked(GridCell cell)
    {
        RotatingGameManager.Instance?.OnCellClicked(cell);
    }

    /// <summary>
    /// 외부링과 내부링을 각각 시계방향으로 한 칸 회전.
    /// 각 칸의 CurrentPlayer 값과 비주얼이 다음 위치로 이동함.
    /// </summary>
    public void RotateBoard()
    {
        RotateRing(OuterRing);
        RotateRing(InnerRing);
    }

    /// <summary>
    /// 주어진 링을 시계방향으로 한 칸 회전.
    /// ring[i]의 값이 ring[(i+1) % n]으로 이동.
    /// </summary>
    private void RotateRing((int r, int c)[] ring)
    {
        int n = ring.Length;

        // 현재 플레이어 값 스냅샷
        int[] snapshot = new int[n];
        for (int i = 0; i < n; i++)
            snapshot[i] = cells[ring[i].r, ring[i].c].CurrentPlayer;

        // 시계방향: i번 내용 → (i+1)%n 번 위치로
        for (int i = 0; i < n; i++)
        {
            int next = (i + 1) % n;
            cells[ring[next].r, ring[next].c].SetPlayerValue(snapshot[i]);
        }
    }

    /// <summary>
    /// 보드 전체에서 4목 달성 여부 확인.
    /// 가로 4줄, 세로 4줄, 대각선 2줄을 검사.
    /// </summary>
    /// <param name="priorityPlayer">동시 4목 달성 시 우선할 플레이어 (보통 현재 플레이어)</param>
    /// <returns>(승리 플레이어 번호, 승리 라인 셀 목록). 승리 없으면 (0, null).</returns>
    public (int winner, List<GridCell> line) CheckWin(int priorityPlayer)
    {
        var lines = GetAllLines();

        int secondPlayer = priorityPlayer == 1 ? 2 : 1;
        List<GridCell> secondWinnerLine = null;

        foreach (var line in lines)
        {
            int p = line[0].CurrentPlayer;
            if (p == 0) continue;

            bool allSame = true;
            foreach (var c in line)
            {
                if (c.CurrentPlayer != p) { allSame = false; break; }
            }

            if (!allSame) continue;

            if (p == priorityPlayer)
                return (p, line);

            // 상대방 승리: 기억해두고 우선 플레이어 확인 후 처리
            if (secondWinnerLine == null)
                secondWinnerLine = line;
        }

        if (secondWinnerLine != null)
            return (secondPlayer, secondWinnerLine);

        return (0, null);
    }

    /// <summary>
    /// 4x4 보드의 모든 4목 라인 (가로 4 + 세로 4 + 대각선 2 = 총 10개)
    /// </summary>
    private List<List<GridCell>> GetAllLines()
    {
        var lines = new List<List<GridCell>>();

        // 가로
        for (int r = 0; r < BoardSize; r++)
        {
            var line = new List<GridCell>();
            for (int c = 0; c < BoardSize; c++) line.Add(cells[r, c]);
            lines.Add(line);
        }

        // 세로
        for (int c = 0; c < BoardSize; c++)
        {
            var line = new List<GridCell>();
            for (int r = 0; r < BoardSize; r++) line.Add(cells[r, c]);
            lines.Add(line);
        }

        // 대각선 \ : (0,0)→(1,1)→(2,2)→(3,3)
        {
            var line = new List<GridCell>();
            for (int i = 0; i < BoardSize; i++) line.Add(cells[i, i]);
            lines.Add(line);
        }

        // 대각선 / : (0,3)→(1,2)→(2,1)→(3,0)
        {
            var line = new List<GridCell>();
            for (int i = 0; i < BoardSize; i++) line.Add(cells[i, BoardSize - 1 - i]);
            lines.Add(line);
        }

        return lines;
    }

    /// <summary>
    /// 현재 빈 셀 목록 반환
    /// </summary>
    public List<GridCell> GetEmptyCells()
    {
        var result = new List<GridCell>();
        for (int r = 0; r < BoardSize; r++)
            for (int c = 0; c < BoardSize; c++)
                if (!cells[r, c].HasPiece) result.Add(cells[r, c]);
        return result;
    }
}
