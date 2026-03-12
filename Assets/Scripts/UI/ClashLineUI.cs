using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 아군과 적 사이에 합/일방공격 연결선을 그린다.
/// UI Image를 늘려서 선처럼 표현.
/// </summary>
public class ClashLineUI : MonoBehaviour
{
    [SerializeField] private RectTransform lineImage;
    [SerializeField] private Image lineColor;
    [SerializeField] private BattleManager battleManager;

    [Header("색상")]
    [SerializeField] private Color clashColor = new Color(1f, 0.85f, 0.2f, 0.8f);
    [SerializeField] private Color unopposedColor = new Color(0.8f, 0.2f, 0.2f, 0.6f);

    private bool _showLine;
    private bool _isClash;

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

    private void Start()
    {
        HideLine();
    }

    private void OnStateChanged(BattleState state)
    {
        if (state == BattleState.ClashResolve)
            ShowLine(true);
        else if (state == BattleState.BattleEnd)
            HideLine();
        // SkillSelect에서는 숨기지 않음 — ApplyResult 후 1초 뒤 숨김
        else if (state == BattleState.ApplyResult)
            Invoke(nameof(HideLine), 1f);
    }

    public void ShowLine(bool isClash)
    {
        _isClash = isClash;
        _showLine = true;

        if (lineImage != null)
            lineImage.gameObject.SetActive(true);

        if (lineColor != null)
            lineColor.color = isClash ? clashColor : unopposedColor;
    }

    public void HideLine()
    {
        _showLine = false;
        if (lineImage != null)
            lineImage.gameObject.SetActive(false);
    }
}
