using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class BattleManager : MonoBehaviour
{
    [Header("유닛")]
    [SerializeField] private List<Unit> allyUnits = new();
    [SerializeField] private List<Unit> enemyUnits = new();

    // 현재 활성 아군/적 (1vs1에선 첫 번째)
    public Unit Ally => allyUnits.Count > 0 ? allyUnits[0] : null;
    public Unit Enemy => enemyUnits.Count > 0 ? enemyUnits[0] : null;
    public IReadOnlyList<Unit> AllyUnits => allyUnits;
    public IReadOnlyList<Unit> EnemyUnits => enemyUnits;


    [Header("HP바")]
    [SerializeField] private List<HPBar> allyHPBars = new();
    [SerializeField] private List<HPBar> enemyHPBars = new();

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

    // 유닛별 스킬 선택 상태 (멀티유닛용)
    private Dictionary<int, SkillData> _unitSelectedSkills = new();
    private Dictionary<int, int> _unitSelectedIndices = new();

    // 타겟팅 (집중전 방식)
    private Unit _currentTarget;
    private Dictionary<int, Unit> _unitTargets = new(); // 유닛별 타겟


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


    public BattleState CurrentState => _fsm.Current;
    public int TurnCount => _turnCount;

    private void Start()
    {
        _fsm = new BattleStateMachine();
        if (allyUnits.Count == 0 || enemyUnits.Count == 0) { Log("[오류] 유닛 미연결"); return; }

        foreach (var unit in allyUnits) unit.Initialize();
        foreach (var unit in enemyUnits) unit.Initialize();
        for (int i = 0; i < allyHPBars.Count && i < allyUnits.Count; i++)
            if (allyHPBars[i] != null) allyHPBars[i].Bind(allyUnits[i]);
        for (int i = 0; i < enemyHPBars.Count && i < enemyUnits.Count; i++)
            if (enemyHPBars[i] != null) enemyHPBars[i].Bind(enemyUnits[i]);

        Log(">> 전투 시작");
        DrawNewHands();
        _fsm.TransitionTo(BattleState.SkillSelect);
        OnStateChanged?.Invoke(_fsm.Current);
    }

    // 이벤트: 타겟 지정됨
    public System.Action<Unit, Unit> OnTargetAssigned; // (attacker, target)

    /// <summary>적 클릭 시 현재 선택 중인 아군의 타겟으로 지정</summary>
    public void SetTarget(Unit target)
    {
        if (!_fsm.Is(BattleState.SkillSelect)) return;
        if (target == null || !target.IsAlive) return;

        // 어느 아군이 타겟을 지정하는지 결정
        // 아군1 카드가 선택됐으면 아군1, 아군2면 아군2
        int activeAlly = _selectedIndex >= 0 ? 0 : -1;
        // 유닛별 선택도 체크
        foreach (var kv in _unitSelectedIndices)
            if (kv.Value >= 0) activeAlly = kv.Key;
        
        if (activeAlly < 0) activeAlly = 0; // 기본 아군1

        _unitTargets[activeAlly] = target;
        _currentTarget = target;
        Log($"[타겟] {(activeAlly < allyUnits.Count ? allyUnits[activeAlly].UnitName : "?")} → {target.UnitName}");
        OnTargetAssigned?.Invoke(activeAlly < allyUnits.Count ? allyUnits[activeAlly] : null, target);

        if (_selectedSkill != null)
            UpdateClashPreview(_selectedSkill);
    }

    /// <summary>유닛 인덱스별 스킬 선택 (멀티유닛용)</summary>
    public void SelectSkillForUnit(int unitIndex, int cardIndex)
    {
        if (!_fsm.Is(BattleState.SkillSelect)) return;
        if (unitIndex < 0 || unitIndex >= allyUnits.Count) return;
        var unit = allyUnits[unitIndex];
        if (unit.Deck == null) return;
        var hand = unit.Deck.CurrentHand;
        if (cardIndex < 0 || cardIndex >= hand.Count) return;

        _unitSelectedSkills[unitIndex] = hand[cardIndex];
        _unitSelectedIndices[unitIndex] = cardIndex;
        Log($"[유닛{unitIndex}] 선택: {hand[cardIndex].skillName}");
    }

    public void SelectSkill(int index)
    {
        if (!_fsm.Is(BattleState.SkillSelect)) return;
        if (Ally.Deck == null) return;
        var hand = Ally.Deck.CurrentHand;
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
        if (Ally.Deck != null && cardIndex >= 0)
        {
            var hand = Ally.Deck.CurrentHand;
            if (cardIndex < hand.Count)
                _evadeSkill = hand[cardIndex];
            Ally.Deck.UseCard(cardIndex);
        }

        Log("[회피] " + Ally.UnitName + " 회피 태세 (" + 
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
        Log($"[방어 준비] {Ally.UnitName} → 방어 태세 ({guardSkill.skillName})");
    }

    private void CancelGuard()
    {
        if (!_isGuarding) return;

        // 원래 카드로 비주얼 복원
        if (Ally.Deck != null && _guardCardIndex >= 0)
        {
            var hand = Ally.Deck.CurrentHand;
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
        var slots = Ally.SkillSlots;
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
            if (Ally.Deck != null && _guardCardIndex >= 0)
                Ally.Deck.UseCard(_guardCardIndex);

            // Shield 부여
            int shieldPower = CoinCalculator.RollPower(_selectedSkill, _selectedSkill.coinCount, Ally.CoinHeadsChance);
            Ally.ApplyShield(shieldPower);
            Log($"[방어] {Ally.UnitName} 방어 발동! 방어막 {shieldPower} (코인 굴림)");
        }
        else if (Ally.Deck != null && _selectedIndex >= 0)
        {
            _selectedSkill = Ally.Deck.UseCard(_selectedIndex);
        }

        _fsm.TransitionTo(BattleState.ClashResolve);
        OnStateChanged?.Invoke(_fsm.Current);
        _turnCount++;

        foreach (var unit in allyUnits) unit.OnTurnStart();
        foreach (var unit in enemyUnits) unit.OnTurnStart();

        Log("===== " + _turnCount + "턴 =====");
        if (Ally.IsStaggered) Log(Ally.UnitName + " 흐트러짐! 행동 불가");
        if (Enemy.IsStaggered) Log(Enemy.UnitName + " 흐트러짐! 행동 불가");

        SkillData enemySkill = PickEnemySkill();
        int allySpeed = Ally.RollSpeed();
        int enemySpeed = Enemy.RollSpeed();
        Log("속도: " + Ally.UnitName + " " + allySpeed + " / " + Enemy.UnitName + " " + enemySpeed);

        var actions = new List<TurnAction>();
        // 아군1 행동 — 타겟 지정된 적 우선, 없으면 랜덤
        if (!Ally.IsStaggered && !_isEvading && !isGuarding)
        {
            Unit ally1Target = _unitTargets.ContainsKey(0) ? _unitTargets[0] : GetRandomAliveEnemy();
            if (ally1Target != null && !ally1Target.IsAlive) ally1Target = GetRandomAliveEnemy();
            actions.Add(new TurnAction(Ally, _selectedSkill, ally1Target ?? Enemy, allySpeed));
        }
        
        // 아군2+ 행동 (멀티유닛)
        for (int ui = 1; ui < allyUnits.Count; ui++)
        {
            var unit = allyUnits[ui];
            if (!unit.IsAlive || unit.IsStaggered) continue;
            SkillData unitSkill = null;
            if (_unitSelectedSkills.TryGetValue(ui, out unitSkill) && unitSkill != null)
            {
                // 선택한 카드 소모
                if (_unitSelectedIndices.TryGetValue(ui, out int ci) && unit.Deck != null)
                    unit.Deck.UseCard(ci);
                int spd = unit.RollSpeed();
                // 타겟 지정된 적 우선, 없으면 랜덤
                Unit target = _unitTargets.ContainsKey(ui) ? _unitTargets[ui] : GetRandomAliveEnemy();
                if (target != null && !target.IsAlive) target = GetRandomAliveEnemy();
                if (target != null)
                    actions.Add(new TurnAction(unit, unitSkill, target, spd));
            }
        }

        // 적 전원 행동
        for (int ei = 0; ei < enemyUnits.Count; ei++)
        {
            var eUnit = enemyUnits[ei];
            if (!eUnit.IsAlive || eUnit.IsStaggered) continue;
            SkillData eSkill = (ei == 0) ? enemySkill : PickEnemySkillFor(eUnit);
            if (eSkill == null) continue;
            int eSpd = eUnit.RollSpeed();
            var eTarget = GetRandomAliveAlly();
            if (eTarget != null)
                actions.Add(new TurnAction(eUnit, eSkill, eTarget, eSpd));
        }

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
            if (allyUnits.Contains(action.target) && _isEvading && _evadeSkill != null)
            {
                int bleedDmg1 = action.actor.ConsumeBleed(action.skill.coinCount);
                if (bleedDmg1 > 0) Log($"  [출혈] {action.actor.UnitName} 출혈 피해 {bleedDmg1}");

                int paraCoins1 = action.actor.GetParalyzedCoins();
                int attackPower = CoinCalculator.RollPower(action.skill, action.skill.coinCount, action.actor.CoinHeadsChance, paraCoins1);
                if (paraCoins1 > 0) Log($"  [마비] {action.actor.UnitName} 코인 {paraCoins1}개 무효!");
                int evadePower = CoinCalculator.RollPower(_evadeSkill, _evadeSkill.coinCount, Ally.CoinHeadsChance);

                if (evadePower >= attackPower)
                {
                    Log("[회피 성공] " + Ally.UnitName + " 회피! (회피 " + evadePower + " >= 공격 " + attackPower + ")");
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
        foreach (var unit in allyUnits) { unit.ClearShield(); unit.TickParalysis(); }
        foreach (var unit in enemyUnits) { unit.ClearShield(); unit.TickParalysis(); }

        CheckBattleEnd();
        _selectedSkill = null;
        _isEvading = false;
        _isGuarding = false;
        _guardCardIndex = -1;
        _evadeSkill = null;
        _unitSelectedSkills.Clear();
        _unitSelectedIndices.Clear();
        _unitTargets.Clear();
        _currentTarget = null;
        if (_fsm.Is(BattleState.SkillSelect)) DrawNewHands();
    }

    private void DrawNewHands()
    {
        foreach (var unit in allyUnits) if (unit.Deck != null) unit.Deck.DrawHand(3);
        foreach (var unit in enemyUnits) if (unit.Deck != null) unit.Deck.DrawHand(3);
        OnHandDrawn?.Invoke();
    }

    private void UpdateClashPreview(SkillData allySkill)
    {
        if (allySkill == null || Enemy == null)
        {
            OnClashPreviewUpdated?.Invoke(string.Empty);
            OnIntentUpdated?.Invoke(string.Empty);
            return;
        }

        SkillData enemySkill = null;
        if (Enemy.Deck != null && Enemy.Deck.CurrentHand.Count > 0)
            enemySkill = Enemy.Deck.CurrentHand[0];
        else if (Enemy.SkillSlots != null && Enemy.SkillSlots.Length > 0)
            enemySkill = Enemy.SkillSlots[0];

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
            $"{Ally.UnitName} → {Enemy.UnitName}  {allySkill.skillName} {GetDamageTypeSymbol(allySkill)}\n" +
            $"{Enemy.UnitName} → {Ally.UnitName}  {enemySkill.skillName} {GetDamageTypeSymbol(enemySkill)}\n" +
            $"충돌: {clashStateColored}";

        OnClashPreviewUpdated?.Invoke(preview);
        OnIntentUpdated?.Invoke(intent);
        OnTargetPreviewUpdated?.Invoke(Ally, Enemy);
    }

    private void ApplyClashDamage(ClashResult clash)
    {
        if (clash.outcome == ClashOutcome.AttackerWin)
        {
            var type = clash.attackerSkill != null ? clash.attackerSkill.damageType : DamageType.Slash;
            int finalDamage = Enemy.TakeDamage(clash.damage, type);
            ApplyHitEffects(Enemy, finalDamage, type);
        }
        else if (clash.outcome == ClashOutcome.DefenderWin)
        {
            var type = clash.defenderSkill != null ? clash.defenderSkill.damageType : DamageType.Slash;
            int finalDamage = Ally.TakeDamage(clash.damage, type);
            ApplyHitEffects(Ally, finalDamage, type);
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

        bool isAlly = allyUnits.Contains(target);
        if (isAlly)
        {
            int idx = allyUnits.IndexOf(target);
            if (idx >= 0 && idx < allyHPBars.Count && allyHPBars[idx] != null) allyHPBars[idx].OnHit();
            if (allyFlash != null) allyFlash.Flash();
        }
        else
        {
            int idx = enemyUnits.IndexOf(target);
            if (idx >= 0 && idx < enemyHPBars.Count && enemyHPBars[idx] != null) enemyHPBars[idx].OnHit();
            if (enemyFlash != null) enemyFlash.Flash();
        }
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
        bool allAlliesDead = allyUnits.TrueForAll(u => !u.IsAlive);
        bool allEnemiesDead = enemyUnits.TrueForAll(u => !u.IsAlive);

        if (allAlliesDead || allEnemiesDead)
        {
            _fsm.TransitionTo(BattleState.BattleEnd);
            OnStateChanged?.Invoke(_fsm.Current);
            if (allAlliesDead && allEnemiesDead) Log("[무승부]");
            else if (allEnemiesDead) Log("[승리!]");
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
        if (Enemy.Deck != null && Enemy.Deck.CurrentHand.Count > 0)
        {
            var hand = Enemy.Deck.CurrentHand;
            int index = Random.Range(0, hand.Count);
            return Enemy.Deck.UseCard(index);
        }
        var slots = Enemy.SkillSlots;
        if (slots == null || slots.Length == 0) return null;
        return slots[Random.Range(0, slots.Length)];
    }

    private Unit GetRandomAliveEnemy()
    {
        var alive = enemyUnits.FindAll(u => u.IsAlive);
        return alive.Count > 0 ? alive[Random.Range(0, alive.Count)] : null;
    }

    private Unit GetRandomAliveAlly()
    {
        var alive = allyUnits.FindAll(u => u.IsAlive);
        return alive.Count > 0 ? alive[Random.Range(0, alive.Count)] : null;
    }

    private SkillData PickEnemySkillFor(Unit enemy)
    {
        if (enemy.Deck != null && enemy.Deck.CurrentHand.Count > 0)
        {
            var hand = enemy.Deck.CurrentHand;
            return enemy.Deck.UseCard(Random.Range(0, hand.Count));
        }
        var slots = enemy.SkillSlots;
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
            case DamageType.Slash: return "참";
            case DamageType.Pierce: return "관";
            case DamageType.Blunt: return "타";
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
