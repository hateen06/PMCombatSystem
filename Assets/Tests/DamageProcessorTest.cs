using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class DamageProcessorTest
{
    [Test]
    public void SkipCoinRoll_UsesRawDamage()
    {
        var go = new GameObject("Target");
        var unit = go.AddComponent<Unit>();

        var data = ScriptableObject.CreateInstance<UnitData>();
        data.unitName = "Test";
        data.baseHP = 100;
        data.hpPerLevel = 0;
        data.level = 1;
        data.slashResist = 1f;
        data.pierceResist = 1f;
        data.bluntResist = 1f;

        var field = typeof(Unit).GetField("unitData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field.SetValue(unit, data);
        unit.Initialize();

        var skill = ScriptableObject.CreateInstance<SkillData>();
        skill.damageType = DamageType.Slash;

        var result = DamageProcessor.Process(new DamageProcessor.DamageContext {
            target = unit, skill = skill, rawDamage = 20, skipCoinRoll = true
        });

        Assert.Greater(result.finalDamage, 0);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void NullTarget_ReturnsZero()
    {
        var result = DamageProcessor.Process(new DamageProcessor.DamageContext { target = null });
        Assert.AreEqual(0, result.finalDamage);
    }
}
