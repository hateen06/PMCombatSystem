using System.Collections.Generic;
using System.Linq;

public static class TurnResolver
{
    public class TurnPlan
    {
        public List<(TurnAction attacker, TurnAction defender)> clashes = new List<(TurnAction, TurnAction)>();
        public List<TurnAction> unopposed = new List<TurnAction>();
    }

    public static TurnPlan Plan(List<TurnAction> actions)
    {
        var plan = new TurnPlan();
        var matched = new HashSet<TurnAction>();
        var sorted = actions.OrderByDescending(a => a.speed).ToList();

        for (int i = 0; i < sorted.Count; i++)
        {
            var action = sorted[i];
            if (matched.Contains(action)) continue;
            if (!action.isAllyAction) continue;
            if (action.skill == null || action.skill.skillType != SkillType.Attack) continue;
            if (action.target == null) continue;

            TurnAction clashTarget = null;
            for (int j = 0; j < sorted.Count; j++)
            {
                var enemyAction = sorted[j];
                if (matched.Contains(enemyAction)) continue;
                if (enemyAction.isAllyAction) continue;
                if (enemyAction.actor != action.target) continue;
                if (enemyAction.skill == null || enemyAction.skill.skillType != SkillType.Attack) continue;

                bool selfDefense = enemyAction.target == action.actor;
                bool intercept = action.speed >= enemyAction.speed;
                if (selfDefense || intercept)
                {
                    clashTarget = enemyAction;
                    break;
                }
            }

            if (clashTarget == null) continue;

            plan.clashes.Add((action, clashTarget));
            matched.Add(action);
            matched.Add(clashTarget);
        }

        foreach (var action in sorted)
        {
            if (!matched.Contains(action))
                plan.unopposed.Add(action);
        }

        return plan;
    }
}
