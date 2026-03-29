using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    [Header("유닛")]
    [SerializeField] private Unit allyUnit;
    [SerializeField] private Unit enemyUnit;

    [Header("HP바")]
    [SerializeField] private HPBar allyHPBar;
    [SerializeField] private HPBar enemyHPBar;

    [Header("연출")]
    [SerializeField] private CameraShake cameraShake;
    [SerializeField] private HitFlash allyFlash;
    [SerializeField] private HitFlash enemyFlash;

    private BattleStateMachine _fsm;
    private SkillData _selectedSkill;
    private int _selectedIndex = -1;
    private int _turnCount;

    public System.Action<string> OnLogMessage;
    public System.Action<ClashResult> OnClashResolved;
    public System.Action<BattleState> OnStateChanged;
    public System.Action<Unit, int> OnDamageDealt;
    public System.Action<string> OnBreakdownUpdated;
    public System.Action<string> OnClashPreviewUpdated;
    public System.Action<string> OnIntentUpdated;
    public System.Action OnHandDrawn;

    public Unit Ally => allyUnit;
    public Unit Enemy => enemyUnit;
    public BattleState CurrentState => _fsm.Current;
    public int TurnCount => _turnCount;

    private void Start()
    {
        _fsm = new BattleStateMachine();
        if (allyUnit == null || enemyUnit == null) { Log("[오류] 유닛 미연결"); return; }

        allyUnit.Initialize();
        enemyUnit.Initialize();
        if (allyHPBar != null) allyHPBar.Bind(allyUnit);
        if (enemyHPBar != null) enemyHPBar.Bind(enemyUnit);

        Log(">> 전투 시작");
        DrawNewHands();
        _fsm.TransitionTo(BattleState.SkillSelect);
        OnStateChanged?.Invoke(_fsm.Current);
    }

    public void SelectSkill(int index)
    {
        if (!_fsm.Is(BattleState.SkillSelect)) return;
        if (allyUnit.Deck == null) return;
        var hand = allyUnit.Deck.CurrentHand;
        if (index < 0 || index >= hand.Count) return;

        _selectedSkill = hand[index];
        _selectedIndex = index;
        if (_selectedSkill != null)
        {
            Log("선택: " + _selectedSkill.skillName);
            UpdateClashPreview(_selectedSkill);
        }
    }

    public void ExecuteTurn()
    {
        if (!_fsm.Is(BattleState.SkillSelect)) return;
        if (_selectedSkill == null) { Log("[!] 스킬을 먼저 선택하세요"); return; }

        // 선택 카드를 실제 덱에서 소모
        if (allyUnit.Deck != null && _selectedIndex >= 0)
        {
            _selectedSkill = allyUnit.Deck.UseCard(_selectedIndex);
        }

        _fsm.TransitionTo(BattleState.ClashResolve);
        OnStateChanged?.Invoke(_fsm.Current);
        _turnCount++;

        allyUnit.OnTurnStart();
        enemyUnit.OnTurnStart();

        Log("===== " + _turnCount + "턴 =====");
        if (allyUnit.IsStaggered) Log(allyUnit.UnitName + " 흐트러짐! 행동 불가");
        if (enemyUnit.IsStaggered) Log(enemyUnit.UnitName + " 흐트러짐! 행동 불가");

        SkillData enemySkill = PickEnemySkill();
        int allySpeed = allyUnit.RollSpeed();
        int enemySpeed = enemyUnit.RollSpeed();
        Log("속도: " + allyUnit.UnitName + " " + allySpeed + " / " + enemyUnit.UnitName + " " + enemySpeed);

        var actions = new List<TurnAction>();
        if (!allyUnit.IsStaggered)
            actions.Add(new TurnAction(allyUnit, _selectedSkill, enemyUnit, allySpeed));
        if (!enemyUnit.IsStaggered && enemySkill != null)
            actions.Add(new TurnAction(enemyUnit, enemySkill, allyUnit, enemySpeed));

        var plan = TurnResolver.Plan(actions);

        _fsm.TransitionTo(BattleState.ApplyResult);
        OnStateChanged?.Invoke(_fsm.Current);

        foreach (var pair in plan.clashes)
        {
            Log("[합] " + pair.attacker.skill.skillName + "(" + GetDamageTypeLabel(pair.attacker.skill) + ") vs " + pair.defender.skill.skillName + "(" + GetDamageTypeLabel(pair.defender.skill) + ")");
            var clash = ClashResolver.Resolve(pair.attacker.actor, pair.attacker.skill,
                pair.defender.actor, pair.defender.skill, pair.attacker.speed, pair.defender.speed);
            OnClashResolved?.Invoke(clash);
            ApplyClashDamage(clash);
            LogClashResult(clash);
        }

        foreach (var action in plan.unopposed)
        {
            if (!action.actor.IsAlive || !action.target.IsAlive) continue;
            int damage = CoinCalculator.RollPower(action.skill, action.skill.coinCount, action.actor.CoinHeadsChance);
            Log("[일방] " + action.actor.UnitName + "의 " + action.skill.skillName + "(" + GetDamageTypeLabel(action.skill) + ") -> " + damage + " 피해");
            int finalDamage = action.target.TakeDamage(damage, action.skill.damageType);
            ApplyHitEffects(action.target, finalDamage, action.skill.damageType);
            if (action.skill.statusPotency > 0 && action.skill.statusCount > 0)
            {
                action.target.AddStatus(action.skill.inflictStatus, action.skill.statusPotency, action.skill.statusCount);
                Log("  " + action.skill.inflictStatus + " +" + action.skill.statusPotency + "/" + action.skill.statusCount);
            }
        }

        CheckBattleEnd();
        _selectedSkill = null;
        if (_fsm.Is(BattleState.SkillSelect)) DrawNewHands();
    }

    private void DrawNewHands()
    {
        if (allyUnit.Deck != null) allyUnit.Deck.DrawHand(2);
        if (enemyUnit.Deck != null) enemyUnit.Deck.DrawHand(2);
        OnHandDrawn?.Invoke();
    }

    private void UpdateClashPreview(SkillData allySkill)
    {
        if (allySkill == null || enemyUnit == null)
        {
            OnClashPreviewUpdated?.Invoke(string.Empty);
            OnIntentUpdated?.Invoke(string.Empty);
            return;
        }

        SkillData enemySkill = null;
        if (enemyUnit.Deck != null && enemyUnit.Deck.CurrentHand.Count > 0)
            enemySkill = enemyUnit.Deck.CurrentHand[0];
        else if (enemyUnit.SkillSlots != null && enemyUnit.SkillSlots.Length > 0)
            enemySkill = enemyUnit.SkillSlots[0];

        if (enemySkill == null)
        {
            OnClashPreviewUpdated?.Invoke("[Preview]\n적 스킬 정보 없음");
            OnIntentUpdated?.Invoke("[Intent]\n적 의도 정보 없음");
            return;
        }

        int allyEstimate = allySkill.basePower + Mathf.RoundToInt(allySkill.coinCount * allySkill.coinPower * 0.5f);
        int enemyEstimate = enemySkill.basePower + Mathf.RoundToInt(enemySkill.coinCount * enemySkill.coinPower * 0.5f);
        string result;
        if (allyEstimate >= enemyEstimate + 3) result = "유리";
        else if (enemyEstimate >= allyEstimate + 3) result = "불리";
        else result = "보통";

        string preview =
            $"[Preview]\n" +
            $"아군: {allySkill.skillName} ({GetDamageTypeLabel(allySkill)}) ~ {allyEstimate}\n" +
            $"적군: {enemySkill.skillName} ({GetDamageTypeLabel(enemySkill)}) ~ {enemyEstimate}\n" +
            $"예상 판정: {result}";

        string intent =
            $"[Intent]\n" +
            $"1. {allyUnit.UnitName} → {enemyUnit.UnitName}\n" +
            $"   스킬: {allySkill.skillName} / 타입: {GetDamageTypeLabel(allySkill)}\n" +
            $"2. {enemyUnit.UnitName} → {allyUnit.UnitName}\n" +
            $"   스킬: {enemySkill.skillName} / 타입: {GetDamageTypeLabel(enemySkill)}\n" +
            $"충돌 예상: {(result == "보통" ? "합 예상" : "우세 비교 가능")}";

        OnClashPreviewUpdated?.Invoke(preview);
        OnIntentUpdated?.Invoke(intent);
    }

    private void ApplyClashDamage(ClashResult clash)
    {
        if (clash.outcome == ClashOutcome.AttackerWin)
        {
            var type = clash.attackerSkill != null ? clash.attackerSkill.damageType : DamageType.Slash;
            int finalDamage = enemyUnit.TakeDamage(clash.damage, type);
            ApplyHitEffects(enemyUnit, finalDamage, type);
        }
        else if (clash.outcome == ClashOutcome.DefenderWin)
        {
            var type = clash.defenderSkill != null ? clash.defenderSkill.damageType : DamageType.Slash;
            int finalDamage = allyUnit.TakeDamage(clash.damage, type);
            ApplyHitEffects(allyUnit, finalDamage, type);
        }
    }

    private void ApplyHitEffects(Unit target, int finalDamage, DamageType damageType)
    {
        if (finalDamage <= 0) return;

        float resist = target.GetResistance(damageType);
        string damageTypeLabel = GetDamageTypeLabel(damageType);
        string breakdown =
            $"[Breakdown]\n" +
            $"대상: {target.UnitName}\n" +
            $"피해 타입: {damageTypeLabel}\n" +
            $"저항: {GetResistanceLabel(resist)} x{resist:0.0}\n" +
            $"최종 피해: {finalDamage}\n" +
            $"흐트러짐: {(target.IsStaggered ? "발생" : "없음")}";

        bool isAlly = target == allyUnit;
        if (isAlly) { if (allyHPBar != null) allyHPBar.OnHit(); if (allyFlash != null) allyFlash.Flash(); }
        else { if (enemyHPBar != null) enemyHPBar.OnHit(); if (enemyFlash != null) enemyFlash.Flash(); }
        if (cameraShake != null) cameraShake.Shake();
        OnDamageDealt?.Invoke(target, finalDamage);
        OnBreakdownUpdated?.Invoke(breakdown);

        Log("  >> 저항: " + GetResistanceLabel(resist) + " x" + resist.ToString("0.0") + " => " + finalDamage + " 피해");

        if (target.IsStaggered) Log("  >> " + target.UnitName + " 흐트러짐 발생!");
    }

    private void LogClashResult(ClashResult clash)
    {
        foreach (var line in clash.log.Split('|'))
        {
            var trimmed = line.Trim();
            if (trimmed.Length > 0) Log(trimmed);
        }
    }

    private void CheckBattleEnd()
    {
        if (!allyUnit.IsAlive || !enemyUnit.IsAlive)
        {
            _fsm.TransitionTo(BattleState.BattleEnd);
            OnStateChanged?.Invoke(_fsm.Current);
            if (!allyUnit.IsAlive && !enemyUnit.IsAlive) Log("[무승부]");
            else if (!enemyUnit.IsAlive) Log("[승리!]");
            else Log("[패배...]");
        }
        else
        {
            _fsm.TransitionTo(BattleState.SkillSelect);
            OnStateChanged?.Invoke(_fsm.Current);
        }
    }

    private SkillData PickEnemySkill()
    {
        if (enemyUnit.Deck != null && enemyUnit.Deck.CurrentHand.Count > 0)
        {
            var hand = enemyUnit.Deck.CurrentHand;
            int index = Random.Range(0, hand.Count);
            return enemyUnit.Deck.UseCard(index);
        }
        var slots = enemyUnit.SkillSlots;
        if (slots == null || slots.Length == 0) return null;
        return slots[Random.Range(0, slots.Length)];
    }

    private string GetDamageTypeLabel(SkillData skill)
    {
        if (skill == null) return "없음";
        return GetDamageTypeLabel(skill.damageType);
    }

    private string GetDamageTypeLabel(DamageType damageType)
    {
        switch (damageType)
        {
            case DamageType.Slash: return "참격";
            case DamageType.Pierce: return "관통";
            case DamageType.Blunt: return "타격";
            default: return "없음";
        }
    }

    private string GetResistanceLabel(float resist)
    {
        if (resist < 1f) return "내성";
        if (resist > 1f) return "취약";
        return "보통";
    }

    private void Log(string message)
    {
        Debug.Log("[Battle] " + message);
        OnLogMessage?.Invoke(message);
    }
}
