using UnityEngine;

public enum SkillType
{
    Attack,
    Defense,
    Evade,
    EGO
}

public enum DamageType
{
    Slash,
    Pierce,
    Blunt
}

[CreateAssetMenu(fileName = "NewSkill", menuName = "PMCombat/SkillData")]
public class SkillData : ScriptableObject
{
    public string skillName;
    public SkillType skillType;
    public DamageType damageType = DamageType.Slash;
    public Sprite cardArtwork;
    public int basePower;
    public int coinCount;
    public int coinPower;

    [Header("상태이상 부여")]
    public StatusType inflictStatus;
    public int statusPotency;
    public int statusCount;

    [Header("E.G.O")]
    public int egoCost;
}