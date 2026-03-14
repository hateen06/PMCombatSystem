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
    private const int MaxLogLines = 10;

    [Header("연결")]
    [SerializeField] private BattleManager battleManager;

    [Header("유닛 정보")]
    [SerializeField] private TextMeshProUGUI allyNameText;
    [SerializeField] private TextMeshProUGUI allyHPText;
    [SerializeField] private TextMeshProUGUI allySPText;
    [SerializeField] private TextMeshProUGUI enemyNameText;
    [SerializeField] private TextMeshProUGUI enemyHPText;
    [SerializeField] private TextMeshProUGUI enemySPText;

    [Header("전투 로그")]
    [SerializeField] private TextMeshProUGUI logText;

    [Header("스킬 카드")]
    [SerializeField] private SkillCardUI[] skillCards;
    [SerializeField] private Button executeButton;

    [Header("데미지 팝업")]
    [SerializeField] private DamagePopup popupPrefab;

    // 하위호환 — 기존 버튼도 유지
    [Header("레거시 버튼 (카드 없을 때)")]
    [SerializeField] private Button[] skillButtons;

    private readonly List<string> _logLines = new List<string>();

    private int _selectedIndex = -1;

    private void Start()
    {
        // 스킬 카드 초기화
        if (skillCards != null && skillCards.Length > 0 && battleManager != null)
        {
            var skills = battleManager.Ally?.SkillSlots;
            for (int i = 0; i < skillCards.Length; i++)
            {
                if (skillCards[i] == null) continue;
                int index = i;

                // 데이터 연결
                if (skills != null && i < skills.Length)
                    skillCards[i].Setup(skills[i]);

                // 카드 버튼 클릭 연결
                var btn = skillCards[i].GetComponentInChildren<Button>();
                if (btn != null)
                    btn.onClick.AddListener(() => OnSkillCardClicked(index));
            }
        }

        // 레거시 버튼 OnClick (카드 없을 때 폴백)
        if (skillButtons != null)
        {
            for (int i = 0; i < skillButtons.Length; i++)
            {
                if (skillButtons[i] == null) continue;
                int index = i;
                skillButtons[i].onClick.AddListener(() => OnSkillButton(index));
            }
        }

        if (executeButton != null)
            executeButton.onClick.AddListener(OnExecuteButton);
    }

    private void OnSkillCardClicked(int index)
    {
        _selectedIndex = index;

        // 선택 시각 피드백
        for (int i = 0; i < skillCards.Length; i++)
            if (skillCards[i] != null)
                skillCards[i].SetSelected(i == index);

        OnSkillButton(index);
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

        // 스킬 카드
        if (skillCards != null)
            foreach (var card in skillCards)
                if (card != null) card.SetInteractable(canInteract);

        // 레거시 버튼
        if (skillButtons != null)
            foreach (var btn in skillButtons)
                if (btn != null) btn.interactable = canInteract;

        if (executeButton != null)
            executeButton.interactable = canInteract;

        // 턴 끝나면 선택 초기화
        if (canInteract)
        {
            _selectedIndex = -1;
            if (skillCards != null)
                foreach (var card in skillCards)
                    if (card != null) card.SetSelected(false);
        }
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
            if (allySPText != null)
            {
                allySPText.text = $"SP: {ally.SP}  ({ally.CoinHeadsChance}%)";
                allySPText.color = ally.SP >= 0
                    ? new Color(0.5f, 0.8f, 1f)
                    : new Color(1f, 0.4f, 0.4f);
            }
        }

        if (enemy != null)
        {
            if (enemyNameText != null) enemyNameText.text = enemy.UnitName;
            if (enemyHPText != null) enemyHPText.text = $"{enemy.CurrentHP}/{enemy.MaxHP}";
            if (enemySPText != null)
            {
                enemySPText.text = $"SP: {enemy.SP}  ({enemy.CoinHeadsChance}%)";
                enemySPText.color = enemy.SP >= 0
                    ? new Color(0.5f, 0.8f, 1f)
                    : new Color(1f, 0.4f, 0.4f);
            }
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
