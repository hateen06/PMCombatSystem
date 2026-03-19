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
    [Tooltip("HP가 이 비율 이하로 떨어지면 흐트러짐 (0.0~1.0)")]
    [Range(0.1f, 0.9f)]
    public float staggerThreshold = 0.5f;
}