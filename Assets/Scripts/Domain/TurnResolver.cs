using System.Collections.Generic;
using System.Linq;
public static class TurnResolver
{
    public class TurnPlan
    {
        // 합이 성립된 쌍
        public List<(TurnAction attacker, TurnAction defender)> clashes
            = new List<(TurnAction, TurnAction)>();

        // 합 상대가 없는 일방공격
        public List<TurnAction> unopposed
            = new List<TurnAction>();
    }
    public static TurnPlan Plan(List<TurnAction> actions)
    {
        var plan = new TurnPlan();
        var matched = new HashSet<Unit>();

        // 속도순 정렬 (빠른 순)
        var sorted = actions.OrderByDescending(a => a.speed).ToList();

        // 합 매칭: 서로 공격하는 쌍 찾기
        for (int i = 0; i < sorted.Count; i++)
        {
            if (matched.Contains(sorted[i].actor)) continue;
            if (sorted[i].skill == null || sorted[i].skill.skillType != SkillType.Attack) continue;

            for (int j = i + 1; j < sorted.Count; j++)
            {
                if (matched.Contains(sorted[j].actor)) continue;
                if (sorted[j].skill == null || sorted[j].skill.skillType != SkillType.Attack) continue;

                // 서로를 타겟하고 있으면 합 성립
                if (sorted[i].target == sorted[j].actor &&
                    sorted[j].target == sorted[i].actor)
                {
                    // 속도 높은 쪽이 attacker
                    plan.clashes.Add((sorted[i], sorted[j]));
                    matched.Add(sorted[i].actor);
                    matched.Add(sorted[j].actor);
                    break;
                }
            }
        }

        // 매칭 안 된 행동 → 일방공격
        foreach (var action in sorted)
        {
            if (!matched.Contains(action.actor))
                plan.unopposed.Add(action);
        }

        return plan;
    }
}
