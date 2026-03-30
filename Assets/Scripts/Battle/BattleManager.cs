using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투의 중심 컨트롤러 — 상태 흐름 + 이벤트 허브만 담당.
/// 실제 로직은 전문 클래스에 위임:
///   ActionBuilder  — 턴 행동 조립 + 적 AI
///   TurnExecutor   — 합/일방/회피 해결 + 피격 연출
///   BattlePresenter — Preview/Intent/Breakdown 텍스트
///   BattleUtils    — 타입 라벨/심볼 (static)
///   DamageProcessor — 피해 계산 파이프라인 (static)
/// </summary>
public class BattleManager : MonoBehaviour
{
    // ── 인스펙터 ──
    [Header("유닛")]
    [SerializeField] private List<Unit> allyUnits = new();
    [SerializeField] private List<Unit> enemyUnits = new();

    [Header("HP바")]
    [SerializeField] private List<HPBar> allyHPBars = new();
    [SerializeField] private List<HPBar> enemyHPBars = new();

    [Header("연출")]
    [SerializeField] private CameraShake cameraShake;
    [SerializeField] private HitFlash allyFlash;
    [SerializeField] private HitFlash enemyFlash;

    // ── 프로퍼티 ──
    public Unit Ally => allyUnits.Count > 0 ? allyUnits[0] : null;
    public Unit Enemy => enemyUnits.Count > 0 ? enemyUnits[0] : null;
    public IReadOnlyList<Unit> AllyUnits => allyUnits;
    public IReadOnlyList<Unit> EnemyUnits => enemyUnits;
    public BattleState CurrentState => _fsm.Current;
    public int TurnCount => _turnCount;

    // ── 이벤트 (UI가 구독) ──
    public System.Action<string> OnLogMessage;
    public System.Action<ClashResult> OnClashResolved;
    public System.Action<BattleState> OnStateChanged;
    public System.Action<Unit, int> OnDamageDealt;
    public System.Action<string> OnBreakdownUpdated;
    public System.Action<string> OnClashPreviewUpdated;
    public System.Action<string> OnIntentUpdated;
    public System.Action<Unit, Unit> OnTargetPreviewUpdated;
    public System.Action OnHandDrawn;
    public System.Action<int, SkillData> OnCardOverridden;
    public System.Action<int, SkillData> OnCardOverridden2;
    public System.Action<Unit, Unit> OnTargetAssigned;

    // ── 내부 상태 ──
    private BattleStateMachine _fsm;
    private int _turnCount;

    // 스킬 선택 상태
    private SkillData _selectedSkill;
    private SkillData _evadeSkill;
    private int _selectedIndex = -1;
    private bool _isEvading;
    private bool _isGuarding;
    private int _guardCardIndex = -1;

    // 멀티유닛 선택
    private Dictionary<int, SkillData> _unitSelectedSkills = new();
    private Dictionary<int, int> _unitSelectedIndices = new();

    // 타겟팅
    private Unit _currentTarget;
    private Dictionary<int, Unit> _unitTargets = new();

    // 방어/회피 토글
    private Dictionary<int, bool> _unitDefenseActive = new();
    private Dictionary<int, SkillData> _unitOriginalSkill = new();

    // ── 위임 객체 ──
    private ActionBuilder _actionBuilder;
    private TurnExecutor _turnExecutor;
    private BattlePresenter _presenter;

    // ═══════════════════════════════════════
    //  초기화
    // ═══════════════════════════════════════

