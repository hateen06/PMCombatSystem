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
    public System.Action<int, int, SkillData> OnCardOverridden;
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
        SelectSkillForUnit(0, index);
    }

    public void SelectSkillForUnit(int unitIndex, int cardIndex)
    {
        if (!_fsm.Is(BattleState.SkillSelect)) return;
        if (unitIndex < 0 || unitIndex >= allyUnits.Count) return;
        var unit = allyUnits[unitIndex];
        if (unit.Deck == null) return;
        var hand = unit.Deck.CurrentHand;
        if (cardIndex < 0 || cardIndex >= hand.Count) return;

                var selection = _sel.Get(unitIndex);
        selection.skill = hand[cardIndex];
        selection.cardIndex = cardIndex;
        selection.originalSkill = hand[cardIndex];

        if (selection.isGuarding || selection.isEvading)
        {
            selection.isGuarding = false;
            selection.isEvading = false;
            selection.evadeSkill = null;
            selection.guardCardIndex = -1;
            OnCardOverridden?.Invoke(unitIndex, cardIndex, hand[cardIndex]);
        }

        if (selection.target == null || !selection.target.IsAlive)
            selection.target = GetRandomAliveEnemy();

        _presenter.UpdateClashPreview(selection.skill, unit, selection.target);
        OnTargetPreviewUpdated?.Invoke(unit, selection.target);
        Log($"[유닛{unitIndex}] 선택: {hand[cardIndex].skillName}");
    }

    // ═══════════════════════════════════════
    //  타겟팅
    // ═══════════════════════════════════════

    public void SetTarget(Unit target)
    {
        if (!_fsm.Is(BattleState.SkillSelect)) return;
        if (target == null || !target.IsAlive) return;
        if (!enemyUnits.Contains(target)) return;

        int activeAlly = -1;
        for (int i = allyUnits.Count - 1; i >= 0; i--)
        {
            var selection = _sel.Get(i);
            if (selection.skill != null && !selection.isGuarding && !selection.isEvading)
            {
                activeAlly = i;
                break;
            }
        }
        if (activeAlly < 0) activeAlly = 0;
        if (activeAlly >= allyUnits.Count) return;

        var source = allyUnits[activeAlly];
        if (source == null || !source.IsAlive) return;

        var current = _sel.Get(activeAlly);
        current.target = target;
        Log($"[타겟] {source.UnitName} → {target.UnitName}");
        OnTargetAssigned?.Invoke(source, target);

        if (current.skill != null)
            _presenter.UpdateClashPreview(current.skill, source, target);

        OnTargetLineUpdated?.Invoke(source, target, false);
    }

    // ═══════════════════════════════════════
    //  방어 / 회피 토글
    // ═══════════════════════════════════════

    public void SelectDefenseForUnit(int unitIndex, int cardIndex)
    {
        if (!_fsm.Is(BattleState.SkillSelect)) return;
        if (unitIndex < 0 || unitIndex >= allyUnits.Count) return;
        var unit = allyUnits[unitIndex];

        var selection = _sel.Get(unitIndex);
        if (selection.isGuarding || selection.isEvading)
        {
            int restoreCardIndex = selection.guardCardIndex >= 0 ? selection.guardCardIndex : selection.cardIndex;
            if (restoreCardIndex < 0) restoreCardIndex = cardIndex;

            selection.isGuarding = false;
            selection.isEvading = false;
            selection.evadeSkill = null;
            selection.guardCardIndex = -1;

            SkillData orig = selection.originalSkill;
            if (orig == null && unit.Deck != null && restoreCardIndex >= 0 && restoreCardIndex < unit.Deck.CurrentHand.Count)
                orig = unit.Deck.CurrentHand[restoreCardIndex];

            selection.skill = orig;
            selection.cardIndex = restoreCardIndex;

            if (orig != null && restoreCardIndex >= 0)
                OnCardOverridden?.Invoke(unitIndex, restoreCardIndex, orig);
            Log($"[해제] {unit.UnitName} → 공격 복귀");
            return;
        }

        var slots = unit.SkillSlots;
        if (slots == null) return;
        SkillData defSkill = null;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            if (slots[i].skillType == SkillType.Defense || slots[i].skillType == SkillType.Evade)
            {
                defSkill = slots[i];
                break;
            }
        }
        if (defSkill == null) { Log("[!] 방어/회피 스킬 없음"); return; }

        var hand = unit.Deck?.CurrentHand;
        if (hand != null && cardIndex >= 0 && cardIndex < hand.Count)
            selection.originalSkill = hand[cardIndex];

        selection.skill = defSkill;
        selection.cardIndex = cardIndex;
        selection.guardCardIndex = cardIndex;
        selection.isGuarding = defSkill.skillType == SkillType.Defense;
        selection.isEvading = defSkill.skillType == SkillType.Evade;
        selection.evadeSkill = defSkill.skillType == SkillType.Evade ? defSkill : null;

        OnCardOverridden?.Invoke(unitIndex, cardIndex, defSkill);
        Log($"[{(defSkill.skillType == SkillType.Defense ? "방어" : "회피")}] {unit.UnitName} → {defSkill.skillName}");
    }

    // 레거시 호환
    public void SelectEvade(int cardIndex)
    {
        if (!_fsm.Is(BattleState.SkillSelect)) return;
        if (Ally == null) return;
        if (cardIndex < 0) return;

        var hand = Ally.Deck?.CurrentHand;
        if (hand == null || cardIndex >= hand.Count) return;

        var selection0 = _sel.Get(0);
        selection0.originalSkill = hand[cardIndex];
        selection0.skill = hand[cardIndex];
        selection0.cardIndex = cardIndex;
        selection0.guardCardIndex = cardIndex;
        selection0.isGuarding = false;
        selection0.isEvading = false;
        selection0.evadeSkill = null;
        OnEvadeButton();
    }

    public void OnEvadeButton()
    {
        var selection0 = _sel.Get(0);
        if (selection0.cardIndex < 0 && !selection0.isGuarding) return;
        int evadeIndex = selection0.isGuarding ? selection0.guardCardIndex : selection0.cardIndex;
        if (evadeIndex < 0) return;

        if (selection0.isGuarding)
            SelectDefenseForUnit(0, evadeIndex);

        var slots = Ally.SkillSlots;
        if (slots == null) return;

        SkillData evadeSkill = null;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            if (slots[i].skillType == SkillType.Evade)
            {
                evadeSkill = slots[i];
                break;
            }
        }

        if (evadeSkill == null)
        {
            Log("[!] 회피 스킬 없음");
            return;
        }

        selection0.skill = evadeSkill;
        selection0.evadeSkill = evadeSkill;
        selection0.cardIndex = evadeIndex;
        selection0.guardCardIndex = evadeIndex;
        selection0.isGuarding = false;
        selection0.isEvading = true;
        OnCardOverridden?.Invoke(0, evadeIndex, evadeSkill);
        Log($"[회피] {Ally.UnitName} → {evadeSkill.skillName}");
        ExecuteTurn();
    }

    public void OnGuardButton()
    {
        if (!_fsm.Is(BattleState.SkillSelect)) return;
        var selection0 = _sel.Get(0);
        if (selection0.cardIndex < 0 && !selection0.isGuarding) { Log("[!] 카드를 먼저 선택하세요"); return; }

        if (selection0.isGuarding) { CancelGuard(); return; }

        var guardSkill = FindGuardSkill();
        if (guardSkill == null) { Log("[!] Guard 스킬 없음"); return; }

        selection0.isGuarding = true;
        selection0.guardCardIndex = selection0.cardIndex;
        OnCardOverridden?.Invoke(0, selection0.cardIndex, guardSkill);
        selection0.skill = guardSkill;
        selection0.isEvading = false;
        Log($"[방어 준비] {Ally.UnitName} → {guardSkill.skillName}");
    }

    private void CancelGuard()
    {
        var selection0 = _sel.Get(0);
        if (!selection0.isGuarding) return;
        if (Ally.Deck != null && selection0.guardCardIndex >= 0)
        {
            var hand = Ally.Deck.CurrentHand;
            if (selection0.guardCardIndex < hand.Count)
            {
                OnCardOverridden?.Invoke(0, selection0.guardCardIndex, hand[selection0.guardCardIndex]);
                selection0.skill = hand[selection0.guardCardIndex];
                selection0.cardIndex = selection0.guardCardIndex;
            }
        }
        selection0.isGuarding = false;
        selection0.guardCardIndex = -1;
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

        var selection = _sel.Get(unitIndex);
        selection.skill = ego;
        selection.cardIndex = -1;
        selection.isGuarding = false;
        selection.isEvading = false;
        Log($"[E.G.O] {unit.UnitName} → {ego.skillName} (SP -{ego.egoCost})");
        ExecuteTurn();
    }

    public void ExecuteTurn()
    {
        if (!_fsm.Is(BattleState.SkillSelect)) return;

        bool hasAnySelection = false;
        foreach (var pair in _sel.units)
        {
            if (pair.Value == null) continue;
            if (pair.Value.skill != null || pair.Value.isEvading || pair.Value.isGuarding)
            {
                hasAnySelection = true;
                break;
            }
        }

        if (!hasAnySelection)
        {
            Log("[!] 스킬을 먼저 선택하세요");
            return;
        }

        StartCoroutine(ExecuteTurnCoroutine());
    }

    private IEnumerator ExecuteTurnCoroutine()
    {
        ApplyPreparedDefense();

        _fsm.TransitionTo(BattleState.ClashResolve);
        OnStateChanged?.Invoke(_fsm.Current);
        _turnCount++;

        foreach (var unit in allyUnits) unit.OnTurnStart();
        foreach (var unit in enemyUnits) unit.OnTurnStart();

        Log($"===== {_turnCount}턴 =====");
        foreach (var unit in allyUnits)
            if (unit.IsStaggered) Log($"{unit.UnitName} 흐트러짐! 행동 불가");
        foreach (var unit in enemyUnits)
            if (unit.IsStaggered) Log($"{unit.UnitName} 흐트러짐! 행동 불가");

        var actions = _actionBuilder.Build(
            allyUnits, enemyUnits,
            BuildSelectedSkills(), BuildSelectedIndices(), BuildSelectedTargets(),
            GetRandomAliveEnemy, GetRandomAliveAlly, Log);

        foreach (var action in actions)
        {
            OnSpeedRolled?.Invoke(action.actor, action.speed);
            if (!action.isAllyAction)
                OnEnemyIntentRevealed?.Invoke(action.actor, action.skill);
        }

        var plan = TurnResolver.Plan(actions);

        foreach (var pair in plan.clashes)
        {
            OnTargetLineUpdated?.Invoke(pair.attacker.actor, pair.defender.actor, true);
            OnTargetLineUpdated?.Invoke(pair.defender.actor, pair.attacker.actor, true);
            OnClashPairHighlighted?.Invoke(pair.attacker.actor, pair.defender.actor);
        }
        foreach (var action in plan.unopposed)
            OnTargetLineUpdated?.Invoke(action.actor, action.target, false);

        _fsm.TransitionTo(BattleState.ApplyResult);
        OnStateChanged?.Invoke(_fsm.Current);

        foreach (var pair in plan.clashes)
        {
            Log($"[합] {pair.attacker.actor.UnitName}:{pair.attacker.skill.skillName} vs {pair.defender.actor.UnitName}:{pair.defender.skill.skillName}");
            var clash = ClashResolver.Resolve(
                pair.attacker.actor, pair.attacker.skill,
                pair.defender.actor, pair.defender.skill,
                pair.attacker.speed, pair.defender.speed);
            OnClashResolved?.Invoke(clash);
            _turnExecutor.ApplyClashSideEffects(clash, pair.attacker.actor, pair.defender.actor);
            _turnExecutor.ExecuteClashDamage(clash, pair.attacker.actor, pair.defender.actor);
            ApplyBattleMorale(pair.attacker.actor, pair.defender.actor, clash);
            LogClashResult(clash);
            yield return new WaitForSeconds(0.5f);
        }

        yield return new WaitForSeconds(0.2f);

        foreach (var action in plan.unopposed)
        {
            if (!action.actor.IsAlive || !action.target.IsAlive) continue;

            if (allyUnits.Contains(action.target))
            {
                int targetIndex = allyUnits.IndexOf(action.target);
                var targetSelection = _sel.Get(targetIndex);
                if (targetSelection.isEvading && targetSelection.evadeSkill != null)
                {
                    _turnExecutor.ExecuteEvade(action, targetSelection.evadeSkill, action.target);
                    yield return new WaitForSeconds(0.4f);
                    continue;
                }
            }

            _turnExecutor.ExecuteUnopposed(action);
            ApplyKillMorale(action.actor, action.target);
            yield return new WaitForSeconds(0.5f);
        }

        yield return new WaitForSeconds(0.3f);

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


    private Dictionary<int, SkillData> BuildSelectedSkills()
    {
        var map = new Dictionary<int, SkillData>();
        foreach (var pair in _sel.units)
            if (pair.Value != null && pair.Value.skill != null)
                map[pair.Key] = pair.Value.skill;
        return map;
    }

    private Dictionary<int, int> BuildSelectedIndices()
    {
        var map = new Dictionary<int, int>();
        foreach (var pair in _sel.units)
            if (pair.Value != null && pair.Value.cardIndex >= 0)
                map[pair.Key] = pair.Value.cardIndex;
        return map;
    }

    private Dictionary<int, Unit> BuildSelectedTargets()
    {
        var map = new Dictionary<int, Unit>();
        foreach (var pair in _sel.units)
            if (pair.Value != null && pair.Value.target != null)
                map[pair.Key] = pair.Value.target;
        return map;
    }

    private void ApplyPreparedDefense()
    {
        foreach (var pair in _sel.units)
        {
            var unitIndex = pair.Key;
            var selection = pair.Value;
            if (selection == null || !selection.isGuarding || selection.skill == null) continue;
            if (unitIndex < 0 || unitIndex >= allyUnits.Count) continue;

            var unit = allyUnits[unitIndex];
            if (unit == null || !unit.IsAlive) continue;

            int shieldPower = CoinCalculator.RollPower(selection.skill, selection.skill.coinCount, unit.CoinHeadsChance);
            unit.ApplyShield(shieldPower);
            Log($"[방어] {unit.UnitName} 방어 발동! 방어막 {shieldPower}");
        }
    }

    private void ApplyBattleMorale(Unit attacker, Unit defender, ClashResult clash)
    {
        if (clash.outcome == ClashOutcome.Draw) return;

        var winner = clash.winnerIsAttacker ? attacker : defender;
        var loser = clash.winnerIsAttacker ? defender : attacker;

        if (winner != null) winner.OnClashWin();
        if (loser != null) loser.ChangeSP(-10);

        var defeated = clash.winnerIsAttacker ? defender : attacker;
        ApplyKillMorale(winner, defeated);
    }

    private void ApplyKillMorale(Unit killer, Unit defeated)
    {
        if (killer == null || defeated == null || defeated.IsAlive) return;

        killer.ChangeSP(10);

        var defeatedTeam = allyUnits.Contains(defeated) ? allyUnits : enemyUnits;
        foreach (var unit in defeatedTeam)
        {
            if (unit == null || !unit.IsAlive || unit == defeated) continue;
            unit.OnAllyDeath();
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
