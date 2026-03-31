using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatusIconUI : MonoBehaviour
{
    [SerializeField] private Unit targetUnit;
    [SerializeField] private GameObject bleedIcon;
    [SerializeField] private TextMeshProUGUI bleedCountText;
    [SerializeField] private GameObject paralysisIcon;
    [SerializeField] private TextMeshProUGUI paralysisCountText;
    [SerializeField] private GameObject burnIcon;
    [SerializeField] private TextMeshProUGUI burnCountText;
    [SerializeField] private GameObject tremorIcon;
    [SerializeField] private TextMeshProUGUI tremorCountText;
    [SerializeField] private GameObject ruptureIcon;
    [SerializeField] private TextMeshProUGUI ruptureCountText;

    private void OnEnable()
    {
        if (targetUnit != null)
            targetUnit.OnStatusChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (targetUnit != null)
            targetUnit.OnStatusChanged -= Refresh;
    }

    private void Refresh()
    {
        if (targetUnit == null) return;

        var bleed = targetUnit.GetStatus(StatusType.Bleed);
        bool hasBleed = bleed != null && !bleed.IsExpired;
        if (bleedIcon != null) bleedIcon.SetActive(hasBleed);
        if (bleedCountText != null && hasBleed)
            bleedCountText.text = $"{bleed.potency}x{bleed.count}";

        var para = targetUnit.GetStatus(StatusType.Paralysis);
        bool hasPara = para != null && !para.IsExpired;
        if (paralysisIcon != null) paralysisIcon.SetActive(hasPara);
        if (paralysisCountText != null && hasPara)
            paralysisCountText.text = $"{para.potency}x{para.count}";

        RefreshStatus(StatusType.Burn, burnIcon, burnCountText);
        RefreshStatus(StatusType.Tremor, tremorIcon, tremorCountText);
        RefreshStatus(StatusType.Rupture, ruptureIcon, ruptureCountText);
    }

    private void RefreshStatus(StatusType type, GameObject icon, TMP_Text text)
    {
        var status = targetUnit.GetStatus(type);
        bool has = status != null && !status.IsExpired;
        if (icon != null) icon.SetActive(has);
        if (text != null && has)
            text.text = $"{status.potency}x{status.count}";
    }
}
