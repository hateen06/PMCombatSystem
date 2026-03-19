public enum ClashOutcome
{
    AttackerWin,
    DefenderWin,
    Draw
}

public class ClashResult
{
    public ClashOutcome outcome;
    public int damage;       // 승자가 패자에게 입히는 피해
    public string log;       // 전투 로그 텍스트

    // 어떤 스킬의 피해 타입인지 추적
    public SkillData attackerSkill;
    public SkillData defenderSkill;
}