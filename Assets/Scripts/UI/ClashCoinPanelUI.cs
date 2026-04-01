using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClashCoinPanelUI : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text attackerNameText;
    [SerializeField] private TMP_Text attackerCoinsText;
    [SerializeField] private TMP_Text defenderNameText;
    [SerializeField] private TMP_Text defenderCoinsText;

    private CanvasGroup _canvasGroup;
    private Tween _fadeTween;

    private void Awake()
    {
        EnsureUI();
        HideImmediate();
    }

    public void Show(ClashResult clash)
    {
        if (clash == null) return;
        EnsureUI();

        string attackerName = clash.attackerSkill != null ? clash.attackerSkill.skillName : "공격";
        string defenderName = clash.defenderSkill != null ? clash.defenderSkill.skillName : "방어";

        titleText.text = clash.outcome switch
        {
            ClashOutcome.AttackerWin => "합 승리",
            ClashOutcome.DefenderWin => "합 패배",
            _ => "합 무승부"
        };
        attackerNameText.text = attackerName;
        defenderNameText.text = defenderName;
        attackerCoinsText.text = FormatCoins(clash.attackerStartingCoins, clash.attackerRemainingCoins, clash.winnerIsAttacker && clash.outcome != ClashOutcome.Draw);
        defenderCoinsText.text = FormatCoins(clash.defenderStartingCoins, clash.defenderRemainingCoins, !clash.winnerIsAttacker && clash.outcome != ClashOutcome.Draw);
        attackerCoinsText.color = clash.winnerIsAttacker ? new Color(1f, 0.88f, 0.35f) : new Color(1f, 0.55f, 0.55f);
        defenderCoinsText.color = !clash.winnerIsAttacker && clash.outcome != ClashOutcome.Draw ? new Color(1f, 0.88f, 0.35f) : new Color(1f, 0.55f, 0.55f);

        gameObject.SetActive(true);
        _fadeTween?.Kill();
        _canvasGroup.alpha = 1f;
        root.localScale = Vector3.one * 0.92f;
        root.DOScale(1f, 0.18f).SetEase(Ease.OutBack);
        _fadeTween = _canvasGroup.DOFade(0f, 1.3f).SetDelay(1.1f).OnComplete(() => gameObject.SetActive(false));
    }

    public void HideImmediate()
    {
        EnsureUI();
        _fadeTween?.Kill();
        _canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    private string FormatCoins(int start, int remain, bool winner)
    {
        remain = Mathf.Clamp(remain, 0, start);
        int broken = Mathf.Max(0, start - remain);
        var live = winner ? "O" : "o";
        var brokenIcon = "x";
        return string.Concat(System.Linq.Enumerable.Repeat(live, remain)) + string.Concat(System.Linq.Enumerable.Repeat(brokenIcon, broken));
    }

    private void EnsureUI()
    {
        if (root != null && _canvasGroup != null) return;

        root = GetComponent<RectTransform>();
        if (root == null) root = gameObject.AddComponent<RectTransform>();
        _canvasGroup ??= gameObject.GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        background ??= gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
        background.color = new Color(0.08f, 0.08f, 0.1f, 0.88f);
        root.anchorMin = new Vector2(0.5f, 0.71f);
        root.anchorMax = new Vector2(0.5f, 0.71f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(268f, 84f);

        titleText ??= CreateText("Title", new Vector2(0f, 24f), new Vector2(220f, 22f), 16, TextAlignmentOptions.Center);
        attackerNameText ??= CreateText("AttackerName", new Vector2(-64f, 0f), new Vector2(110f, 20f), 13, TextAlignmentOptions.Center);
        attackerCoinsText ??= CreateText("AttackerCoins", new Vector2(-64f, -22f), new Vector2(120f, 22f), 20, TextAlignmentOptions.Center);
        defenderNameText ??= CreateText("DefenderName", new Vector2(64f, 0f), new Vector2(110f, 20f), 13, TextAlignmentOptions.Center);
        defenderCoinsText ??= CreateText("DefenderCoins", new Vector2(64f, -22f), new Vector2(120f, 22f), 20, TextAlignmentOptions.Center);
    }

    private TMP_Text CreateText(string name, Vector2 anchoredPos, Vector2 size, float fontSize, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        var text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = align;
        text.color = Color.white;
        text.outlineWidth = 0.18f;
        text.outlineColor = Color.black;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }
}
