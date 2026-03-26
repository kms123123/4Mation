using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 게임 UI 표시 담당. 턴, 상태 메시지, 게임 오버 오버레이 등.
/// </summary>
public class UIManager : SingletonBehaviour<UIManager>
{
    [Header("게임 UI")]
    [SerializeField] private TextMeshProUGUI turnText;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("게임 오버 오버레이 (Background)")]
    [SerializeField] private CanvasGroup gameOverOverlay;
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private Button restartButton;

    [Header("게임 오버 애니메이션")]
    [SerializeField] private float delayBeforeFade = 1f;
    [SerializeField] private float fadeInDuration = 1f;

    private Coroutine gameOverCoroutine;

    protected override void Awake()
    {
        base.Awake();
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);
    }

    /// <summary>
    /// 턴 표시 업데이트 (플레이어 N의 턴, 말 개수)
    /// </summary>
    public void UpdateTurn(int currentPlayer, int pieceCount)
    {
        if (turnText != null)
            turnText.text = $"Player {currentPlayer}'s Turn ({pieceCount}/48)";
    }

    /// <summary>
    /// 상태 메시지 표시 (에러, 안내 등)
    /// </summary>
    public void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    /// <summary>
    /// 상태 메시지 초기화
    /// </summary>
    public void ClearStatus()
    {
        if (statusText != null)
            statusText.text = "";
    }

    /// <summary>
    /// 승리 표시 - 1초 대기 후 1초간 페이드인
    /// </summary>
    public void ShowVictory(int winner)
    {
        var message = $"Player {winner} Wins!";
        if (turnText != null) turnText.text = message;
        if (statusText != null) statusText.text = message;
        ShowGameOverOverlay(message);
    }

    /// <summary>
    /// 무승부 표시 - 1초 대기 후 1초간 페이드인
    /// </summary>
    public void ShowDraw()
    {
        var message = "Draw!";
        if (turnText != null) turnText.text = message;
        if (statusText != null) statusText.text = message;
        ShowGameOverOverlay(message);
    }

    /// <summary>
    /// 게임 오버 오버레이 표시: 1초 대기 -> 1초간 alpha 0->1 페이드인
    /// </summary>
    private void ShowGameOverOverlay(string message)
    {
        if (gameOverOverlay == null) return;

        if (gameOverCoroutine != null)
            StopCoroutine(gameOverCoroutine);

        gameOverCoroutine = StartCoroutine(GameOverFadeInCoroutine(message));
    }

    private IEnumerator GameOverFadeInCoroutine(string message)
    {
        if (gameOverText != null)
            gameOverText.text = message;

        gameOverOverlay.gameObject.SetActive(true);
        gameOverOverlay.alpha = 0f;

        yield return new WaitForSeconds(delayBeforeFade);

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            gameOverOverlay.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }

        gameOverOverlay.alpha = 1f;
        gameOverCoroutine = null;
    }

    private void OnRestartClicked()
    {
        HideGameOverOverlay();
        GameManager.Instance?.RestartGame();
        RotatingGameManager.Instance?.RestartGame();
    }

    /// <summary>
    /// 게임 오버 오버레이 숨김
    /// </summary>
    private void HideGameOverOverlay()
    {
        if (gameOverCoroutine != null)
        {
            StopCoroutine(gameOverCoroutine);
            gameOverCoroutine = null;
        }
        if (gameOverOverlay != null)
            gameOverOverlay.gameObject.SetActive(false);
    }

    /// <summary>
    /// 게임 재시작 시 UI 초기화
    /// </summary>
    public void ResetForNewGame()
    {
        HideGameOverOverlay();
        UpdateTurn(1, 0);
        ClearStatus();
    }
}
