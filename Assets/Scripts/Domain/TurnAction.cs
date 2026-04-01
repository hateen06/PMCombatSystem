public class TurnAction
{
    public Unit actor;
    public SkillData skill;
    public Unit target;
    public int speed;
    public bool isAllyAction;

    public TurnAction(Unit actor, SkillData skill, Unit target, int speed, bool isAllyAction = false)
    {
        this.actor = actor;
        this.skill = skill;
        this.target = target;
        this.speed = speed;
        this.isAllyAction = isAllyAction;
    }
}
