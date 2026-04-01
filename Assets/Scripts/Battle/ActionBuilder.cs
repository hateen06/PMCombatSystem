using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ActionBuilder
{
    public List<TurnAction> Build(
        List<Unit> allyUnits, List<Unit> enemyUnits,
        Dictionary<int, SkillData> unitSelectedSkills,
        Dictionary<int, int> unitSelectedIndices,
        Dictionary<int, Unit> unitTargets,
        System.Func<Unit> getRandomAliveEnemy,
        System.Func<Unit> getRandomAliveAlly,
        System.Action<string> log)
    {
        var actions = new List<TurnAction>();
        var allySpeeds = RollSpeeds(allyUnits);
        var enemySpeeds = RollSpeeds(enemyUnits);

        for (int ui = 0; ui < allyUnits.Count; ui++)
        {
            var unit = allyUnits[ui];
            if (unit == null || !unit.IsAlive || unit.IsStaggered) continue;

            SkillData unitSkill = null;
            int cardIndex = -1;

            if (unitSelectedSkills.TryGetValue(ui, out unitSkill) && unitSkill != null)
            {
                unitSelectedIndices.TryGetValue(ui, out cardIndex);
            }
            else if (unit.Deck != null && unit.Deck.CurrentHand.Count > 0)
            {
                cardIndex = 0;
                unitSkill = unit.Deck.CurrentHand[0];
                log?.Invoke($"[자동] {unit.UnitName} → {unitSkill.skillName}");
            }

            if (unitSkill == null) continue;

            if (cardIndex >= 0 && unit.Deck != null)
                unit.Deck.UseCard(cardIndex);

            int speed = allySpeeds[unit];
            Unit target = unitTargets.ContainsKey(ui) ? unitTargets[ui] : getRandomAliveEnemy();
            if (target != null && !target.IsAlive) target = getRandomAliveEnemy();
            if (target != null)
                actions.Add(new TurnAction(unit, unitSkill, target, speed, true));
        }

        for (int ei = 0; ei < enemyUnits.Count; ei++)
        {
            var enemy = enemyUnits[ei];
            if (enemy == null || !enemy.IsAlive || enemy.IsStaggered) continue;

            var skill = PickEnemySkillFor(enemy);
            if (skill == null) continue;

            int speed = enemySpeeds[enemy];
            var target = PickEnemyTarget(allyUnits, allySpeeds) ?? getRandomAliveAlly();
            if (target != null)
                actions.Add(new TurnAction(enemy, skill, target, speed, false));
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

    private Dictionary<Unit, int> RollSpeeds(List<Unit> units)
    {
        var map = new Dictionary<Unit, int>();
        for (int i = 0; i < units.Count; i++)
        {
            var unit = units[i];
            if (unit == null || !unit.IsAlive) continue;
            map[unit] = unit.RollSpeed();
        }
        return map;
    }

    private Unit PickEnemyTarget(List<Unit> allyUnits, Dictionary<Unit, int> allySpeeds)
    {
        return allyUnits
            .Where(u => u != null && u.IsAlive)
            .OrderBy(u => allySpeeds.TryGetValue(u, out var speed) ? speed : int.MaxValue)
            .FirstOrDefault();
    }
}
