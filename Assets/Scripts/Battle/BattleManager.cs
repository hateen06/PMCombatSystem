using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투의 중심 컨트롤러.
/// TurnResolver로 합/일방공격 분류 + SP/Stagger 반영.
/// </summary>
public class BattleManager : MonoBehaviour
{
    [Header("유닛")]
    [SerializeField] private Unit allyUnit;
    [SerializeField] private Unit enemyUnit;

    [Header("HP바")]
    [SerializeField] private HPBar allyHPBar;
    [SerializeField] private HPBar enemyHPBar;

    [Header("연출")]
    [SerializeField] private CameraShake cameraShake;
    [SerializeField] private HitFlash allyFlash;
    [SerializeField] private HitFlash enemyFlash;

    private BattleStateMachine _fsm;
    private SkillData _selectedSkill;
    private int _turnCount;

    // ── 이벤트 ──
    public System.Action<string> OnLogMessage;
    public System.Action<ClashResult> OnClashResolved;
    public System.Action<BattleState> OnStateChanged;
    public System.Action<Unit, int> OnDamageDealt;

    // ── 프로퍼티 ──
    public Unit Ally => allyUnit;
    public Unit Enemy => enemyUnit;
    public BattleState CurrentState => _fsm.Current;
    public int TurnCount => _turnCount;

    private void Start()
    {
        _fsm = new BattleStateMachine();

        if (allyUnit == null || enemyUnit == null)
        {
            Log("[오류] 유닛 미연결");
            return;
        }

        allyUnit.Initialize();
        enemyUnit.Initialize();

        if (allyHPBar != null) allyHPBar.Bind(allyUnit);
        if (enemyHPBar != null) enemyHPBar.Bind(enemyUnit);

        Log(">> 전투 시작");
        _fsm.TransitionTo(BattleState.SkillSelect);
        OnStateChanged?.Invoke(_fsm.Current);
    }

    public void SelectSkill(int index)
    {
        if (!_fsm.Is(BattleState.SkillSelect)) return;

        var slots = allyUnit.SkillSlots;
        if (slots == null || index < 0 || index >= slots.Length) return;

        _selectedSkill = slots[index];
        if (_selectedSkill != null)
            Log($"선택: {_selectedSkill.skillName}");
    }

    public void ExecuteTurn()
    {
        if (!_fsm.Is(BattleState.SkillSelect)) return;
        if (_selectedSkill == null)
        {
            Log("[!] 스킬을 먼저 선택하세요");
            return;
        }

        _fsm.TransitionTo(BattleState.ClashResolve);
        OnStateChanged?.Invoke(_fsm.Current);

        _turnCount++;

        // ── 턴 시작: Stagger 회복 체크 ──
        allyUnit.OnTurnStart();
        enemyUnit.OnTurnStart();

        Log($"===== {_turnCount}턴 =====");

        // ── Stagger 체크 ──
        if (allyUnit.IsStaggered)
            Log($"{allyUnit.UnitName} 흐트러짐! 행동 불가 (피해 +50%)");
        if (enemyUnit.IsStaggered)
            Log($"{enemyUnit.UnitName} 흐트러짐! 행동 불가 (피해 +50%)");

        // ── TurnAction 생성 ──
        SkillData enemySkill = PickEnemySkill();
        int allySpeed = allyUnit.RollSpeed();
        int enemySpeed = enemyUnit.RollSpeed();

        Log($"속도: {allyUnit.UnitName} {allySpeed} / {enemyUnit.UnitName} {enemySpeed}");

        // 흐트러진 유닛은 행동 불가
        var actions = new List<TurnAction>();

        if (!allyUnit.IsStaggered)
            actions.Add(new TurnAction(allyUnit, _selectedSkill, enemyUnit, allySpeed));
        if (!enemyUnit.IsStaggered && enemySkill != null)
            actions.Add(new TurnAction(enemyUnit, enemySkill, allyUnit, enemySpeed));

        // ── TurnResolver로 합/일방공격 분류 ──
        var plan = TurnResolver.Plan(actions);

        // ── 결과 적용 ──
        _fsm.TransitionTo(BattleState.ApplyResult);
        OnStateChanged?.Invoke(_fsm.Current);

        // 합 처리
        foreach (var (attacker, defender) in plan.clashes)
        {
            Log($"[합] {attacker.skill.skillName} vs {defender.skill.skillName}");

            var clash = ClashResolver.Resolve(
                attacker.actor, attacker.skill,
                defender.actor, defender.skill,
                attacker.speed, defender.speed);

            OnClashResolved?.Invoke(clash);
            ApplyClashDamage(clash);
            LogClashResult(clash);
        }

        // 일방공격 처리
        foreach (var action in plan.unopposed)
        {
            if (!action.actor.IsAlive || !action.target.IsAlive) continue;

            int damage = CoinCalculator.RollPower(action.skill, action.skill.coinCount, action.actor.CoinHeadsChance);
            Log($"[일방] {action.actor.UnitName}의 {action.skill.skillName} -> {damage} 피해");

            action.target.TakeDamage(damage);
            ApplyHitEffects(action.target, damage);

            if (action.skill.statusPotency > 0 && action.skill.statusCount > 0)
            {
                action.target.AddStatus(action.skill.inflictStatus,
                    action.skill.statusPotency, action.skill.statusCount);
                Log($"  {action.skill.inflictStatus} +{action.skill.statusPotency}/{action.skill.statusCount}");
            }
        }

        CheckBattleEnd();
        _selectedSkill = null;
    }

