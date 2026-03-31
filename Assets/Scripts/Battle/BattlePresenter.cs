using UnityEngine;
using System.Collections.Generic;
public class BattlePresenter
{
    public System.Action<string> OnBreakdownUpdated;
    public System.Action<string> OnClashPreviewUpdated;
    public System.Action<string> OnIntentUpdated;
    public void UpdateClashPreview(SkillData allySkill, Unit ally, Unit enemy)
    {
        if (allySkill == null || enemy == null)
        {
            OnClashPreviewUpdated?.Invoke(string.Empty);
            OnIntentUpdated?.Invoke(string.Empty);
            return;
        }

        SkillData enemySkill = PeekEnemySkill(enemy);
        if (enemySkill == null)
        {
            OnClashPreviewUpdated?.Invoke("[Preview]\n적 스킬 정보 없음");
            OnIntentUpdated?.Invoke("[Intent]\n적 의도 정보 없음");
            return;
        }

        int allyEst = allySkill.basePower + Mathf.RoundToInt(allySkill.coinCount * allySkill.coinPower * 0.5f);
        int enemyEst = enemySkill.basePower + Mathf.RoundToInt(enemySkill.coinCount * enemySkill.coinPower * 0.5f);

        string result;
        if (allyEst >= enemyEst + 3) result = "유리";
        else if (enemyEst >= allyEst + 3) result = "불리";
        else result = "보통";

        string resultColored = result == "유리"
            ? "<color=#66FF99>유리</color>"
            : result == "불리"
                ? "<color=#FF6666>불리</color>"
                : "<color=#FFD966>보통</color>";

        string preview =
            $"[Preview]\n" +
            $"아군  {allySkill.skillName} {BattleUtils.GetDamageTypeSymbol(allySkill)}  ~{allyEst}\n" +
            $"적군  {enemySkill.skillName} {BattleUtils.GetDamageTypeSymbol(enemySkill)}  ~{enemyEst}\n" +
            $"판정  {resultColored}";

        string clashColored = result == "유리"
            ? "<color=#66FF99>우세</color>"
            : result == "불리"
                ? "<color=#FF6666>열세</color>"
                : "<color=#FFD966>경합</color>";

        string intent =
            $"[Intent]\n" +
            $"{ally.UnitName} → {enemy.UnitName}  {allySkill.skillName} {BattleUtils.GetDamageTypeSymbol(allySkill)}\n" +
            $"{enemy.UnitName} → {ally.UnitName}  {enemySkill.skillName} {BattleUtils.GetDamageTypeSymbol(enemySkill)}\n" +
            $"충돌: {clashColored}";

        OnClashPreviewUpdated?.Invoke(preview);
        OnIntentUpdated?.Invoke(intent);
    }
    public void UpdateBreakdown(Unit target, int finalDamage, DamageType damageType)
    {
        if (finalDamage <= 0) return;

        float resist = target.GetResistance(damageType);
        string damageTypeLabel = BattleUtils.GetDamageTypeLabel(damageType);
        string resistColored = resist < 1f
            ? $"<color=#66FF99>내성</color> x{resist:0.0}"
            : resist > 1f
                ? $"<color=#FF6666>취약</color> x{resist:0.0}"
                : $"보통 x{resist:0.0}";

        string staggerText = target.IsStaggered
            ? "  <color=#FF6666>흐트러짐!</color>"
            : "";

        string breakdown =
            $"[Breakdown]\n" +
            $"{target.UnitName}  {BattleUtils.GetDamageTypeSymbol(damageType)} {resistColored}  → {finalDamage}{staggerText}";

        OnBreakdownUpdated?.Invoke(breakdown);
    }

    private SkillData PeekEnemySkill(Unit enemy)
    {
        if (enemy.Deck != null && enemy.Deck.CurrentHand.Count > 0)
            return enemy.Deck.CurrentHand[0];
        if (enemy.SkillSlots != null && enemy.SkillSlots.Length > 0)
            return enemy.SkillSlots[0];
        return null;
    }
}
