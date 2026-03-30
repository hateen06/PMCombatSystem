/// <summary>
/// 전투 관련 유틸리티 (정적 헬퍼).
/// 타입 라벨, 심볼, 저항 표시 등.
/// </summary>
public static class BattleUtils
{
    public static string GetDamageTypeLabel(SkillData skill)
    {
        if (skill == null) return "없음";
        return GetDamageTypeLabel(skill.damageType);
    }

    public static string GetDamageTypeLabel(DamageType damageType)
    {
        switch (damageType)
        {
            case DamageType.Slash: return "참격";
            case DamageType.Pierce: return "관통";
            case DamageType.Blunt: return "타격";
            default: return "없음";
        }
    }

    public static string GetDamageTypeSymbol(SkillData skill)
    {
        if (skill == null) return "?";
        return GetDamageTypeSymbol(skill.damageType);
    }

    public static string GetDamageTypeSymbol(DamageType damageType)
    {
        switch (damageType)
        {
            case DamageType.Slash: return "<color=#FF6666>참</color>";
            case DamageType.Pierce: return "<color=#66BBFF>관</color>";
            case DamageType.Blunt: return "<color=#FFD966>타</color>";
            default: return "?";
        }
    }

    public static string GetResistanceLabel(float resist)
    {
        if (resist < 1f) return "내성";
        if (resist > 1f) return "취약";
        return "보통";
    }
}