    private void Start()
    {
        _fsm = new BattleStateMachine();
        if (allyUnits.Count == 0 || enemyUnits.Count == 0) { Log("[오류] 유닛 미연결"); return; }

        // 위임 객체 생성
        _actionBuilder = new ActionBuilder();
        _presenter = new BattlePresenter();
        _turnExecutor = new TurnExecutor(allyUnits, enemyUnits, allyHPBars, enemyHPBars, cameraShake, _presenter);

        // 위임 객체 이벤트 → BattleManager 이벤트로 전달
        _turnExecutor.OnLogMessage = msg => Log(msg);
        _turnExecutor.OnDamageDealt = (u, d) => OnDamageDealt?.Invoke(u, d);
        _presenter.OnBreakdownUpdated = s => OnBreakdownUpdated?.Invoke(s);
        _presenter.OnClashPreviewUpdated = s => OnClashPreviewUpdated?.Invoke(s);
        _presenter.OnIntentUpdated = s => OnIntentUpdated?.Invoke(s);

        // 유닛 초기화
        foreach (var unit in allyUnits) unit.Initialize();
        foreach (var unit in enemyUnits) unit.Initialize();
        for (int i = 0; i < allyHPBars.Count && i < allyUnits.Count; i++)
            if (allyHPBars[i] != null) allyHPBars[i].Bind(allyUnits[i]);
        for (int i = 0; i < enemyHPBars.Count && i < enemyUnits.Count; i++)
            if (enemyHPBars[i] != null) enemyHPBars[i].Bind(enemyUnits[i]);

        Log(">> 전투 시작");
        DrawNewHands();
        _fsm.TransitionTo(BattleState.SkillSelect);
        OnStateChanged?.Invoke(_fsm.Current);
    }

    // ═══════════════════════════════════════
    //  스킬 선택 (UI → BattleManager)
    // ═══════════════════════════════════════

    public void SelectSkill(int index)
    {
        if (!_fsm.Is(BattleState.SkillSelect)) return;
        if (Ally.Deck == null) return;
        var hand = Ally.Deck.CurrentHand;
        if (index < 0 || index >= hand.Count) return;

        _selectedSkill = hand[index];
        _selectedIndex = index;
        _isGuarding = false;
        _isEvading = false;
        _unitOriginalSkill[0] = _selectedSkill;

        if (_unitDefenseActive.ContainsKey(0) && _unitDefenseActive[0])
        {
            _unitDefenseActive[0] = false;
            OnCardOverridden?.Invoke(index, _selectedSkill);
        }
        Log("선택: " + _selectedSkill.skillName);
        _presenter.UpdateClashPreview(_selectedSkill, Ally, Enemy);
        OnTargetPreviewUpdated?.Invoke(Ally, Enemy);
    }

    public void SelectSkillForUnit(int unitIndex, int cardIndex)
    {
        if (!_fsm.Is(BattleState.SkillSelect)) return;
        if (unitIndex < 0 || unitIndex >= allyUnits.Count) return;
        var unit = allyUnits[unitIndex];
        if (unit.Deck == null) return;
        var hand = unit.Deck.CurrentHand;
        if (cardIndex < 0 || cardIndex >= hand.Count) return;

        _unitSelectedSkills[unitIndex] = hand[cardIndex];
        _unitSelectedIndices[unitIndex] = cardIndex;
        _unitOriginalSkill[unitIndex] = hand[cardIndex];

        if (_unitDefenseActive.ContainsKey(unitIndex) && _unitDefenseActive[unitIndex])
        {
            _unitDefenseActive[unitIndex] = false;
            OnCardOverridden2?.Invoke(cardIndex, hand[cardIndex]);
        }
        Log($"[유닛{unitIndex}] 선택: {hand[cardIndex].skillName}");
    }

    // ═══════════════════════════════════════
    //  타겟팅
    // ═══════════════════════════════════════

    public void SetTarget(Unit target)
    {
        if (!_fsm.Is(BattleState.SkillSelect)) return;
        if (target == null || !target.IsAlive) return;

        int activeAlly = _selectedIndex >= 0 ? 0 : -1;
        foreach (var kv in _unitSelectedIndices)
            if (kv.Value >= 0) activeAlly = kv.Key;
        if (activeAlly < 0) activeAlly = 0;

        _unitTargets[activeAlly] = target;
        _currentTarget = target;
        Log($"[타겟] {(activeAlly < allyUnits.Count ? allyUnits[activeAlly].UnitName : "?")} → {target.UnitName}");
        OnTargetAssigned?.Invoke(activeAlly < allyUnits.Count ? allyUnits[activeAlly] : null, target);

        if (_selectedSkill != null)
            _presenter.UpdateClashPreview(_selectedSkill, Ally, Enemy);
    }

