using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 개별 스킬 카드 UI.
/// 스킬 데이터를 받아서 카드 형태로 표시.
/// </summary>
public class SkillCardUI : MonoBehaviour
{
    [Header("카드 요소")]
    [SerializeField] private Image cardBackground;
    [SerializeField] private Image cardBorder;
    [SerializeField] private Image artworkImage;
    [SerializeField] private TextMeshProUGUI skillNameText;
    [SerializeField] private TextMeshProUGUI powerText;
    [SerializeField] private TextMeshProUGUI coinInfoText;
    [SerializeField] private TextMeshProUGUI typeText;
    [SerializeField] private TextMeshProUGUI damageTypeText;
    [SerializeField] private Button cardButton;

    [Header("색상")]
    [SerializeField] private Color attackColor = new Color(0.6f, 0.15f, 0.15f, 1f);
    [SerializeField] private Color defenseColor = new Color(0.15f, 0.4f, 0.6f, 1f);
    [SerializeField] private Color evadeColor = new Color(0.15f, 0.5f, 0.3f, 1f);
    [SerializeField] private Color selectedBorderColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color normalBorderColor = new Color(0.3f, 0.3f, 0.35f, 1f);

    private SkillData _skillData;
    private bool _isSelected;

    public void Setup(SkillData skill)
    {
        _skillData = skill;
        if (skill == null) return;

        if (skillNameText != null) skillNameText.text = skill.skillName;
        if (powerText != null) powerText.text = skill.basePower.ToString();
        if (coinInfoText != null) coinInfoText.text = $"x{skill.coinCount} (+{skill.coinPower})";
        if (artworkImage != null)
        {
            artworkImage.sprite = skill.cardArtwork;
            artworkImage.enabled = skill.cardArtwork != null;
        }

        // 행동 타입 표시
        if (typeText != null)
        {
            switch (skill.skillType)
            {
                case SkillType.Attack:  typeText.text = "공격"; break;
                case SkillType.Defense: typeText.text = "방어"; break;
                case SkillType.Evade:   typeText.text = "회피"; break;
            }
        }

        // 피해 타입 표시
        if (damageTypeText != null)
        {
            switch (skill.damageType)
            {
                case DamageType.Slash:  damageTypeText.text = "참격"; break;
                case DamageType.Pierce: damageTypeText.text = "관통"; break;
                case DamageType.Blunt:  damageTypeText.text = "타격"; break;
            }
        }

        // 타입별 색상
        if (cardBackground != null)
        {
            switch (skill.skillType)
            {
                case SkillType.Attack:  cardBackground.color = attackColor; break;
                case SkillType.Defense: cardBackground.color = defenseColor; break;
                case SkillType.Evade:   cardBackground.color = evadeColor; break;
            }
        }

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        if (cardBorder != null)
            cardBorder.color = selected ? selectedBorderColor : normalBorderColor;
    }

    public void SetInteractable(bool interactable)
    {
        if (cardButton != null) cardButton.interactable = interactable;
    }
}
