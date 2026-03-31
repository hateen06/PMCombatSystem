using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
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
    [SerializeField] private SkillCardUI[] skillCards3;      // 아군3 카드
    [SerializeField] private Button executeButton;

    [Header("데미지 팝업")]
    [SerializeField] private DamagePopup popupPrefab;

    [Header("속도 다이스")]
    [SerializeField] private SpeedDiceUI speedDicePrefab;
    [SerializeField] private Transform speedDiceRoot;

    [Header("속도 타임라인")]
    [SerializeField] private SpeedTimelineUI speedTimeline;

    [Header("타겟 라인")]
    [SerializeField] private TargetLineUI targetLinePrefab;
    [SerializeField] private Transform targetLineRoot;

    // 하위호환 — 기존 버튼도 유지
    [Header("레거시 버튼 (카드 없을 때)")]
    [SerializeField] private Button[] skillButtons;

    private readonly List<string> _logLines = new List<string>();
    private readonly Dictionary<Unit, SpeedDiceUI> _speedDiceMap = new Dictionary<Unit, SpeedDiceUI>();
    private readonly List<TargetLineUI> _targetLines = new List<TargetLineUI>();
    private readonly HashSet<Unit> _boundUnits = new HashSet<Unit>();
    private readonly Dictionary<Unit, ResistanceIndicatorUI> _resistIndicators = new();
    private readonly Dictionary<Unit, SpeedDiceUI> _enemyIntentIcons = new();

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
            BindObservedData();
            EnsureSpeedTimeline();
            CreateColorVeils();
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

                int rIdx2 = i;
                skillCards2[i].OnRightClicked = () => OnSkillCardRightClicked(1, rIdx2);
            }
        }

        // 아군3 스킬 카드 초기화
        if (skillCards3 != null && skillCards3.Length > 0 && battleManager != null)
        {
            for (int i = 0; i < skillCards3.Length; i++)
            {
                if (skillCards3[i] == null) continue;
                int index = i;
                var btn = skillCards3[i].GetComponentInChildren<Button>();
                if (btn != null)
                    btn.onClick.AddListener(() => OnSkillCard3Clicked(index));

                int rIdx3 = i;
                skillCards3[i].OnRightClicked = () => OnSkillCardRightClicked(2, rIdx3);
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
        battleManager.OnSpeedRolled += UpdateSpeedDice;
        battleManager.OnTargetLineUpdated += UpdateTargetLine;
        battleManager.OnClashPairHighlighted += OnClashPairHighlighted;
        battleManager.OnEnemyIntentRevealed += OnEnemyIntentRevealed;
        battleManager.OnHandDrawn += RefreshSkillCards;
        battleManager.OnCardOverridden += OnCardOverridden;
        battleManager.OnCardOverridden2 += OnCardOverridden2;
        battleManager.OnCardOverridden3 += OnCardOverridden3;
        BindObservedData();
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
        battleManager.OnSpeedRolled -= UpdateSpeedDice;
        battleManager.OnTargetLineUpdated -= UpdateTargetLine;
        battleManager.OnClashPairHighlighted -= OnClashPairHighlighted;
        battleManager.OnEnemyIntentRevealed -= OnEnemyIntentRevealed;
        battleManager.OnHandDrawn -= RefreshSkillCards;
        battleManager.OnCardOverridden -= OnCardOverridden;
        battleManager.OnCardOverridden2 -= OnCardOverridden2;
        battleManager.OnCardOverridden3 -= OnCardOverridden3;
        UnbindObservedData();
    }

    private void BindObservedData()
    {
        if (battleManager == null) return;
        foreach (var unit in battleManager.AllyUnits)
            BindUnit(unit);
        foreach (var unit in battleManager.EnemyUnits)
            BindUnit(unit);
        RefreshUnitInfo();
        RefreshSkillCards();
    }

    private void UnbindObservedData()
    {
        foreach (var unit in _boundUnits)
        {
            if (unit == null) continue;
            unit.OnHPChanged -= HandleUnitHPChanged;
            unit.OnSPChanged -= HandleUnitSPChanged;
            unit.OnStatusChanged -= HandleUnitStatusChanged;
            unit.OnStaggerChanged -= HandleStaggerChanged;
            if (unit.Deck != null) unit.Deck.OnHandChanged -= HandleHandChanged;
        }
        _boundUnits.Clear();
    }

    private void BindUnit(Unit unit)
    {
        if (unit == null || _boundUnits.Contains(unit)) return;
        unit.OnHPChanged += HandleUnitHPChanged;
        unit.OnSPChanged += HandleUnitSPChanged;
        unit.OnStatusChanged += HandleUnitStatusChanged;
        unit.OnStaggerChanged += HandleStaggerChanged;
        if (unit.Deck != null) unit.Deck.OnHandChanged += HandleHandChanged;
        _boundUnits.Add(unit);
    }

    private void CreateColorVeils()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        void MakeVeil(string name, float xMin, float xMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(canvas.transform, false);
            go.transform.SetAsFirstSibling();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(xMin, 0f);
            rt.anchorMax = new Vector2(xMax, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.GetComponent<UnityEngine.UI.Image>().color = color;
            go.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        }

        MakeVeil("AllyVeil", 0f, 0.4f, new Color(0.125f, 0.2f, 0.286f, 0.14f));
        MakeVeil("EnemyVeil", 0.6f, 1f, new Color(0.29f, 0.15f, 0.15f, 0.16f));
    }

    private void EnsureSpeedTimeline()
    {
        if (speedTimeline != null) return;
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;
        var go = new GameObject("SpeedTimeline", typeof(RectTransform), typeof(SpeedTimelineUI));
        go.transform.SetParent(canvas.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.88f);
        rt.anchorMax = new Vector2(0.9f, 0.97f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        speedTimeline = go.GetComponent<SpeedTimelineUI>();
    }

    private void OnClashPairHighlighted(Unit a, Unit b)
    {
        if (speedTimeline != null) speedTimeline.HighlightClashPair(a, b);
    }

    private void OnEnemyIntentRevealed(Unit enemy, SkillData skill)
    {
        if (enemy == null || skill == null) return;
        if (speedTimeline != null)
            speedTimeline.AddUnit(enemy, 0, true, skill);

        // 저항 표시: 모든 아군에 대해 이 스킬의 피해 타입 기준 저항 표시
        if (battleManager == null) return;
        foreach (var ally in battleManager.AllyUnits)
        {
            if (ally == null || !ally.IsAlive) continue;
            if (!_resistIndicators.TryGetValue(ally, out var indicator) || indicator == null)
            {
                var canvas = GetComponentInParent<Canvas>();
                if (canvas == null) continue;
                var go = new GameObject($"Resist_{ally.UnitName}", typeof(RectTransform), typeof(ResistanceIndicatorUI));
                go.transform.SetParent(canvas.transform, false);
                indicator = go.GetComponent<ResistanceIndicatorUI>();
                indicator.Bind(ally, canvas);
                _resistIndicators[ally] = indicator;
            }
            indicator.Show(skill.damageType);
        }
    }

    private void HandleStaggerChanged(bool isStaggered, int count)
    {
        // 이벤트 발신자 특정 불가하므로 전체 순회 — 각 유닛의 실제 상태 기준
        foreach (var unit in _boundUnits)
        {
            if (unit == null) continue;
            var sr = unit.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = unit.IsStaggered ? new Color(0.6f, 0.4f, 0.4f, 1f) : Color.white;
        }
        // 타임라인 dim 처리
        if (speedTimeline != null)
        {
            foreach (var unit in _boundUnits)
                if (unit != null && unit.IsStaggered)
                    speedTimeline.AddUnit(unit, 0, battleManager.EnemyUnits.Contains(unit));
        }
    }

    private void HandleUnitHPChanged(int current, int max) => RefreshUnitInfo();
    private void HandleUnitSPChanged(int sp) => RefreshUnitInfo();
    private void HandleUnitStatusChanged() => RefreshUnitInfo();
    private void HandleHandChanged(IReadOnlyList<SkillData> hand) => RefreshSkillCards();

    private void RefreshSkillCards()
    {
        RefreshCardSet(skillCards, battleManager?.Ally);

        Unit ally2 = null;
        if (battleManager != null && battleManager.AllyUnits != null && battleManager.AllyUnits.Count > 1)
            ally2 = battleManager.AllyUnits[1];
        RefreshCardSet(skillCards2, ally2);

        Unit ally3 = null;
        if (battleManager != null && battleManager.AllyUnits != null && battleManager.AllyUnits.Count > 2)
            ally3 = battleManager.AllyUnits[2];
        RefreshCardSet(skillCards3, ally3);
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

    private void UpdateSpeedDice(Unit unit, int speed)
    {
        if (unit == null) return;

        bool isEnemy = battleManager != null && battleManager.EnemyUnits != null &&
            battleManager.EnemyUnits.Contains(unit);

        if (speedTimeline != null)
            speedTimeline.AddUnit(unit, speed, isEnemy);

        if (!_speedDiceMap.TryGetValue(unit, out var dice) || dice == null)
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            var parent = speedDiceRoot != null ? speedDiceRoot : canvas.transform;

            if (speedDicePrefab != null)
                dice = Instantiate(speedDicePrefab, parent);
            else
            {
                var go = new GameObject($"SpeedDice_{unit.UnitName}", typeof(RectTransform), typeof(SpeedDiceUI));
                go.transform.SetParent(parent, false);
                dice = go.GetComponent<SpeedDiceUI>();
            }

            dice.Bind(unit, canvas, isEnemy);
            _speedDiceMap[unit] = dice;
        }

        dice.SetValue(speed, highlighted: speed >= 5);
    }

    private void UpdateTargetLine(Unit from, Unit to, bool isClash)
    {
        if (from == null || to == null) return;

        var parent = targetLineRoot != null ? targetLineRoot : transform;
        TargetLineUI line;
        if (targetLinePrefab != null)
        {
            line = Instantiate(targetLinePrefab, parent);
        }
        else
        {
            var go = new GameObject($"TargetLine_{from.UnitName}_to_{to.UnitName}", typeof(LineRenderer), typeof(TargetLineUI));
            go.transform.SetParent(parent, false);
            line = go.GetComponent<TargetLineUI>();
        }
        line.SetTargets(from, to, isClash);
        _targetLines.Add(line);
    }

    private void OnStateChanged(BattleState state)
    {
        bool canInteract = state == BattleState.SkillSelect;

        // 스킬 카드
        if (skillCards != null)
            foreach (var card in skillCards)
                if (card != null) card.SetInteractable(canInteract);
        if (skillCards2 != null)
            foreach (var card in skillCards2)
                if (card != null) card.SetInteractable(canInteract);
        if (skillCards3 != null)
            foreach (var card in skillCards3)
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

            if (speedTimeline != null) speedTimeline.Clear();

            foreach (var kv in _resistIndicators)
                if (kv.Value != null) kv.Value.Hide();

            for (int i = 0; i < _targetLines.Count; i++)
                if (_targetLines[i] != null) Destroy(_targetLines[i].gameObject);
            _targetLines.Clear();

            if (skillCards != null)
                foreach (var card in skillCards)
                    if (card != null) card.SetSelected(false);
            if (skillCards2 != null)
                foreach (var card in skillCards2)
                    if (card != null) card.SetSelected(false);
            if (skillCards3 != null)
                foreach (var card in skillCards3)
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
                allySPText.text = $"SP: {ally.SP} ({ally.CoinHeadsChance}%)";
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
                enemySPText.text = $"SP: {enemy.SP} ({enemy.CoinHeadsChance}%)";
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
        if (canvas == null || Camera.main == null) return;

        var popup = Instantiate(popupPrefab, canvas.transform);
        var rect = popup.GetComponent<RectTransform>();

        // 유닛 월드 좌표 → 스크린 → Canvas 좌표
        Vector3 worldPos = target.transform.position + new Vector3(0f, 1.2f, 0f); // 머리 위
        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(), screenPos, canvas.worldCamera, out Vector2 canvasPos);
        rect.anchoredPosition = canvasPos + new Vector2(Random.Range(-20f, 20f), Random.Range(0f, 30f));

        // 아군 피격 = 빨강, 적 피격 = 노랑
        bool isAlly = battleManager.AllyUnits != null && 
            ((System.Collections.Generic.IList<Unit>)battleManager.AllyUnits).Contains(target);
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
        if (skillCards2 != null)
            for (int i = 0; i < skillCards2.Length; i++)
                if (skillCards2[i] != null)
                    skillCards2[i].SetSelected(i == index);

        battleManager?.SelectSkillForUnit(1, index);
    }

    private void OnSkillCard3Clicked(int index)
    {
        if (skillCards3 != null)
            for (int i = 0; i < skillCards3.Length; i++)
                if (skillCards3[i] != null)
                    skillCards3[i].SetSelected(i == index);

        battleManager?.SelectSkillForUnit(2, index);
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

    private void OnCardOverridden3(int cardIndex, SkillData newSkill)
    {
        if (skillCards3 != null && cardIndex >= 0 && cardIndex < skillCards3.Length)
            if (skillCards3[cardIndex] != null)
                skillCards3[cardIndex].Setup(newSkill);
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
