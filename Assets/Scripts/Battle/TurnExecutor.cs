using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class TurnExecutor
{
    private readonly List<Unit> _allyUnits;
    private readonly List<Unit> _enemyUnits;
    private readonly List<HPBar> _allyHPBars;
    private readonly List<HPBar> _enemyHPBars;
    private readonly CameraShake _cameraShake;
    private readonly BattlePresenter _presenter;

    public System.Action<Unit, int> OnDamageDealt;
    public System.Action<string> OnLogMessage;

    public TurnExecutor(
        List<Unit> allyUnits, List<Unit> enemyUnits,
        List<HPBar> allyHPBars, List<HPBar> enemyHPBars,
        CameraShake cameraShake, BattlePresenter presenter)
    {
        _allyUnits = allyUnits;
        _enemyUnits = enemyUnits;
        _allyHPBars = allyHPBars;
        _enemyHPBars = enemyHPBars;
        _cameraShake = cameraShake;
        _presenter = presenter;
    }

    public int ExecuteUnopposed(TurnAction action)
    {
        if (!action.actor.IsAlive || !action.target.IsAlive) return 0;

        PlayAttackSequence(action.actor, action.target);

        int paraCoins = action.actor.GetParalyzedCoins();
        var hitPowers = CoinCalculator.RollHitPowers(action.skill, action.skill.coinCount, action.actor.CoinHeadsChance, paraCoins);
        int bleedDmg = action.actor.ConsumeBleed(action.skill.coinCount);
        if (bleedDmg > 0) Log($"  [출혈] {action.actor.UnitName} 출혈 피해 {bleedDmg}");
        if (paraCoins > 0) Log($"  [마비] {action.actor.UnitName} 코인 {paraCoins}개 무효!");

        int totalDamage = 0;
        for (int i = 0; i < hitPowers.Count && action.target.IsAlive; i++)
        {
            var dmgResult = DamageProcessor.Process(new DamageProcessor.DamageContext
            {
                attacker = action.actor,
                target = action.target,
                skill = action.skill,
                rawDamage = hitPowers[i],
                skipCoinRoll = true
            });

            totalDamage += dmgResult.finalDamage;
            ApplyHitEffects(action.target, dmgResult.finalDamage, action.skill.damageType);
        }

        Log($"[일방] {action.actor.UnitName}의 {action.skill.skillName}({BattleUtils.GetDamageTypeLabel(action.skill)}) → {totalDamage} 피해");

        if (action.skill.statusPotency > 0 && action.skill.statusCount > 0 && action.target.IsAlive)
        {
            action.target.AddStatus(action.skill.inflictStatus, action.skill.statusPotency, action.skill.statusCount);
            Log($"  [{action.skill.inflictStatus}] {action.target.UnitName}에게 부여");
        }

        return totalDamage;
    }

    public int ExecuteEvade(TurnAction action, SkillData evadeSkill, Unit evader)
    {
        int paraCoins = action.actor.GetParalyzedCoins();
        int attackPower = CoinCalculator.RollPower(action.skill, action.skill.coinCount, action.actor.CoinHeadsChance, paraCoins);
        int bleedDmg = action.actor.ConsumeBleed(action.skill.coinCount);
        if (bleedDmg > 0) Log($"  [출혈] {action.actor.UnitName} 출혈 피해 {bleedDmg}");
        if (paraCoins > 0) Log($"  [마비] {action.actor.UnitName} 코인 {paraCoins}개 무효!");

        int evadePower = CoinCalculator.RollPower(evadeSkill, evadeSkill.coinCount, evader.CoinHeadsChance);

        if (evadePower >= attackPower)
        {
            Log($"[회피 성공] {evader.UnitName} 회피! (회피 {evadePower} >= 공격 {attackPower})");
            return 0;
        }

        int reducedDamage = attackPower - evadePower;
        var evadeResult = DamageProcessor.Process(new DamageProcessor.DamageContext
        {
            attacker = action.actor,
            target = action.target,
            skill = action.skill,
            rawDamage = reducedDamage,
            skipCoinRoll = true
        });
        Log($"[회피 실패] 관통 피해 {evadeResult.finalDamage} (공격 {attackPower} - 회피 {evadePower})");
        ApplyHitEffects(action.target, evadeResult.finalDamage, action.skill.damageType);
        return evadeResult.finalDamage;
    }

    public void ApplyClashSideEffects(ClashResult clash, Unit attacker, Unit defender)
    {
        foreach (var sa in clash.statusApplications)
        {
            var target = sa.applyToAttacker ? attacker : defender;
            target.AddStatus(sa.type, sa.potency, sa.count);
            Log($"  [{sa.type}] {target.UnitName}에게 부여");
        }
    }

    public int ExecuteClashDamage(ClashResult clash, Unit ally, Unit enemy)
    {
        int totalDamage = 0;
        if (clash.outcome == ClashOutcome.AttackerWin)
        {
            for (int i = 0; i < clash.followUpHitPowers.Count && enemy.IsAlive; i++)
            {
                var result = DamageProcessor.Process(new DamageProcessor.DamageContext
                {
                    attacker = ally,
                    target = enemy,
                    skill = clash.attackerSkill,
                    rawDamage = clash.followUpHitPowers[i],
                    skipCoinRoll = true
                });
                totalDamage += result.finalDamage;
                ApplyHitEffects(enemy, result.finalDamage, clash.attackerSkill?.damageType ?? DamageType.Slash);
            }
        }
        else if (clash.outcome == ClashOutcome.DefenderWin)
        {
            for (int i = 0; i < clash.followUpHitPowers.Count && ally.IsAlive; i++)
            {
                var result = DamageProcessor.Process(new DamageProcessor.DamageContext
                {
                    attacker = enemy,
                    target = ally,
                    skill = clash.defenderSkill,
                    rawDamage = clash.followUpHitPowers[i],
                    skipCoinRoll = true
                });
                totalDamage += result.finalDamage;
                ApplyHitEffects(ally, result.finalDamage, clash.defenderSkill?.damageType ?? DamageType.Slash);
            }
        }
        return totalDamage;
    }

    public void ApplyHitEffects(Unit target, int finalDamage, DamageType damageType)
    {
        if (finalDamage <= 0) return;

        _presenter.UpdateBreakdown(target, finalDamage, damageType);

        bool isAlly = _allyUnits.Contains(target);
        if (isAlly)
        {
            int idx = _allyUnits.IndexOf(target);
            if (idx >= 0 && idx < _allyHPBars.Count && _allyHPBars[idx] != null) _allyHPBars[idx].OnHit();
        }
        else
        {
            int idx = _enemyUnits.IndexOf(target);
            if (idx >= 0 && idx < _enemyHPBars.Count && _enemyHPBars[idx] != null) _enemyHPBars[idx].OnHit();
        }

        var flash = target.GetComponent<HitFlash>();
        if (flash != null) flash.Flash();
        else
        {
            var sr = target.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                var origColor = sr.color;
                sr.color = Color.red;
                DOVirtual.DelayedCall(0.1f, () => { if (sr != null) sr.color = origColor; });
            }
        }

        if (_cameraShake != null) _cameraShake.Shake();
        OnDamageDealt?.Invoke(target, finalDamage);

        if (target.IsStaggered)
            Log($"  >> {target.UnitName} 흐트러짐 발생!");
    }

    public Sequence PlayAttackSequence(Unit attacker, Unit target)
    {
        if (attacker == null || target == null) return null;

        var attackerTr = attacker.transform;
        var targetTr = target.transform;
        Vector3 attackerStart = attackerTr.position;
        Vector3 targetStart = targetTr.position;
        float dir = Mathf.Sign(targetStart.x - attackerStart.x);
        if (dir == 0f) dir = 1f;
        Vector3 attackStep = new Vector3(dir * 0.8f, 0f, 0f);
        Vector3 recoilStep = new Vector3(dir * 0.25f, 0f, 0f);

        attackerTr.DOKill();
        targetTr.DOKill();
        Sequence seq = DOTween.Sequence();
        seq.Append(attackerTr.DOMove(attackerStart + attackStep, 0.12f).SetEase(Ease.OutQuad));
        seq.AppendInterval(0.03f);
        seq.Append(targetTr.DOMove(targetStart + recoilStep, 0.08f).SetEase(Ease.OutQuad));
        seq.Append(targetTr.DOMove(targetStart, 0.10f).SetEase(Ease.InQuad));
        seq.Join(attackerTr.DOMove(attackerStart, 0.16f).SetEase(Ease.InOutQuad));
        return seq;
    }

    private void Log(string msg) => OnLogMessage?.Invoke(msg);
}
