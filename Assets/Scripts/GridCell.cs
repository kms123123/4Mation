using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 7x7 보드의 각 그리드 셀.
/// 상하좌우 + 대각선 4방향, 총 8개의 이웃과 연결됨.
/// (가장자리/코너 셀은 존재하는 이웃만 연결)
/// </summary>
public class GridCell : MonoBehaviour
{
    [Header("위치")]
    public int Row;
    public int Col;

    [Header("8방향 이웃 (상하좌우 + 대각선)")]
    public GridCell North;      // 위
    public GridCell South;     // 아래
    public GridCell East;       // 오른쪽
    public GridCell West;       // 왼쪽
    public GridCell NorthEast;  // 오른쪽 위
    public GridCell NorthWest;  // 왼쪽 위
    public GridCell SouthEast;  // 오른쪽 아래
    public GridCell SouthWest;  // 왼쪽 아래

    [Header("UI")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image pieceImage;

    public bool HasPiece => CurrentPlayer != 0;

    public void SetImageReferences(Image bg, Image piece)
    {
        backgroundImage = bg;
        pieceImage = piece;
    }
    public int CurrentPlayer { get; private set; }

    private BoardManager boardManager;

    public void Initialize(int row, int col, BoardManager manager)
    {
        Row = row;
        Col = col;
        boardManager = manager;

        if (TryGetComponent<Button>(out var button))
        {
            button.onClick.AddListener(OnClicked);
        }

        if (pieceImage != null)
            pieceImage.gameObject.SetActive(false);
    }

    /// <summary>
    /// 8방향 이웃 배열 반환 (null 제외)
    /// </summary>
    public GridCell[] GetNeighbors()
    {
        return new[]
        {
            North, South, East, West,
            NorthEast, NorthWest, SouthEast, SouthWest
        };
    }

    /// <summary>
    /// 연결된 이웃 개수 (최대 8)
    /// </summary>
    public int NeighborCount
    {
        get
        {
            int count = 0;
            if (North != null) count++;
            if (South != null) count++;
            if (East != null) count++;
            if (West != null) count++;
            if (NorthEast != null) count++;
            if (NorthWest != null) count++;
            if (SouthEast != null) count++;
            if (SouthWest != null) count++;
            return count;
        }
    }

    public void PlacePiece(int player)
    {
        CurrentPlayer = player;
        if (pieceImage != null)
        {
            pieceImage.gameObject.SetActive(true);
            pieceImage.color = player == 1 ? Color.black : Color.white;
        }
    }

    public void ClearPiece()
    {
        CurrentPlayer = 0;
        if (pieceImage != null)
            pieceImage.gameObject.SetActive(false);
    }

    /// <summary>
    /// 4목 승리 시 해당 돌 표시 (하이라이트)
    /// </summary>
    public void SetHighlight(bool on)
    {
        if (pieceImage == null) return;
        if (on && HasPiece)
            pieceImage.color = new Color(1f, 0.84f, 0f, 1f); // 골드
        else if (HasPiece)
            pieceImage.color = CurrentPlayer == 1 ? Color.black : Color.white;
    }

    private void OnClicked()
    {
        boardManager?.OnCellClicked(this);
    }

    private void Awake()
    {
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
        if (pieceImage == null && transform.childCount > 0)
        {
            var child = transform.GetChild(0);
            if (child.TryGetComponent<Image>(out var img))
                pieceImage = img;
        }
    }
}
