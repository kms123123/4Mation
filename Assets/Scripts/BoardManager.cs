using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 씬에 배치된 7x7 보드의 각 셀 8방향 이웃 연결 및 클릭 처리.
/// 보드는 씬에서 직접 디자인하고, boardContainer에 할당.
/// </summary>
public class BoardManager : MonoBehaviour
{
    [Header("보드 설정")]
    [SerializeField] private int boardSize = 7;

    [Header("씬에서 디자인한 보드")]
    [Tooltip("7x7 GridCell들이 자식으로 있는 부모 (GridLayoutGroup 권장)")]
    [SerializeField] private RectTransform boardContainer;

    [Tooltip("체크 시 자식 순서대로 Row, Col 자동 할당 (0,0)→(0,1)→...→(6,6)")]
    [SerializeField] private bool autoAssignRowCol = true;

    private GridCell[,] cells;
    private GameManager gameManager;

    public int BoardSize => boardSize;
    public GridCell[,] Cells => cells;

    public GridCell GetCell(int row, int col)
    {
        if (cells == null || row < 0 || row >= boardSize || col < 0 || col >= boardSize)
            return null;
        return cells[row, col];
    }

    private void Awake()
    {
        gameManager = GetComponent<GameManager>();
        if (gameManager == null)
            gameManager = FindAnyObjectByType<GameManager>();

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
        gameManager?.OnCellClicked(cell);
    }
}
