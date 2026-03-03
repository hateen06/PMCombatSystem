using UnityEngine;

[CreateAssetMenu(fileName = "NewUnit", menuName = "PMCombat/UnitData")]
public class UnitData : ScriptableObject
{
    public string unitName;
    public int maxHP;
    public int minSpeed;
    public int maxSpeed;
    public SkillData[] skillSlot;
}