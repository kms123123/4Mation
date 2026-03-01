using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 보드 UI를 화면 해상도에 맞게 조절.
/// 화면의 짧은 쪽 기준으로 보드가 들어가도록 크기 조정.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class BoardResolutionAdapter : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private RectTransform boardRect;
    [SerializeField] private GridLayoutGroup gridLayout;

    [Header("설정")]
    [Tooltip("화면 짧은 쪽 대비 보드가 차지할 비율 (0.5~1)")]
    [Range(0.5f, 1f)]
    [SerializeField] private float screenFillRatio = 0.85f;

    [Tooltip("셀 간격 (픽셀)")]
    [SerializeField] private float spacing = 4f;

    private Canvas rootCanvas;

    private void Awake()
    {
        if (boardRect == null) boardRect = GetComponent<RectTransform>();
        if (gridLayout == null) gridLayout = GetComponent<GridLayoutGroup>();
        rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
    }

    private void Start()
    {
        AdaptToScreen();
    }

    private void OnRectTransformDimensionsChange()
    {
        AdaptToScreen();
    }

    private int lastScreenWidth;
    private int lastScreenHeight;

    private void Update()
    {
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            AdaptToScreen();
        }
    }

    /// <summary>
    /// 화면 크기에 맞춰 보드 및 셀 크기 조정
    /// </summary>
    public void AdaptToScreen()
    {
        if (boardRect == null || gridLayout == null) return;

        float availableSize = GetAvailableBoardSize();
        if (availableSize <= 0) return;

        int cols = gridLayout.constraintCount;
        float totalSpacing = (cols - 1) * spacing;
        float cellSize = (availableSize - totalSpacing) / cols;

        boardRect.sizeDelta = new Vector2(availableSize, availableSize);
        gridLayout.cellSize = new Vector2(cellSize, cellSize);
        gridLayout.spacing = new Vector2(spacing, spacing);
    }

    private float GetAvailableBoardSize()
    {
        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;

        if (rootCanvas == null)
            return Mathf.Min(Screen.width, Screen.height) * screenFillRatio;

        float scaleFactor = Mathf.Max(rootCanvas.scaleFactor, 0.001f);
        float widthInCanvas = Screen.width / scaleFactor;
        float heightInCanvas = Screen.height / scaleFactor;
        return Mathf.Min(widthInCanvas, heightInCanvas) * screenFillRatio;
    }
}
