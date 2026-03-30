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
    [SerializeField] private Image allySPFill;
    [SerializeField] private TextMeshProUGUI enemyNameText;
    [SerializeField] private TextMeshProUGUI enemyHPText;
    [SerializeField] private TextMeshProUGUI enemySPText;
    [SerializeField] private Image enemySPFill;

    [Header("전투 로그")]
    [SerializeField] private TextMeshProUGUI logText;
    [SerializeField] private TextMeshProUGUI breakdownText;
    [SerializeField] private TextMeshProUGUI clashPreviewText;
    [SerializeField] private TextMeshProUGUI intentText;

    [Header("쇼케이스 모드")]
    [SerializeField] private bool showcaseMode = true;
    [SerializeField] private GameObject logPanelRoot;
    [SerializeField] private GameObject breakdownPanelRoot;

    [Header("스킬 카드")]
    [SerializeField] private SkillCardUI[] skillCards;       // 아군1 카드
    [SerializeField] private SkillCardUI[] skillCards2;      // 아군2 카드
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
            for (int i = 0; i < skillCards.Length; i++)
            {
                if (skillCards[i] == null) continue;
                int index = i;

                // 카드 버튼 클릭 연결
                var btn = skillCards[i].GetComponentInChildren<Button>();
                if (btn != null)
                    btn.onClick.AddListener(() => OnSkillCardClicked(index));

                // 우클릭 → 방어 (아군1 = 이상)
                int rIdx = i;
                skillCards[i].OnRightClicked = () => OnSkillCardRightClicked(0, rIdx);
            }

            RefreshSkillCards();
        }

        // 아군2 스킬 카드 초기화
        if (skillCards2 != null && skillCards2.Length > 0 && battleManager != null)
        {
            for (int i = 0; i < skillCards2.Length; i++)
            {
                if (skillCards2[i] == null) continue;
                int index = i;
                var btn = skillCards2[i].GetComponentInChildren<Button>();
                if (btn != null)
                    btn.onClick.AddListener(() => OnSkillCard2Clicked(index));

                // 우클릭 → 회피 (아군2 = 파우스트)
                int rIdx2 = i;
                skillCards2[i].OnRightClicked = () => OnSkillCardRightClicked(1, rIdx2);
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

        ApplyPresentationMode();
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
        battleManager.OnBreakdownUpdated += UpdateBreakdown;
        battleManager.OnClashPreviewUpdated += UpdateClashPreview;
        battleManager.OnIntentUpdated += UpdateIntent;
        battleManager.OnTargetPreviewUpdated += UpdateTargetPreview;
        battleManager.OnHandDrawn += RefreshSkillCards;
        battleManager.OnCardOverridden += OnCardOverridden;
        battleManager.OnCardOverridden2 += OnCardOverridden2;
    }

    private void OnDisable()
    {
        if (battleManager == null) return;
        battleManager.OnLogMessage -= AddLog;
        battleManager.OnStateChanged -= OnStateChanged;
        battleManager.OnDamageDealt -= SpawnDamagePopup;
        battleManager.OnBreakdownUpdated -= UpdateBreakdown;
        battleManager.OnClashPreviewUpdated -= UpdateClashPreview;
        battleManager.OnIntentUpdated -= UpdateIntent;
        battleManager.OnTargetPreviewUpdated -= UpdateTargetPreview;
        battleManager.OnHandDrawn -= RefreshSkillCards;
        battleManager.OnCardOverridden -= OnCardOverridden;
        battleManager.OnCardOverridden2 -= OnCardOverridden2;
    }

    private void RefreshSkillCards()
    {
        // 아군1 카드
        RefreshCardSet(skillCards, battleManager?.Ally);
        // 아군2 카드
        Unit ally2 = null;
        if (battleManager != null && battleManager.AllyUnits != null && battleManager.AllyUnits.Count > 1)
            ally2 = battleManager.AllyUnits[1];
        RefreshCardSet(skillCards2, ally2);
    }

    private void RefreshCardSet(SkillCardUI[] cards, Unit unit)
    {
        if (cards == null) return;
        var hand = unit?.Deck?.CurrentHand;
        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null) continue;
            if (hand != null && i < hand.Count && hand[i] != null)
            {
                cards[i].gameObject.SetActive(true);
                cards[i].Setup(hand[i]);
            }
            else
            {
                cards[i].gameObject.SetActive(false);
            }
        }
    }

    private void Update()
    {
        RefreshUnitInfo();
    }

    private void ApplyPresentationMode()
    {
        if (showcaseMode)
        {
            if (logPanelRoot != null) logPanelRoot.SetActive(false);
            if (breakdownPanelRoot != null) breakdownPanelRoot.SetActive(false);
            else if (breakdownText != null) breakdownText.gameObject.SetActive(false);
        }
        else
        {
            if (logPanelRoot != null) logPanelRoot.SetActive(true);
            if (breakdownPanelRoot != null) breakdownPanelRoot.SetActive(true);
            else if (breakdownText != null) breakdownText.gameObject.SetActive(true);
        }
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

    private void UpdateBreakdown(string message)
    {
        if (breakdownText != null)
            breakdownText.text = message;
    }

    private void UpdateClashPreview(string message)
    {
        if (clashPreviewText != null)
            clashPreviewText.text = message;
    }

    private void UpdateIntent(string message)
    {
        if (intentText != null)
            intentText.text = message;
    }

    private void UpdateTargetPreview(Unit source, Unit target)
    {
        if (battleManager == null) return;

        var allyRenderer = battleManager.Ally != null ? battleManager.Ally.GetComponent<SpriteRenderer>() : null;
        var enemyRenderer = battleManager.Enemy != null ? battleManager.Enemy.GetComponent<SpriteRenderer>() : null;

        Color dim = new Color(0.65f, 0.65f, 0.65f, 1f);
        Color normal = Color.white;
        Color highlight = new Color(1f, 0.92f, 0.65f, 1f);

        if (allyRenderer != null) allyRenderer.color = normal;
        if (enemyRenderer != null) enemyRenderer.color = normal;

        if (target == battleManager.Ally)
        {
            if (allyRenderer != null) allyRenderer.color = highlight;
            if (enemyRenderer != null) enemyRenderer.color = dim;
        }
        else if (target == battleManager.Enemy)
        {
            if (enemyRenderer != null) enemyRenderer.color = highlight;
            if (allyRenderer != null) allyRenderer.color = dim;
        }
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
                allySPText.text = $"SP: {ally.SP}";
                allySPText.color = ally.SP >= 0
                    ? new Color(0.5f, 0.8f, 1f)
                    : new Color(1f, 0.4f, 0.4f);
            }
            if (allySPFill != null)
            {
                float ratio = Mathf.InverseLerp(-45f, 45f, ally.SP);
                allySPFill.fillAmount = ratio;
                allySPFill.color = ally.SP >= 0
                    ? new Color(0.35f, 0.8f, 1f)
                    : new Color(1f, 0.45f, 0.45f);
            }
        }

        if (enemy != null)
        {
            if (enemyNameText != null) enemyNameText.text = enemy.UnitName;
            if (enemyHPText != null) enemyHPText.text = $"{enemy.CurrentHP}/{enemy.MaxHP}";
            if (enemySPText != null)
            {
                enemySPText.text = $"SP: {enemy.SP}";
                enemySPText.color = enemy.SP >= 0
                    ? new Color(0.5f, 0.8f, 1f)
                    : new Color(1f, 0.4f, 0.4f);
            }
            if (enemySPFill != null)
            {
                float ratio = Mathf.InverseLerp(-45f, 45f, enemy.SP);
                enemySPFill.fillAmount = ratio;
                enemySPFill.color = enemy.SP >= 0
                    ? new Color(0.35f, 0.8f, 1f)
                    : new Color(1f, 0.45f, 0.45f);
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

    private void OnSkillCardRightClicked(int unitIndex, int cardIndex)
    {
        battleManager?.SelectDefenseForUnit(unitIndex, cardIndex);

        // 파우스트 카드 비주얼 직접 갱신 (OnCardOverridden은 아군1 기준)
        if (unitIndex > 0 && skillCards2 != null && cardIndex < skillCards2.Length)
        {
            // 현재 상태 확인 후 갱신은 OnCardOverridden에서 처리
        }
    }

    private void OnSkillCard2Clicked(int index)
    {
        // 아군2 카드 선택 시각 피드백
        if (skillCards2 != null)
            for (int i = 0; i < skillCards2.Length; i++)
                if (skillCards2[i] != null)
                    skillCards2[i].SetSelected(i == index);

        battleManager?.SelectSkillForUnit(1, index);
    }

    private void OnCardOverridden(int cardIndex, SkillData newSkill)
    {
        if (skillCards != null && cardIndex >= 0 && cardIndex < skillCards.Length)
            if (skillCards[cardIndex] != null)
                skillCards[cardIndex].Setup(newSkill);
    }

    private void OnCardOverridden2(int cardIndex, SkillData newSkill)
    {
        if (skillCards2 != null && cardIndex >= 0 && cardIndex < skillCards2.Length)
            if (skillCards2[cardIndex] != null)
                skillCards2[cardIndex].Setup(newSkill);
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
