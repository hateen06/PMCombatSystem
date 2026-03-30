using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

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
    private SkillData _evadeSkill; // 회피에 사용한 카드
    private int _selectedIndex = -1;
    private int _turnCount;
    private bool _isEvading;
    private bool _isGuarding;
    private int _guardCardIndex = -1; // Guard로 전환한 카드 인덱스


    public System.Action<string> OnLogMessage;
    public System.Action<ClashResult> OnClashResolved;
    public System.Action<BattleState> OnStateChanged;
    public System.Action<Unit, int> OnDamageDealt;
    public System.Action<string> OnBreakdownUpdated;
    public System.Action<string> OnClashPreviewUpdated;
    public System.Action<string> OnIntentUpdated;
    public System.Action<Unit, Unit> OnTargetPreviewUpdated;
    public System.Action OnHandDrawn;
    public System.Action<int, SkillData> OnCardOverridden;

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

        // Guard로 전환된 카드를 다시 클릭하면 Guard 유지
        if (_isGuarding && index == _guardCardIndex)
        {
            _selectedIndex = index;
            Log("선택: " + _selectedSkill.skillName);
            return;
        }

        // 다른 카드를 선택하면 Guard 해제
        if (_isGuarding)
        {
            _isGuarding = false;
            // 이전 Guard 카드 비주얼 복원
            if (_guardCardIndex >= 0 && _guardCardIndex < hand.Count)
                OnCardOverridden?.Invoke(_guardCardIndex, hand[_guardCardIndex]);
            _guardCardIndex = -1;
        }

        _selectedSkill = hand[index];
        _selectedIndex = index;
        if (_selectedSkill != null)
        {
            Log("선택: " + _selectedSkill.skillName);
            UpdateClashPreview(_selectedSkill);
        }
    }
    public void SelectEvade(int cardIndex)
    {
        _isEvading = true;
        _selectedSkill = null;
        _selectedIndex = cardIndex;

        // 버린 카드를 회피용으로 저장 후 소모
        if (allyUnit.Deck != null && cardIndex >= 0)
        {
            var hand = allyUnit.Deck.CurrentHand;
            if (cardIndex < hand.Count)
                _evadeSkill = hand[cardIndex];
            allyUnit.Deck.UseCard(cardIndex);
        }

        Log("[회피] " + allyUnit.UnitName + " 회피 태세 (" + 
            (_evadeSkill != null ? _evadeSkill.skillName : "?") + ")");
    }

    public void OnEvadeButton()
    {
        if (_selectedIndex < 0 && !_isGuarding) return;

        // Guard 상태에서 회피 누르면 Guard 해제 후 회피 진행
        int evadeIndex = _isGuarding ? _guardCardIndex : _selectedIndex;
        if (evadeIndex < 0) return;

        CancelGuard();
        _selectedIndex = evadeIndex;
        SelectEvade(_selectedIndex);
        ExecuteTurn();
    }

    public void OnGuardButton()
    {
        if (!_fsm.Is(BattleState.SkillSelect)) return;
        if (_selectedIndex < 0 && !_isGuarding) { Log("[!] 카드를 먼저 선택하세요"); return; }

        // 이미 Guard 상태면 토글 해제
        if (_isGuarding)
        {
            CancelGuard();
            return;
        }

        var guardSkill = FindGuardSkill();
        if (guardSkill == null) { Log("[!] Guard 스킬 없음"); return; }

        // 카드 비주얼을 Guard로 교체 (아직 턴 실행 안 함)
        _isGuarding = true;
        _guardCardIndex = _selectedIndex;
        OnCardOverridden?.Invoke(_selectedIndex, guardSkill);

        // 선택 상태를 Guard 스킬로 전환
        _selectedSkill = guardSkill;
        _isEvading = false;
        Log($"[방어 준비] {allyUnit.UnitName} → 방어 태세 ({guardSkill.skillName})");
    }

    private void CancelGuard()
    {
        if (!_isGuarding) return;

        // 원래 카드로 비주얼 복원
        if (allyUnit.Deck != null && _guardCardIndex >= 0)
        {
            var hand = allyUnit.Deck.CurrentHand;
            if (_guardCardIndex < hand.Count)
            {
                OnCardOverridden?.Invoke(_guardCardIndex, hand[_guardCardIndex]);
                _selectedSkill = hand[_guardCardIndex];
                _selectedIndex = _guardCardIndex;
            }
        }

        _isGuarding = false;
        _guardCardIndex = -1;
        Log("[방어 해제]");
    }

    private SkillData FindGuardSkill()
    {
        var slots = allyUnit.SkillSlots;
        if (slots == null) return null;
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] != null && slots[i].skillType == SkillType.Defense)
                return slots[i];
        return null;
    }

    public void ExecuteTurn()
    {
        if (!_fsm.Is(BattleState.SkillSelect)) return;
        if (_selectedSkill == null && !_isEvading && !_isGuarding) { Log("[!] 스킬을 먼저 선택하세요"); return; }

        // 선택 카드를 실제 덱에서 소모
        bool isGuarding = _isGuarding;
        if (isGuarding)
        {
            // Guard: 전환한 카드를 소모
            if (allyUnit.Deck != null && _guardCardIndex >= 0)
                allyUnit.Deck.UseCard(_guardCardIndex);

            // Shield 부여
            int shieldPower = CoinCalculator.RollPower(_selectedSkill, _selectedSkill.coinCount, allyUnit.CoinHeadsChance);
            allyUnit.ApplyShield(shieldPower);
            Log($"[방어] {allyUnit.UnitName} 방어 발동! 방어막 {shieldPower} (코인 굴림)");
        }
        else if (allyUnit.Deck != null && _selectedIndex >= 0)
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
        if (!allyUnit.IsStaggered && !_isEvading && !isGuarding)
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

            //회피 판정
            if (action.target == allyUnit && _isEvading && _evadeSkill != null)
            {
                int bleedDmg1 = action.actor.ConsumeBleed(action.skill.coinCount);
                if (bleedDmg1 > 0) Log($"  [출혈] {action.actor.UnitName} 출혈 피해 {bleedDmg1}");

                int paraCoins1 = action.actor.GetParalyzedCoins();
                int attackPower = CoinCalculator.RollPower(action.skill, action.skill.coinCount, action.actor.CoinHeadsChance, paraCoins1);
                if (paraCoins1 > 0) Log($"  [마비] {action.actor.UnitName} 코인 {paraCoins1}개 무효!");
                int evadePower = CoinCalculator.RollPower(_evadeSkill, _evadeSkill.coinCount, allyUnit.CoinHeadsChance);

                if (evadePower >= attackPower)
                {
                    Log("[회피 성공] " + allyUnit.UnitName + " 회피! (회피 " + evadePower + " >= 공격 " + attackPower + ")");
                    continue; // 피해 없음
                }
                else
                {
                    int reducedDamage = attackPower - evadePower;
                    Log("[회피 실패] 관통 피해 " + reducedDamage + " (공격 " + attackPower + " - 회피 " + evadePower + ")");
                    int evadeFinalDamage = action.target.TakeDamage(reducedDamage, action.skill.damageType);
                    ApplyHitEffects(action.target, evadeFinalDamage, action.skill.damageType);
                    continue;

                }
            }
            PlayOneSidedAttackSequence(action.actor, action.target);

            // 출혈: 공격자가 코인 던질 때 자신에게 고정 피해
            int bleedDmg = action.actor.ConsumeBleed(action.skill.coinCount);
            if (bleedDmg > 0) Log($"  [출혈] {action.actor.UnitName} 출혈 피해 {bleedDmg}");

            int paraCoins = action.actor.GetParalyzedCoins();
            int damage = CoinCalculator.RollPower(action.skill, action.skill.coinCount, action.actor.CoinHeadsChance, paraCoins);
            if (paraCoins > 0) Log($"  [마비] {action.actor.UnitName} 코인 {paraCoins}개 무효!");
            Log("[일방] " + action.actor.UnitName + "의 " + action.skill.skillName + "(" + GetDamageTypeLabel(action.skill) + ") -> " + damage + " 피해");
            int finalDamage = action.target.TakeDamage(damage, action.skill.damageType, action.actor.OffenseLevel);
            ApplyHitEffects(action.target, finalDamage, action.skill.damageType);
            if (action.skill.statusPotency > 0 && action.skill.statusCount > 0)
            {
                action.target.AddStatus(action.skill.inflictStatus, action.skill.statusPotency, action.skill.statusCount);
                Log("  " + action.skill.inflictStatus + " +" + action.skill.statusPotency + "/" + action.skill.statusCount);
            }
        }

        // 턴 종료: Shield 소멸 + 마비 횟수 감소
        allyUnit.ClearShield();
        enemyUnit.ClearShield();
        allyUnit.TickParalysis();
        enemyUnit.TickParalysis();

        CheckBattleEnd();
        _selectedSkill = null;
        _isEvading = false;
        _isGuarding = false;
        _guardCardIndex = -1;
        _evadeSkill = null;
        if (_fsm.Is(BattleState.SkillSelect)) DrawNewHands();
    }

    private void DrawNewHands()
    {
        if (allyUnit.Deck != null) allyUnit.Deck.DrawHand(3);
        if (enemyUnit.Deck != null) enemyUnit.Deck.DrawHand(3);
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

        string resultColored = result == "유리"
            ? "<color=#66FF99>유리</color>"
            : result == "불리"
                ? "<color=#FF6666>불리</color>"
                : "<color=#FFD966>보통</color>";

        string preview =
            $"[Preview]\n" +
            $"아군  {allySkill.skillName} {GetDamageTypeSymbol(allySkill)}  ~{allyEstimate}\n" +
            $"적군  {enemySkill.skillName} {GetDamageTypeSymbol(enemySkill)}  ~{enemyEstimate}\n" +
            $"판정  {resultColored}";

        string clashStateColored = result == "유리"
            ? "<color=#66FF99>우세</color>"
            : result == "불리"
                ? "<color=#FF6666>열세</color>"
                : "<color=#FFD966>경합</color>";

        string intent =
            $"[Intent]\n" +
            $"{allyUnit.UnitName} → {enemyUnit.UnitName}  {allySkill.skillName} {GetDamageTypeSymbol(allySkill)}\n" +
            $"{enemyUnit.UnitName} → {allyUnit.UnitName}  {enemySkill.skillName} {GetDamageTypeSymbol(enemySkill)}\n" +
            $"충돌: {clashStateColored}";

        OnClashPreviewUpdated?.Invoke(preview);
        OnIntentUpdated?.Invoke(intent);
        OnTargetPreviewUpdated?.Invoke(allyUnit, enemyUnit);
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

    private void PlayOneSidedAttackSequence(Unit attacker, Unit target)
    {
        if (attacker == null || target == null) return;

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
    }

    private void ApplyHitEffects(Unit target, int finalDamage, DamageType damageType)
    {
        if (finalDamage <= 0) return;

        float resist = target.GetResistance(damageType);
        string damageTypeLabel = GetDamageTypeLabel(damageType);
        string resistColored = resist < 1f
            ? $"<color=#66FF99>내성</color> x{resist:0.0}"
            : resist > 1f
                ? $"<color=#FF6666>취약</color> x{resist:0.0}"
                : $"보통 x{resist:0.0}";

        string staggerText = target.IsStaggered
            ? "  <color=#FF6666>흐트러짐!</color>"
            : "";

        string breakdown =
            $"[Breakdown]\n" +
            $"{target.UnitName}  {GetDamageTypeSymbol(damageType)} {resistColored}  → {finalDamage}{staggerText}";

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

    private string GetDamageTypeSymbol(SkillData skill)
    {
        if (skill == null) return "?";
        return GetDamageTypeSymbol(skill.damageType);
    }

    private string GetDamageTypeSymbol(DamageType damageType)
    {
        switch (damageType)
        {
            case DamageType.Slash: return "斬";
            case DamageType.Pierce: return "貫";
            case DamageType.Blunt: return "打";
            default: return "?";
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
