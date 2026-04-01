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
    public int loserSPChange;
    public bool winnerIsAttacker;
    public int remainingCoins;
    public List<int> followUpHitPowers = new();
    public List<StatusApplication> statusApplications = new();
    public List<(bool isAttacker, int bleedDamage)> bleedResults = new();
}