    // ═══════════════════════════════════════
    //  방어 / 회피 토글
    // ═══════════════════════════════════════

    public void SelectDefenseForUnit(int unitIndex, int cardIndex)
    {
        if (!_fsm.Is(BattleState.SkillSelect)) return;
        if (unitIndex < 0 || unitIndex >= allyUnits.Count) return;
        var unit = allyUnits[unitIndex];

        // 토글 해제
        if (_unitDefenseActive.ContainsKey(unitIndex) && _unitDefenseActive[unitIndex])
        {
            _unitDefenseActive[unitIndex] = false;
            if (unitIndex == 0)
            {
                SkillData orig = _unitOriginalSkill.ContainsKey(0) ? _unitOriginalSkill[0] : null;
                if (orig == null && Ally.Deck != null && cardIndex < Ally.Deck.CurrentHand.Count)
                    orig = Ally.Deck.CurrentHand[cardIndex];
                if (orig != null) { _selectedSkill = orig; OnCardOverridden?.Invoke(cardIndex, orig); }
                _isGuarding = false;
                _isEvading = false;
            }
            else
            {
                SkillData orig = _unitOriginalSkill.ContainsKey(unitIndex) ? _unitOriginalSkill[unitIndex] : null;
                if (orig == null && unit.Deck != null && cardIndex < unit.Deck.CurrentHand.Count)
                    orig = unit.Deck.CurrentHand[cardIndex];
                if (orig != null) OnCardOverridden2?.Invoke(cardIndex, orig);
                _unitSelectedSkills.Remove(unitIndex);
            }
            Log($"[해제] {unit.UnitName} → 공격 복귀");
            return;
        }

        // 방어/회피 스킬 찾기
        var slots = unit.SkillSlots;
        if (slots == null) return;
        SkillData defSkill = null;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            if (slots[i].skillType == SkillType.Defense || slots[i].skillType == SkillType.Evade)
            { defSkill = slots[i]; break; }
        }
        if (defSkill == null) { Log("[!] 방어/회피 스킬 없음"); return; }

        _unitDefenseActive[unitIndex] = true;

