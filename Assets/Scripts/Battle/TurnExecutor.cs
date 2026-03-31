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
    public void ExecuteUnopposed(TurnAction action)
    {
        if (!action.actor.IsAlive || !action.target.IsAlive) return;

        PlayAttackSequence(action.actor, action.target);

        var dmgResult = DamageProcessor.Process(new DamageProcessor.DamageContext {
            attacker = action.actor, target = action.target, skill = action.skill
        });

        if (dmgResult.bleedDamage > 0)
            Log($"  [출혈] {action.actor.UnitName} 출혈 피해 {dmgResult.bleedDamage}");
        if (dmgResult.paralyzedCoins > 0)
            Log($"  [마비] {action.actor.UnitName} 코인 {dmgResult.paralyzedCoins}개 무효!");
        Log($"[일방] {action.actor.UnitName}의 {action.skill.skillName}({BattleUtils.GetDamageTypeLabel(action.skill)}) → {dmgResult.finalDamage} 피해");

        ApplyHitEffects(action.target, dmgResult.finalDamage, action.skill.damageType);

        // 상태이상 부여
        if (action.skill.statusPotency > 0 && action.skill.statusCount > 0)
        {
            action.target.AddStatus(action.skill.inflictStatus,
                action.skill.statusPotency, action.skill.statusCount);
            Log($"  [{action.skill.inflictStatus}] {action.target.UnitName}에게 부여");
        }
    }
    public void ExecuteEvade(TurnAction action, SkillData evadeSkill, Unit evader)
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
            return;
        }

        int reducedDamage = attackPower - evadePower;
        var evadeResult = DamageProcessor.Process(new DamageProcessor.DamageContext {
            attacker = action.actor, target = action.target,
            skill = action.skill, rawDamage = reducedDamage, skipCoinRoll = true
        });
        Log($"[회피 실패] 관통 피해 {evadeResult.finalDamage} (공격 {attackPower} - 회피 {evadePower})");
        ApplyHitEffects(action.target, evadeResult.finalDamage, action.skill.damageType);
    }
    public void ApplyClashSideEffects(ClashResult clash, Unit attacker, Unit defender)
    {
        var winner = clash.winnerIsAttacker ? attacker : defender;
        if (clash.winnerSPChange != 0)
            winner.ChangeSP(clash.winnerSPChange);

        foreach (var sa in clash.statusApplications)
        {
            var target = sa.applyToAttacker ? attacker : defender;
            target.AddStatus(sa.type, sa.potency, sa.count);
            Log($"  [{sa.type}] {target.UnitName}에게 부여");
        }
    }

    public void ExecuteClashDamage(ClashResult clash, Unit ally, Unit enemy)
    {
        if (clash.outcome == ClashOutcome.AttackerWin)
        {
            var result = DamageProcessor.Process(new DamageProcessor.DamageContext {
                attacker = ally, target = enemy,
                skill = clash.attackerSkill, rawDamage = clash.damage, skipCoinRoll = true
            });
            ApplyHitEffects(enemy, result.finalDamage, clash.attackerSkill?.damageType ?? DamageType.Slash);
        }
        else if (clash.outcome == ClashOutcome.DefenderWin)
        {
            var result = DamageProcessor.Process(new DamageProcessor.DamageContext {
                attacker = enemy, target = ally,
                skill = clash.defenderSkill, rawDamage = clash.damage, skipCoinRoll = true
            });
            ApplyHitEffects(ally, result.finalDamage, clash.defenderSkill?.damageType ?? DamageType.Slash);
        }
    }

    public void ApplyHitEffects(Unit target, int finalDamage, DamageType damageType)
    {
        if (finalDamage <= 0) return;

        // Breakdown 텍스트 갱신
        _presenter.UpdateBreakdown(target, finalDamage, damageType);

        // HP바 피격
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

        // 피격 플래시
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
