using System.Collections.Generic;

public enum ClashOutcome
{
    AttackerWin,
    DefenderWin,
    Draw
}

public struct StatusApplication
{
    public bool applyToAttacker;
    public StatusType type;
    public int potency;
    public int count;
}

public class ClashResult
{
    public ClashOutcome outcome;
    public int damage;
    public string log;
    public SkillData attackerSkill;
    public SkillData defenderSkill;

    public int winnerSPChange;
    public bool winnerIsAttacker;
    public List<StatusApplication> statusApplications = new();
    public List<(bool isAttacker, int bleedDamage)> bleedResults = new();
}
