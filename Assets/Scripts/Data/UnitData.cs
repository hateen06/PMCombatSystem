using UnityEngine;

[CreateAssetMenu(fileName = "NewUnit", menuName = "PMCombat/UnitData")]
public class UnitData : ScriptableObject
{
    public string unitName;
    public int maxHP;
    public int minSpeed;
    public int maxSpeed;
    public SkillData[] skillSlot;

    [Header("참격 / 관통 / 타격 저항")]
    [Tooltip("0.5 = 내성, 1.0 = 보통, 1.5 = 취약")]
    public float slashResist = 1f;
    public float pierceResist = 1f;
    public float bluntResist = 1f;

    [Header("흐트러짐 (Stagger)")]
    [Tooltip("HP가 이 비율 이하로 떨어지면 흐트러짐 — 3단계 구간")]
    public float staggerThreshold1 = 0.65f;
    public float staggerThreshold2 = 0.35f;
    public float staggerThreshold3 = 0.15f;

    [Header("레벨")]
    public int level = 1;

    [Header("레벨 성장 계수")]
    [Tooltip("HP = baseHP + level × hpPerLevel")]
    public int baseHP = 50;
    public float hpPerLevel = 8f;

    [Tooltip("OffenseLevel = level + offenseMod")]
    public int offenseMod = 0;

    [Tooltip("DefenseLevel = level + defenseMod")]
    public int defenseMod = 0;
    public int LevelHP => Mathf.RoundToInt(baseHP + level * hpPerLevel);
    public int OffenseLevel => level + offenseMod;
    public int DefenseLevel => level + defenseMod;
}