        if (unitIndex == 0)
        {
            if (_selectedSkill != null) _unitOriginalSkill[0] = _selectedSkill;
            _isGuarding = defSkill.skillType == SkillType.Defense;
            _isEvading = defSkill.skillType == SkillType.Evade;
            _guardCardIndex = cardIndex;
            _selectedSkill = defSkill;
            _selectedIndex = cardIndex;
            if (_isEvading) _evadeSkill = defSkill;
            OnCardOverridden?.Invoke(cardIndex, defSkill);
            Log($"[{(_isGuarding ? "방어" : "회피")}] {unit.UnitName} → {defSkill.skillName}");
        }
        else
        {
            var hand = unit.Deck?.CurrentHand;
            if (hand != null && cardIndex < hand.Count)
                _unitOriginalSkill[unitIndex] = hand[cardIndex];
            _unitSelectedSkills[unitIndex] = defSkill;
            _unitSelectedIndices[unitIndex] = cardIndex;
            OnCardOverridden2?.Invoke(cardIndex, defSkill);
            Log($"[{(defSkill.skillType == SkillType.Defense ? "방어" : "회피")}] {unit.UnitName} → {defSkill.skillName}");
        }
    }

    // 레거시 호환
    public void SelectEvade(int cardIndex)
    {
        _isEvading = true;
        _selectedSkill = null;
        _selectedIndex = cardIndex;
        if (Ally.Deck != null && cardIndex >= 0)
        {
            var hand = Ally.Deck.CurrentHand;
            if (cardIndex < hand.Count) _evadeSkill = hand[cardIndex];
            Ally.Deck.UseCard(cardIndex);
        }
        Log("[회피] " + Ally.UnitName + " 회피 태세");
    }

    public void OnEvadeButton()
    {
        if (_selectedIndex < 0 && !_isGuarding) return;
        int evadeIndex = _isGuarding ? _guardCardIndex : _selectedIndex;
        if (evadeIndex < 0) return;
        CancelGuard();
        _selectedIndex = evadeIndex;
        SelectEvade(_selectedIndex);
        ExecuteTurn();
    }

    public void OnGuardButton()
    {
        if (!_fsm.Is(BattleState.SkillSelect)) return;
        if (_selectedIndex < 0 && !_isGuarding) { Log("[!] 카드를 먼저 선택하세요"); return; }

        if (_isGuarding) { CancelGuard(); return; }

        var guardSkill = FindGuardSkill();
        if (guardSkill == null) { Log("[!] Guard 스킬 없음"); return; }

        _isGuarding = true;
        _guardCardIndex = _selectedIndex;
        OnCardOverridden?.Invoke(_selectedIndex, guardSkill);
        _selectedSkill = guardSkill;
        _isEvading = false;
        Log($"[방어 준비] {Ally.UnitName} → {guardSkill.skillName}");
    }

    private void CancelGuard()
    {
        if (!_isGuarding) return;
        if (Ally.Deck != null && _guardCardIndex >= 0)
        {
            var hand = Ally.Deck.CurrentHand;
            if (_guardCardIndex < hand.Count)
            {
                OnCardOverridden?.Invoke(_guardCardIndex, hand[_guardCardIndex]);
                _selectedSkill = hand[_guardCardIndex];
                _selectedIndex = _guardCardIndex;
            }
        }
        _isGuarding = false;
        _guardCardIndex = -1;
        Log("[방어 해제]");
    }

    private SkillData FindGuardSkill()
    {
        var slots = Ally.SkillSlots;
        if (slots == null) return null;
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] != null && slots[i].skillType == SkillType.Defense)
                return slots[i];
        return null;
    }

    // ═══════════════════════════════════════
    //  턴 실행 — 핵심 흐름
    // ═══════════════════════════════════════

    public void ExecuteTurn()
    {
        if (!_fsm.Is(BattleState.SkillSelect)) return;
        if (_selectedSkill == null && !_isEvading && !_isGuarding) { Log("[!] 스킬을 먼저 선택하세요"); return; }

        // Guard 처리
        bool isGuarding = _isGuarding;
        if (isGuarding)
        {
            if (Ally.Deck != null && _guardCardIndex >= 0)
                Ally.Deck.UseCard(_guardCardIndex);
            int shieldPower = CoinCalculator.RollPower(_selectedSkill, _selectedSkill.coinCount, Ally.CoinHeadsChance);
            Ally.ApplyShield(shieldPower);
            Log($"[방어] {Ally.UnitName} 방어 발동! 방어막 {shieldPower}");
        }
        else if (Ally.Deck != null && _selectedIndex >= 0)
        {
            _selectedSkill = Ally.Deck.UseCard(_selectedIndex);
        }

        // 상태 전이
        _fsm.TransitionTo(BattleState.ClashResolve);
        OnStateChanged?.Invoke(_fsm.Current);
        _turnCount++;

        // 턴 시작 처리
        foreach (var unit in allyUnits) unit.OnTurnStart();
        foreach (var unit in enemyUnits) unit.OnTurnStart();

        Log($"===== {_turnCount}턴 =====");
        foreach (var unit in allyUnits)
            if (unit.IsStaggered) Log($"{unit.UnitName} 흐트러짐! 행동 불가");
        foreach (var unit in enemyUnits)
            if (unit.IsStaggered) Log($"{unit.UnitName} 흐트러짐! 행동 불가");

        // ── ActionBuilder로 행동 목록 조립 ──
        var actions = _actionBuilder.Build(
            allyUnits, enemyUnits,
            (!_isEvading && !isGuarding && !Ally.IsStaggered) ? _selectedSkill : null,
            _selectedIndex,
            _unitSelectedSkills, _unitSelectedIndices, _unitTargets,
            GetRandomAliveEnemy, GetRandomAliveAlly, Log);

        var plan = TurnResolver.Plan(actions);

        // ── 결과 적용 ──
        _fsm.TransitionTo(BattleState.ApplyResult);
        OnStateChanged?.Invoke(_fsm.Current);

        // 합 처리
        foreach (var pair in plan.clashes)
        {
            Log($"[합] {pair.attacker.skill.skillName}({BattleUtils.GetDamageTypeLabel(pair.attacker.skill)}) vs {pair.defender.skill.skillName}({BattleUtils.GetDamageTypeLabel(pair.defender.skill)})");
            var clash = ClashResolver.Resolve(
                pair.attacker.actor, pair.attacker.skill,
                pair.defender.actor, pair.defender.skill,
                pair.attacker.speed, pair.defender.speed);
            OnClashResolved?.Invoke(clash);
            _turnExecutor.ExecuteClashDamage(clash, Ally, Enemy);
            LogClashResult(clash);
        }

        // 일방 공격 처리
        foreach (var action in plan.unopposed)
        {
            if (!action.actor.IsAlive || !action.target.IsAlive) continue;

            // 회피 판정
            if (allyUnits.Contains(action.target) && _isEvading && _evadeSkill != null)
            {
                _turnExecutor.ExecuteEvade(action, _evadeSkill, Ally);
                continue;
            }

            _turnExecutor.ExecuteUnopposed(action);
        }

        // 턴 종료
        foreach (var unit in allyUnits) { unit.ClearShield(); unit.TickParalysis(); }
        foreach (var unit in enemyUnits) { unit.ClearShield(); unit.TickParalysis(); }

        CheckBattleEnd();
        ResetTurnState();
        if (_fsm.Is(BattleState.SkillSelect)) DrawNewHands();
    }

    // ═══════════════════════════════════════
    //  내부 헬퍼
    // ═══════════════════════════════════════

    private void ResetTurnState()
    {
        _selectedSkill = null;
        _selectedIndex = -1;
        _isEvading = false;
        _isGuarding = false;
        _guardCardIndex = -1;
        _evadeSkill = null;
        _unitSelectedSkills.Clear();
        _unitSelectedIndices.Clear();
        _unitTargets.Clear();
        _currentTarget = null;
        _unitDefenseActive.Clear();
        _unitOriginalSkill.Clear();
    }

    private void DrawNewHands()
    {
        foreach (var unit in allyUnits) if (unit.Deck != null) unit.Deck.DrawHand(2);
        foreach (var unit in enemyUnits) if (unit.Deck != null) unit.Deck.DrawHand(2);
        OnHandDrawn?.Invoke();
    }

    private void CheckBattleEnd()
    {
        bool allAlliesDead = allyUnits.TrueForAll(u => !u.IsAlive);
        bool allEnemiesDead = enemyUnits.TrueForAll(u => !u.IsAlive);

        if (allAlliesDead || allEnemiesDead)
        {
            _fsm.TransitionTo(BattleState.BattleEnd);
            OnStateChanged?.Invoke(_fsm.Current);
            if (allAlliesDead && allEnemiesDead) Log("[무승부]");
            else if (allEnemiesDead) Log("[승리!]");
            else Log("[패배...]");
        }
        else
        {
            _fsm.TransitionTo(BattleState.SkillSelect);
            OnStateChanged?.Invoke(_fsm.Current);
        }
    }

    private Unit GetRandomAliveEnemy()
    {
        var alive = enemyUnits.FindAll(u => u.IsAlive);
        return alive.Count > 0 ? alive[Random.Range(0, alive.Count)] : null;
    }

    private Unit GetRandomAliveAlly()
    {
        var alive = allyUnits.FindAll(u => u.IsAlive);
        return alive.Count > 0 ? alive[Random.Range(0, alive.Count)] : null;
    }

    private void LogClashResult(ClashResult clash)
    {
        foreach (var line in clash.log.Split('|'))
        {
            var trimmed = line.Trim();
            if (trimmed.Length > 0) Log(trimmed);
        }
    }

    private void Log(string message)
    {
        Debug.Log("[Battle] " + message);
        OnLogMessage?.Invoke(message);
    }
}
