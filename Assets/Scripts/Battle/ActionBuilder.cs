using System.Collections.Generic;
using UnityEngine;
public class ActionBuilder
{
    public List<TurnAction> Build(
        List<Unit> allyUnits, List<Unit> enemyUnits,
        SkillData ally1Skill, int ally1CardIndex,
        Dictionary<int, SkillData> unitSelectedSkills,
        Dictionary<int, int> unitSelectedIndices,
        Dictionary<int, Unit> unitTargets,
        System.Func<Unit> getRandomAliveEnemy,
        System.Func<Unit> getRandomAliveAlly,
        System.Action<string> log)
    {
        var actions = new List<TurnAction>();

        // ── 아군1 ──
        var ally1 = allyUnits.Count > 0 ? allyUnits[0] : null;
        if (ally1 != null && ally1.IsAlive && !ally1.IsStaggered && ally1Skill != null)
        {
            // ally1 카드 소모는 BattleManager에서 이미 처리됨
            int spd = ally1.RollSpeed();
            Unit target = unitTargets.ContainsKey(0) ? unitTargets[0] : null;
            if (target == null || !target.IsAlive) target = getRandomAliveEnemy();
            if (target != null)
                actions.Add(new TurnAction(ally1, ally1Skill, target, spd));
        }

        // ── 아군2+ ──
        for (int ui = 1; ui < allyUnits.Count; ui++)
        {
            var unit = allyUnits[ui];
            if (!unit.IsAlive || unit.IsStaggered) continue;

            SkillData unitSkill = null;
            int ci = -1;

            if (unitSelectedSkills.TryGetValue(ui, out unitSkill) && unitSkill != null)
            {
                unitSelectedIndices.TryGetValue(ui, out ci);
            }
            else
            {
                // 미선택 시 자동: 첫 번째 핸드 카드
                if (unit.Deck != null && unit.Deck.CurrentHand.Count > 0)
                {
                    ci = 0;
                    unitSkill = unit.Deck.CurrentHand[0];
                    log?.Invoke($"[자동] {unit.UnitName} → {unitSkill.skillName}");
                }
            }

            if (unitSkill == null) continue;

            if (ci >= 0 && unit.Deck != null)
                unit.Deck.UseCard(ci);

            int spd = unit.RollSpeed();
            Unit target = unitTargets.ContainsKey(ui) ? unitTargets[ui] : getRandomAliveEnemy();
            if (target != null && !target.IsAlive) target = getRandomAliveEnemy();
            if (target != null)
                actions.Add(new TurnAction(unit, unitSkill, target, spd));
        }

        // ── 적 전원 ──
        for (int ei = 0; ei < enemyUnits.Count; ei++)
        {
            var eUnit = enemyUnits[ei];
            if (!eUnit.IsAlive || eUnit.IsStaggered) continue;
            SkillData eSkill = PickEnemySkillFor(eUnit);
            if (eSkill == null) continue;
            int eSpd = eUnit.RollSpeed();
            var eTarget = getRandomAliveAlly();
            if (eTarget != null)
                actions.Add(new TurnAction(eUnit, eSkill, eTarget, eSpd));
        }

        return actions;
    }

    public SkillData PickEnemySkillFor(Unit enemy)
    {
        if (enemy.Deck != null && enemy.Deck.CurrentHand.Count > 0)
        {
            var hand = enemy.Deck.CurrentHand;
            int index = Random.Range(0, hand.Count);
            return enemy.Deck.UseCard(index);
        }
        var slots = enemy.SkillSlots;
        if (slots == null || slots.Length == 0) return null;
        return slots[Random.Range(0, slots.Length)];
    }
}
