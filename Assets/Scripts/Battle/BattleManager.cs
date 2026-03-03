using UnityEngine;

/// <summary>
/// 전투의 중심 컨트롤러.
/// 역할: 상태머신 구동 + Unit/UI 연결만 담당.
/// 전투 규칙(ClashResolver)이나 UI 표현은 여기서 직접 하지 않는다.
/// </summary>
public class BattleManager : MonoBehaviour
{
    [Header("유닛")]
    [SerializeField] private Unit allyUnit;
    [SerializeField] private Unit enemyUnit;

    [Header("HP바")]
    [SerializeField] private HPBar allyHPBar;
    [SerializeField] private HPBar enemyHPBar;

    private BattleStateMachine _fsm;
    private SkillData _selectedSkill;
    private int _turnCount;

    // ── 이벤트 (UI가 구독) ──
    public System.Action<string> OnLogMessage;
    public System.Action<ClashResult> OnClashResolved;
    public System.Action<BattleState> OnStateChanged;
    public System.Action<Unit, int> OnDamageDealt;  // 누가, 얼마나

    // ── 프로퍼티 (UI에서 읽기용) ──
    public Unit Ally => allyUnit;
    public Unit Enemy => enemyUnit;
    public BattleState CurrentState => _fsm.Current;
    public int TurnCount => _turnCount;

    private void Start()
    {
        _fsm = new BattleStateMachine();

        if (allyUnit == null || enemyUnit == null)
        {
            Log("※ 유닛이 연결되지 않았습니다");
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

    // ── UI 버튼에서 호출 ──
    public void SelectSkill(int index)
    {
        if (!_fsm.Is(BattleState.SkillSelect)) return;

        var slots = allyUnit.SkillSlots;
        if (slots == null || index < 0 || index >= slots.Length) return;

        _selectedSkill = slots[index];
        if (_selectedSkill != null)
            Log($"▷ 선택: {_selectedSkill.skillName}");
    }

    // ── UI 실행 버튼에서 호출 ──
    public void ExecuteTurn()
    {
        if (!_fsm.Is(BattleState.SkillSelect)) return;
        if (_selectedSkill == null)
        {
            Log("※ 스킬을 먼저 선택하세요");
            return;
        }

        // 1) 클래시 해결
        _fsm.TransitionTo(BattleState.ClashResolve);
        OnStateChanged?.Invoke(_fsm.Current);

        SkillData enemySkill = PickEnemySkill();
        int allySpeed = allyUnit.RollSpeed();
        int enemySpeed = enemyUnit.RollSpeed();

        _turnCount++;
        Log($"[ {_turnCount}턴 ] 속도 {allySpeed}:{enemySpeed}");

        ClashResult clash = ClashResolver.Resolve(
            allyUnit, _selectedSkill,
            enemyUnit, enemySkill,
            allySpeed, enemySpeed);

        OnClashResolved?.Invoke(clash);

        // 2) 결과 적용
        _fsm.TransitionTo(BattleState.ApplyResult);
        OnStateChanged?.Invoke(_fsm.Current);

        ApplyClashResult(clash);
        Log(clash.log);

        // 3) 전투 종료 체크
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
            // 다음 턴
            _fsm.TransitionTo(BattleState.SkillSelect);
            OnStateChanged?.Invoke(_fsm.Current);
        }

        _selectedSkill = null;
    }

    // ── 내부 함수 ──

    private void ApplyClashResult(ClashResult clash)
    {
        switch (clash.outcome)
        {
            case ClashOutcome.AttackerWin:
                enemyUnit.TakeDamage(clash.damage);
                if (enemyHPBar != null) enemyHPBar.OnHit();
                OnDamageDealt?.Invoke(enemyUnit, clash.damage);
                break;
            case ClashOutcome.DefenderWin:
                allyUnit.TakeDamage(clash.damage);
                if (allyHPBar != null) allyHPBar.OnHit();
                OnDamageDealt?.Invoke(allyUnit, clash.damage);
                break;
        }
    }

    private SkillData PickEnemySkill()
    {
        // 단순 랜덤 선택 (나중에 EnemyAI로 분리 가능)
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
