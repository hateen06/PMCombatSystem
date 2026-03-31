using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
    public System.Action<Unit, int> OnSpeedRolled;
    public System.Action<Unit, Unit, bool> OnTargetLineUpdated;
    public System.Action<Unit, Unit> OnClashPairHighlighted;
    public System.Action<Unit, SkillData> OnEnemyIntentRevealed;
    public System.Action OnHandDrawn;
    public System.Action<int, SkillData> OnCardOverridden;
    public System.Action<int, SkillData> OnCardOverridden2;
    public System.Action<Unit, Unit> OnTargetAssigned;

    // ── 내부 상태 ──
    private BattleStateMachine _fsm;
    private int _turnCount;

    private TurnSelectionState _sel = new();

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

        _sel.selectedSkill = hand[index];
        _sel.selectedIndex = index;
        _sel.isGuarding = false;
        _sel.isEvading = false;
        _sel.unitOriginalSkill[0] = _sel.selectedSkill;

        if (_sel.unitDefenseActive.ContainsKey(0) && _sel.unitDefenseActive[0])
        {
            _sel.unitDefenseActive[0] = false;
            OnCardOverridden?.Invoke(index, _sel.selectedSkill);
        }
        Log("선택: " + _sel.selectedSkill.skillName);
        _presenter.UpdateClashPreview(_sel.selectedSkill, Ally, Enemy);
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

        _sel.unitSelectedSkills[unitIndex] = hand[cardIndex];
        _sel.unitSelectedIndices[unitIndex] = cardIndex;
        _sel.unitOriginalSkill[unitIndex] = hand[cardIndex];

        if (_sel.unitDefenseActive.ContainsKey(unitIndex) && _sel.unitDefenseActive[unitIndex])
        {
            _sel.unitDefenseActive[unitIndex] = false;
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

        int activeAlly = _sel.selectedIndex >= 0 ? 0 : -1;
        foreach (var kv in _sel.unitSelectedIndices)
            if (kv.Value >= 0) activeAlly = kv.Key;
        if (activeAlly < 0) activeAlly = 0;

        _sel.unitTargets[activeAlly] = target;
        _sel.currentTarget = target;
        Log($"[타겟] {(activeAlly < allyUnits.Count ? allyUnits[activeAlly].UnitName : "?")} → {target.UnitName}");
        OnTargetAssigned?.Invoke(activeAlly < allyUnits.Count ? allyUnits[activeAlly] : null, target);

        if (_sel.selectedSkill != null)
            _presenter.UpdateClashPreview(_sel.selectedSkill, Ally, Enemy);

        // 현재 타겟 라인 프리뷰
        if (activeAlly < allyUnits.Count)
            OnTargetLineUpdated?.Invoke(allyUnits[activeAlly], target, false);
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
        if (_sel.unitDefenseActive.ContainsKey(unitIndex) && _sel.unitDefenseActive[unitIndex])
        {
            _sel.unitDefenseActive[unitIndex] = false;
            if (unitIndex == 0)
            {
                SkillData orig = _sel.unitOriginalSkill.ContainsKey(0) ? _sel.unitOriginalSkill[0] : null;
                if (orig == null && Ally.Deck != null && cardIndex < Ally.Deck.CurrentHand.Count)
                    orig = Ally.Deck.CurrentHand[cardIndex];
                if (orig != null) { _sel.selectedSkill = orig; OnCardOverridden?.Invoke(cardIndex, orig); }
                _sel.isGuarding = false;
                _sel.isEvading = false;
            }
            else
            {
                SkillData orig = _sel.unitOriginalSkill.ContainsKey(unitIndex) ? _sel.unitOriginalSkill[unitIndex] : null;
                if (orig == null && unit.Deck != null && cardIndex < unit.Deck.CurrentHand.Count)
                    orig = unit.Deck.CurrentHand[cardIndex];
                if (orig != null) OnCardOverridden2?.Invoke(cardIndex, orig);
                _sel.unitSelectedSkills.Remove(unitIndex);
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

        _sel.unitDefenseActive[unitIndex] = true;

        if (unitIndex == 0)
        {
            if (_sel.selectedSkill != null) _sel.unitOriginalSkill[0] = _sel.selectedSkill;
            _sel.isGuarding = defSkill.skillType == SkillType.Defense;
            _sel.isEvading = defSkill.skillType == SkillType.Evade;
            _sel.guardCardIndex = cardIndex;
            _sel.selectedSkill = defSkill;
            _sel.selectedIndex = cardIndex;
            if (_sel.isEvading) _sel.evadeSkill = defSkill;
            OnCardOverridden?.Invoke(cardIndex, defSkill);
            Log($"[{(_sel.isGuarding ? "방어" : "회피")}] {unit.UnitName} → {defSkill.skillName}");
        }
        else
        {
            var hand = unit.Deck?.CurrentHand;
            if (hand != null && cardIndex < hand.Count)
                _sel.unitOriginalSkill[unitIndex] = hand[cardIndex];
            _sel.unitSelectedSkills[unitIndex] = defSkill;
            _sel.unitSelectedIndices[unitIndex] = cardIndex;
            OnCardOverridden2?.Invoke(cardIndex, defSkill);
            Log($"[{(defSkill.skillType == SkillType.Defense ? "방어" : "회피")}] {unit.UnitName} → {defSkill.skillName}");
        }
    }

    // 레거시 호환
    public void SelectEvade(int cardIndex)
    {
        _sel.isEvading = true;
        _sel.selectedSkill = null;
        _sel.selectedIndex = cardIndex;
        if (Ally.Deck != null && cardIndex >= 0)
        {
            var hand = Ally.Deck.CurrentHand;
            if (cardIndex < hand.Count) _sel.evadeSkill = hand[cardIndex];
            Ally.Deck.UseCard(cardIndex);
        }
        Log("[회피] " + Ally.UnitName + " 회피 태세");
    }

    public void OnEvadeButton()
    {
        if (_sel.selectedIndex < 0 && !_sel.isGuarding) return;
        int evadeIndex = _sel.isGuarding ? _sel.guardCardIndex : _sel.selectedIndex;
        if (evadeIndex < 0) return;
        CancelGuard();
        _sel.selectedIndex = evadeIndex;
        SelectEvade(_sel.selectedIndex);
        ExecuteTurn();
    }

    public void OnGuardButton()
    {
        if (!_fsm.Is(BattleState.SkillSelect)) return;
        if (_sel.selectedIndex < 0 && !_sel.isGuarding) { Log("[!] 카드를 먼저 선택하세요"); return; }

        if (_sel.isGuarding) { CancelGuard(); return; }

        var guardSkill = FindGuardSkill();
        if (guardSkill == null) { Log("[!] Guard 스킬 없음"); return; }

        _sel.isGuarding = true;
        _sel.guardCardIndex = _sel.selectedIndex;
        OnCardOverridden?.Invoke(_sel.selectedIndex, guardSkill);
        _sel.selectedSkill = guardSkill;
        _sel.isEvading = false;
        Log($"[방어 준비] {Ally.UnitName} → {guardSkill.skillName}");
    }

    private void CancelGuard()
    {
        if (!_sel.isGuarding) return;
        if (Ally.Deck != null && _sel.guardCardIndex >= 0)
        {
            var hand = Ally.Deck.CurrentHand;
            if (_sel.guardCardIndex < hand.Count)
            {
                OnCardOverridden?.Invoke(_sel.guardCardIndex, hand[_sel.guardCardIndex]);
                _sel.selectedSkill = hand[_sel.guardCardIndex];
                _sel.selectedIndex = _sel.guardCardIndex;
            }
        }
        _sel.isGuarding = false;
        _sel.guardCardIndex = -1;
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

    public void SelectEgo(int unitIndex = 0)
    {
        if (!_fsm.Is(BattleState.SkillSelect)) return;
        if (unitIndex < 0 || unitIndex >= allyUnits.Count) return;
        var unit = allyUnits[unitIndex];
        if (!unit.CanUseEgo()) { Log("[!] E.G.O 사용 불가 (SP 부족)"); return; }

        var ego = unit.UseEgo();
        if (ego == null) return;

        _sel.selectedSkill = ego;
        _sel.selectedIndex = -1;
        _sel.isGuarding = false;
        _sel.isEvading = false;
        Log($"[E.G.O] {unit.UnitName} → {ego.skillName} (SP -{ego.egoCost})");
        ExecuteTurn();
    }

    public void ExecuteTurn()
    {
        if (!_fsm.Is(BattleState.SkillSelect)) return;
        if (_sel.selectedSkill == null && !_sel.isEvading && !_sel.isGuarding) { Log("[!] 스킬을 먼저 선택하세요"); return; }
        StartCoroutine(ExecuteTurnCoroutine());
    }

    private IEnumerator ExecuteTurnCoroutine()
    {

        // Guard 처리
        bool isGuarding = _sel.isGuarding;
        if (isGuarding)
        {
            if (Ally.Deck != null && _sel.guardCardIndex >= 0)
                Ally.Deck.UseCard(_sel.guardCardIndex);
            int shieldPower = CoinCalculator.RollPower(_sel.selectedSkill, _sel.selectedSkill.coinCount, Ally.CoinHeadsChance);
            Ally.ApplyShield(shieldPower);
            Log($"[방어] {Ally.UnitName} 방어 발동! 방어막 {shieldPower}");
        }
        else if (Ally.Deck != null && _sel.selectedIndex >= 0)
        {
            _sel.selectedSkill = Ally.Deck.UseCard(_sel.selectedIndex);
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
            (!_sel.isEvading && !isGuarding && !Ally.IsStaggered) ? _sel.selectedSkill : null,
            _sel.selectedIndex,
            _sel.unitSelectedSkills, _sel.unitSelectedIndices, _sel.unitTargets,
            GetRandomAliveEnemy, GetRandomAliveAlly, Log);

        // 속도 다이스 표시 갱신
        foreach (var action in actions)
        {
            OnSpeedRolled?.Invoke(action.actor, action.speed);
            if (enemyUnits.Contains(action.actor))
                OnEnemyIntentRevealed?.Invoke(action.actor, action.skill);
        }

        var plan = TurnResolver.Plan(actions);

        // 계획된 타겟 라인 표시 (합/일방 구분)
        foreach (var pair in plan.clashes)
        {
            OnTargetLineUpdated?.Invoke(pair.attacker.actor, pair.defender.actor, true);
            OnTargetLineUpdated?.Invoke(pair.defender.actor, pair.attacker.actor, true);
            OnClashPairHighlighted?.Invoke(pair.attacker.actor, pair.defender.actor);
        }
        foreach (var action in plan.unopposed)
            OnTargetLineUpdated?.Invoke(action.actor, action.target, false);

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
            _turnExecutor.ApplyClashSideEffects(clash, Ally, Enemy);
            LogClashResult(clash);
            yield return new WaitForSeconds(0.5f);
        }

        yield return new WaitForSeconds(0.2f);

        // 일방 공격 처리
        foreach (var action in plan.unopposed)
        {
            if (!action.actor.IsAlive || !action.target.IsAlive) continue;

            // 회피 판정
            if (allyUnits.Contains(action.target) && _sel.isEvading && _sel.evadeSkill != null)
            {
                _turnExecutor.ExecuteEvade(action, _sel.evadeSkill, Ally);
                yield return new WaitForSeconds(0.4f);
                continue;
            }

            _turnExecutor.ExecuteUnopposed(action);
            yield return new WaitForSeconds(0.5f);
        }

        yield return new WaitForSeconds(0.3f);

        // 턴 종료
        foreach (var unit in allyUnits) { unit.ClearShield(); unit.TickParalysis(); int burn = unit.TickBurn(); if (burn > 0) Log($"  [화상] {unit.UnitName} {burn} 피해"); }
        foreach (var unit in enemyUnits) { unit.ClearShield(); unit.TickParalysis(); int burn = unit.TickBurn(); if (burn > 0) Log($"  [화상] {unit.UnitName} {burn} 피해"); }

        CheckBattleEnd();
        ResetTurnState();
        if (_fsm.Is(BattleState.SkillSelect)) DrawNewHands();
    }

    // ═══════════════════════════════════════
    //  내부 헬퍼
    // ═══════════════════════════════════════

    private void ResetTurnState() => _sel.Reset();

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
