using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전투 검증용 런타임 디버그 패널.
/// HP / SP / 상태이상 / 흐트러짐을 빠르게 재현하는 용도.
/// 포트폴리오에서 "검증 가능한 시스템"이라는 신호를 주기 위한 개발자 도구.
/// </summary>
public class BattleDebugPanel : MonoBehaviour
{
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private GameObject root;
    [SerializeField] private TextMeshProUGUI summaryText;

    private void Update()
    {
        RefreshSummary();
    }

    private void RefreshSummary()
    {
        if (summaryText == null || battleManager == null) return;

        var ally = battleManager.Ally;
        var enemy = battleManager.Enemy;
        if (ally == null || enemy == null) return;

        summaryText.text =
            $"[DEBUG]\n" +
            $"ALLY HP {ally.CurrentHP}/{ally.MaxHP} | SP {ally.SP} | STG {(ally.IsStaggered ? "Y" : "N")}\n" +
            $"ENEMY HP {enemy.CurrentHP}/{enemy.MaxHP} | SP {enemy.SP} | STG {(enemy.IsStaggered ? "Y" : "N")}";
    }

    public void AllyDamage10() => battleManager?.Ally?.TakeDamage(10, DamageType.Slash);
    public void EnemyDamage10() => battleManager?.Enemy?.TakeDamage(10, DamageType.Slash);

    public void AllySPPlus10() => battleManager?.Ally?.ChangeSP(10);
    public void AllySPMinus10() => battleManager?.Ally?.ChangeSP(-10);
    public void EnemySPPlus10() => battleManager?.Enemy?.ChangeSP(10);
    public void EnemySPMinus10() => battleManager?.Enemy?.ChangeSP(-10);

    public void AllyBleed() => battleManager?.Ally?.AddStatus(StatusType.Bleed, 3, 2);
    public void EnemyBleed() => battleManager?.Enemy?.AddStatus(StatusType.Bleed, 3, 2);
    public void AllyParalysis() => battleManager?.Ally?.AddStatus(StatusType.Paralysis, 2, 1);
    public void EnemyParalysis() => battleManager?.Enemy?.AddStatus(StatusType.Paralysis, 2, 1);

    public void ForceEnemyStagger()
    {
        var enemy = battleManager?.Enemy;
        if (enemy == null) return;
        enemy.TakeDamage(Mathf.CeilToInt(enemy.CurrentHP * 0.6f), DamageType.Blunt);
    }

    public void ForceAllyStagger()
    {
        var ally = battleManager?.Ally;
        if (ally == null) return;
        ally.TakeDamage(Mathf.CeilToInt(ally.CurrentHP * 0.6f), DamageType.Blunt);
    }

    public void RestartBattle()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}
