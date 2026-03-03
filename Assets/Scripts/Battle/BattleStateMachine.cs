using UnityEngine;

public class BattleStateMachine
{
    private BattleState _current;

    public BattleState Current => _current;

    public BattleStateMachine()
    {
        _current = BattleState.Idle;
    }

    public void TransitionTo(BattleState next)
    {
        if (!CanTransition(_current, next))
        {
            Debug.LogWarning($"[FSM] 잘못된 전환: {_current} → {next}");
            return;
        }

        Debug.Log($"[FSM] {_current} → {next}");
        _current = next;
    }

    public bool Is(BattleState state) => _current == state;

    private bool CanTransition(BattleState from, BattleState to)
    {
        return (from, to) switch
        {
            (BattleState.Idle,         BattleState.SkillSelect)  => true,
            (BattleState.SkillSelect,  BattleState.ClashResolve) => true,
            (BattleState.ClashResolve, BattleState.ApplyResult)  => true,
            (BattleState.ApplyResult,  BattleState.SkillSelect)  => true, // 다음 턴
            (BattleState.ApplyResult,  BattleState.BattleEnd)    => true, // 전투 종료
            _ => false
        };
    }
}
