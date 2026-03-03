using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 전투 화면 UI.
/// BattleManager의 이벤트를 구독해서 표시만 담당.
/// 전투 로직을 알 필요 없다.
/// </summary>
public class BattleUI : MonoBehaviour
{
    private const int MaxLogLines = 6;

    [Header("연결")]
    [SerializeField] private BattleManager battleManager;

    [Header("유닛 정보")]
    [SerializeField] private TextMeshProUGUI allyNameText;
    [SerializeField] private TextMeshProUGUI allyHPText;
    [SerializeField] private TextMeshProUGUI enemyNameText;
    [SerializeField] private TextMeshProUGUI enemyHPText;

    [Header("전투 로그")]
    [SerializeField] private TextMeshProUGUI logText;

    [Header("버튼")]
    [SerializeField] private Button[] skillButtons;
    [SerializeField] private Button executeButton;

    [Header("데미지 팝업")]
    [SerializeField] private DamagePopup popupPrefab;

    private readonly List<string> _logLines = new List<string>();

    private void Start()
    {
        // 버튼 OnClick 자동 연결
        for (int i = 0; i < skillButtons.Length; i++)
        {
            if (skillButtons[i] == null) continue;
            int index = i; // 클로저 캡처용
            skillButtons[i].onClick.AddListener(() => OnSkillButton(index));
        }
        if (executeButton != null)
            executeButton.onClick.AddListener(OnExecuteButton);
    }

    private void OnEnable()
    {
        if (battleManager == null) return;
        battleManager.OnLogMessage += AddLog;
        battleManager.OnStateChanged += OnStateChanged;
        battleManager.OnDamageDealt += SpawnDamagePopup;
    }

    private void OnDisable()
    {
        if (battleManager == null) return;
        battleManager.OnLogMessage -= AddLog;
        battleManager.OnStateChanged -= OnStateChanged;
        battleManager.OnDamageDealt -= SpawnDamagePopup;
    }

    private void Update()
    {
        RefreshUnitInfo();
    }

    // ── 이벤트 핸들러 ──

    private void AddLog(string message)
    {
        _logLines.Add(message);
        while (_logLines.Count > MaxLogLines)
            _logLines.RemoveAt(0);

        if (logText != null)
            logText.text = string.Join("\n", _logLines);
    }

    private void OnStateChanged(BattleState state)
    {
        bool canInteract = state == BattleState.SkillSelect;

        foreach (var btn in skillButtons)
            if (btn != null) btn.interactable = canInteract;

        if (executeButton != null)
            executeButton.interactable = canInteract;
    }

    // ── UI 갱신 ──

    private void RefreshUnitInfo()
    {
        if (battleManager == null) return;

        var ally = battleManager.Ally;
        var enemy = battleManager.Enemy;

        if (ally != null)
        {
            if (allyNameText != null) allyNameText.text = ally.UnitName;
            if (allyHPText != null) allyHPText.text = $"{ally.CurrentHP}/{ally.MaxHP}";
        }

        if (enemy != null)
        {
            if (enemyNameText != null) enemyNameText.text = enemy.UnitName;
            if (enemyHPText != null) enemyHPText.text = $"{enemy.CurrentHP}/{enemy.MaxHP}";
        }
    }

    // ── 데미지 팝업 ──

    private void SpawnDamagePopup(Unit target, int damage)
    {
        if (popupPrefab == null || damage <= 0) return;

        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        var popup = Instantiate(popupPrefab, canvas.transform);
        var rect = popup.GetComponent<RectTransform>();

        // 피격 유닛 위치에 따라 팝업 위치 결정
        bool isAlly = target == battleManager.Ally;
        rect.anchoredPosition = isAlly
            ? new Vector2(-300, 100)   // 아군 쪽
            : new Vector2(300, 100);   // 적 쪽

        Color color = isAlly ? Color.red : Color.yellow;
        popup.Setup(damage, color);
    }

    // ── 버튼에서 호출 (Inspector OnClick에 연결) ──

    public void OnSkillButton(int index)
    {
        battleManager?.SelectSkill(index);
    }

    public void OnExecuteButton()
    {
        battleManager?.ExecuteTurn();
    }
}
