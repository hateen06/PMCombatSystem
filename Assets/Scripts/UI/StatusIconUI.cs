using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 유닛의 상태이상을 아이콘으로 표시.
/// 출혈: 빨간, 마비: 노란 배지 + 스택 수
/// </summary>
public class StatusIconUI : MonoBehaviour
{
    [SerializeField] private Unit targetUnit;
    [SerializeField] private GameObject bleedIcon;
    [SerializeField] private TextMeshProUGUI bleedCountText;
    [SerializeField] private GameObject paralysisIcon;
    [SerializeField] private TextMeshProUGUI paralysisCountText;

    private void Update()
    {
        if (targetUnit == null) return;

        // 출혈
        var bleed = targetUnit.GetStatus(StatusType.Bleed);
        bool hasBleed = bleed != null && !bleed.IsExpired;
        if (bleedIcon != null) bleedIcon.SetActive(hasBleed);
        if (bleedCountText != null && hasBleed)
            bleedCountText.text = $"{bleed.potency}x{bleed.count}";

        // 마비
        var para = targetUnit.GetStatus(StatusType.Paralysis);
        bool hasPara = para != null && !para.IsExpired;
        if (paralysisIcon != null) paralysisIcon.SetActive(hasPara);
        if (paralysisCountText != null && hasPara)
            paralysisCountText.text = $"{para.potency}x{para.count}";
    }
}
