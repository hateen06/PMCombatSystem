using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class TurnResolverTest
{
    private TurnAction MakeAction(string name, int speed)
    {
        var go = new GameObject(name);
        var unit = go.AddComponent<Unit>();
        return new TurnAction(unit, null, unit, speed);
    }

    [Test]
    public void MutualTarget_FormsClash()
    {
        var go1 = new GameObject("A"); var u1 = go1.AddComponent<Unit>();
        var go2 = new GameObject("B"); var u2 = go2.AddComponent<Unit>();
        var a1 = new TurnAction(u1, null, u2, 5);
        var a2 = new TurnAction(u2, null, u1, 3);
        var plan = TurnResolver.Plan(new List<TurnAction> { a1, a2 });
        Assert.AreEqual(1, plan.clashes.Count);
        Assert.AreEqual(0, plan.unopposed.Count);
        Object.DestroyImmediate(go1); Object.DestroyImmediate(go2);
    }

    [Test]
    public void OneAction_IsUnopposed()
    {
        var go1 = new GameObject("A"); var u1 = go1.AddComponent<Unit>();
        var go2 = new GameObject("B"); var u2 = go2.AddComponent<Unit>();
        var a1 = new TurnAction(u1, null, u2, 5);
        var plan = TurnResolver.Plan(new List<TurnAction> { a1 });
        Assert.AreEqual(0, plan.clashes.Count);
        Assert.AreEqual(1, plan.unopposed.Count);
        Object.DestroyImmediate(go1); Object.DestroyImmediate(go2);
    }

    [Test]
    public void EmptyList_NoCrash()
    {
        var plan = TurnResolver.Plan(new List<TurnAction>());
        Assert.AreEqual(0, plan.clashes.Count);
        Assert.AreEqual(0, plan.unopposed.Count);
    }
}