    // ── 내부 함수 ──

    private void ApplyClashDamage(ClashResult clash)
    {
        switch (clash.outcome)
        {
            case ClashOutcome.AttackerWin:
                enemyUnit.TakeDamage(clash.damage);
                ApplyHitEffects(enemyUnit, clash.damage);
                break;
            case ClashOutcome.DefenderWin:
                allyUnit.TakeDamage(clash.damage);
                ApplyHitEffects(allyUnit, clash.damage);
                break;
        }
    }

    private void ApplyHitEffects(Unit target, int damage)
    {
        if (damage <= 0) return;

        bool isAlly = target == allyUnit;
        if (isAlly)
        {
            if (allyHPBar != null) allyHPBar.OnHit();
            if (allyFlash != null) allyFlash.Flash();
        }
        else
        {
            if (enemyHPBar != null) enemyHPBar.OnHit();
            if (enemyFlash != null) enemyFlash.Flash();
        }

        if (cameraShake != null) cameraShake.Shake();
        OnDamageDealt?.Invoke(target, damage);

        // Stagger 알림
        if (target.IsStaggered)
            Log($"  >> {target.UnitName} 흐트러짐 발생!");
    }

    private void LogClashResult(ClashResult clash)
    {
        foreach (var line in clash.log.Split('|'))
        {
            var trimmed = line.Trim();
            if (!string.IsNullOrEmpty(trimmed))
                Log(trimmed);
        }
    }

    private void CheckBattleEnd()
    {
        if (!allyUnit.IsAlive || !enemyUnit.IsAlive)
        {
            _fsm.TransitionTo(BattleState.BattleEnd);
            OnStateChanged?.Invoke(_fsm.Current);

            if (!allyUnit.IsAlive && !enemyUnit.IsAlive)
                Log("[무승부]");
            else if (!enemyUnit.IsAlive)
                Log("[승리!]");
            else
                Log("[패배...]");
        }
        else
        {
            _fsm.TransitionTo(BattleState.SkillSelect);
            OnStateChanged?.Invoke(_fsm.Current);
        }
    }

    private SkillData PickEnemySkill()
    {
        var slots = enemyUnit.SkillSlots;
        if (slots == null || slots.Length == 0) return null;
        return slots[Random.Range(0, slots.Length)];
    }

    private void Log(string message)
    {
        Debug.Log($"[Battle] {message}");
        OnLogMessage?.Invoke(message);
    }
}
