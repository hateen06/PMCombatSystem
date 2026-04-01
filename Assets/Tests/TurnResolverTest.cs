using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class TurnResolverTest
{
    [Test]
    public void MutualTarget_FormsClash()
    {
        var go1 = new GameObject("A");
        var u1 = go1.AddComponent<Unit>();
        var go2 = new GameObject("B");
        var u2 = go2.AddComponent<Unit>();
        var skill = ScriptableObject.CreateInstance<SkillData>();
        skill.skillType = SkillType.Attack;
        var a1 = new TurnAction(u1, skill, u2, 5, true);
        var a2 = new TurnAction(u2, skill, u1, 3, false);
        var plan = TurnResolver.Plan(new List<TurnAction> { a1, a2 });
        Assert.AreEqual(1, plan.clashes.Count);
        Assert.AreEqual(0, plan.unopposed.Count);
        Object.DestroyImmediate(skill);
        Object.DestroyImmediate(go1);
        Object.DestroyImmediate(go2);
    }

    [Test]
    public void FasterAlly_CanInterceptEnemyTargetingAnotherAlly()
    {
        var go1 = new GameObject("FastAlly");
        var fastAlly = go1.AddComponent<Unit>();
        var go2 = new GameObject("SlowAlly");
        var slowAlly = go2.AddComponent<Unit>();
        var go3 = new GameObject("Enemy");
        var enemy = go3.AddComponent<Unit>();
        var skill = ScriptableObject.CreateInstance<SkillData>();
        skill.skillType = SkillType.Attack;

        var allyAction = new TurnAction(fastAlly, skill, enemy, 7, true);
        var enemyAction = new TurnAction(enemy, skill, slowAlly, 4, false);

        var plan = TurnResolver.Plan(new List<TurnAction> { allyAction, enemyAction });
        Assert.AreEqual(1, plan.clashes.Count);
        Assert.AreEqual(0, plan.unopposed.Count);

        Object.DestroyImmediate(skill);
        Object.DestroyImmediate(go1);
        Object.DestroyImmediate(go2);
        Object.DestroyImmediate(go3);
    }

    [Test]
    public void SlowerAlly_CanClashIfEnemyTargetsSelf()
    {
        var go1 = new GameObject("Ally");
        var ally = go1.AddComponent<Unit>();
        var go2 = new GameObject("Enemy");
        var enemy = go2.AddComponent<Unit>();
        var skill = ScriptableObject.CreateInstance<SkillData>();
        skill.skillType = SkillType.Attack;

        var allyAction = new TurnAction(ally, skill, enemy, 2, true);
        var enemyAction = new TurnAction(enemy, skill, ally, 4, false);

        var plan = TurnResolver.Plan(new List<TurnAction> { allyAction, enemyAction });
        Assert.AreEqual(1, plan.clashes.Count);
        Assert.AreEqual(0, plan.unopposed.Count);

        Object.DestroyImmediate(skill);
        Object.DestroyImmediate(go1);
        Object.DestroyImmediate(go2);
    }

    [Test]
    public void SlowerAlly_CannotInterceptEnemyTargetingSomeoneElse()
    {
        var go1 = new GameObject("SlowAlly");
        var slowAlly = go1.AddComponent<Unit>();
        var go2 = new GameObject("OtherAlly");
        var otherAlly = go2.AddComponent<Unit>();
        var go3 = new GameObject("Enemy");
        var enemy = go3.AddComponent<Unit>();
        var skill = ScriptableObject.CreateInstance<SkillData>();
        skill.skillType = SkillType.Attack;

        var allyAction = new TurnAction(slowAlly, skill, enemy, 2, true);
        var enemyAction = new TurnAction(enemy, skill, otherAlly, 4, false);

        var plan = TurnResolver.Plan(new List<TurnAction> { allyAction, enemyAction });
        Assert.AreEqual(0, plan.clashes.Count);
        Assert.AreEqual(2, plan.unopposed.Count);

        Object.DestroyImmediate(skill);
        Object.DestroyImmediate(go1);
        Object.DestroyImmediate(go2);
        Object.DestroyImmediate(go3);
    }

    [Test]
    public void OneAction_IsUnopposed()
    {
        var go1 = new GameObject("A");
        var u1 = go1.AddComponent<Unit>();
        var go2 = new GameObject("B");
        var u2 = go2.AddComponent<Unit>();
        var skill = ScriptableObject.CreateInstance<SkillData>();
        skill.skillType = SkillType.Attack;
        var a1 = new TurnAction(u1, skill, u2, 5, true);
        var plan = TurnResolver.Plan(new List<TurnAction> { a1 });
        Assert.AreEqual(0, plan.clashes.Count);
        Assert.AreEqual(1, plan.unopposed.Count);
        Object.DestroyImmediate(skill);
        Object.DestroyImmediate(go1);
        Object.DestroyImmediate(go2);
    }

    [Test]
    public void FasterInterceptorLeavesSlowerAllyAsOneSidedAttack()
    {
        var go1 = new GameObject("FastAlly");
        var fastAlly = go1.AddComponent<Unit>();
        var go2 = new GameObject("SlowAlly");
        var slowAlly = go2.AddComponent<Unit>();
        var go3 = new GameObject("Enemy");
        var enemy = go3.AddComponent<Unit>();
        var skill = ScriptableObject.CreateInstance<SkillData>();
        skill.skillType = SkillType.Attack;

        var fastAction = new TurnAction(fastAlly, skill, enemy, 7, true);
        var slowAction = new TurnAction(slowAlly, skill, enemy, 2, true);
        var enemyAction = new TurnAction(enemy, skill, slowAlly, 4, false);

        var plan = TurnResolver.Plan(new List<TurnAction> { fastAction, slowAction, enemyAction });
        Assert.AreEqual(1, plan.clashes.Count);
        Assert.AreEqual(1, plan.unopposed.Count);
        Assert.AreSame(slowAction, plan.unopposed[0]);

        Object.DestroyImmediate(skill);
        Object.DestroyImmediate(go1);
        Object.DestroyImmediate(go2);
        Object.DestroyImmediate(go3);
    }

    [Test]
    public void EmptyList_NoCrash()
    {
        var plan = TurnResolver.Plan(new List<TurnAction>());
        Assert.AreEqual(0, plan.clashes.Count);
        Assert.AreEqual(0, plan.unopposed.Count);
    }
}
