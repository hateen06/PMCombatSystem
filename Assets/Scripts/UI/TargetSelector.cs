using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// 전투 중 적 유닛 클릭 감지.
/// BattleManager에 붙여서 모든 적 클릭을 한 곳에서 처리.
/// </summary>
public class TargetSelector : MonoBehaviour
{
    private BattleManager _bm;

    private void Start()
    {
        _bm = GetComponent<BattleManager>();
        if (_bm == null) _bm = FindObjectOfType<BattleManager>();
    }

    private void Update()
    {
        if (_bm == null) return;

        // New Input System
        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

        // UI 위를 클릭한 경우 무시
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Vector2 worldPos = Camera.main.ScreenToWorldPoint(mouse.position.ReadValue());
        var hit = Physics2D.Raycast(worldPos, Vector2.zero);

        if (hit.collider == null) return;

        var unit = hit.collider.GetComponent<Unit>();
        if (unit == null || !unit.IsAlive) return;

        _bm.SetTarget(unit);
    }
}
