using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

/// <summary>
/// 승/패 결과 화면.
/// CanvasGroup으로 페이드인 — SetActive(false)로 숨기면
/// OnEnable 이벤트 구독이 안 되므로 alpha로 제어.
/// </summary>
public class BattleResultUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private Button retryButton;
    [SerializeField] private BattleManager battleManager;

    private void Start()
    {
        // 처음엔 안 보이게
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetry);
    }

    private void OnEnable()
    {
        if (battleManager != null)
            battleManager.OnStateChanged += OnStateChanged;
    }

    private void OnDisable()
    {
        if (battleManager != null)
            battleManager.OnStateChanged -= OnStateChanged;
    }

    private void OnStateChanged(BattleState state)
    {
        if (state != BattleState.BattleEnd) return;

        // 결과 판정
        bool allyAlive = battleManager.Ally.IsAlive;
        bool enemyAlive = battleManager.Enemy.IsAlive;

        if (!allyAlive && !enemyAlive)
        {
            ShowResult("무승부", "양측 모두 전투불능", Color.gray);
        }
        else if (!enemyAlive)
        {
            ShowResult("승리!", "적을 처치했습니다", new Color(1f, 0.85f, 0.2f));
        }
        else
        {
            ShowResult("패배...", "아군이 전투불능", new Color(0.8f, 0.2f, 0.2f));
        }
    }

    private void ShowResult(string title, string subtitle, Color color)
    {
        if (titleText != null)
        {
            titleText.text = title;
            titleText.color = color;
        }

        if (subtitleText != null)
            subtitleText.text = subtitle;

        // 페이드인
        if (canvasGroup != null)
        {
            canvasGroup.DOFade(1f, 0.5f)
                .SetEase(Ease.OutQuad);
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        // 타이틀 팝 효과
        if (titleText != null)
        {
            titleText.transform.localScale = Vector3.zero;
            titleText.transform.DOScale(1f, 0.4f)
                .SetDelay(0.2f)
                .SetEase(Ease.OutBack);
        }
    }

    private void OnRetry()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
