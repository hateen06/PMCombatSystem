using UnityEngine;

public enum SkillType
{
    Attack,
    Defense,
    Evade
}

[CreateAssetMenu(fileName = "NewSkill", menuName = "PMCombat/SkillData")]
public class SkillData : ScriptableObject
{
    public string skillName;
    public SkillType skillType;
    public int basePower;
    public int coinCount;
    public int coinPower;

    [Header("상태이상 부여")]
    public StatusType inflictStatus;
    public int statusPotency;   // 위력
    public int statusCount;     // 횟수
}