
public class TurnAction
{
    public Unit actor;       // 행동 유닛
    public SkillData skill;  // 선택한 스킬
    public Unit target;      // 공격 대상
    public int speed;        // 이번 턴 속도

    public TurnAction(Unit actor, SkillData skill, Unit target, int speed)
    {
        this.actor = actor;
        this.skill = skill;
        this.target = target;
        this.speed = speed;
    }
}
