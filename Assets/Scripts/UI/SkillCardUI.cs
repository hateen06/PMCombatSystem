using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
public class SkillCardUI : MonoBehaviour, IPointerClickHandler
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
    [SerializeField] private TextMeshProUGUI powerRangeText;
    [SerializeField] private Button cardButton;

    [Header("색상")]
    [SerializeField] private Color attackColor = new Color(0.6f, 0.15f, 0.15f, 1f);
    [SerializeField] private Color defenseColor = new Color(0.15f, 0.4f, 0.6f, 1f);
    [SerializeField] private Color evadeColor = new Color(0.15f, 0.5f, 0.3f, 1f);
    [SerializeField] private Color selectedBorderColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color normalBorderColor = new Color(0.3f, 0.3f, 0.35f, 1f);

    private SkillData _skillData;
    private bool _isSelected;
    public System.Action OnRightClicked;

    public void Setup(SkillData skill)
    {
        _skillData = skill;
        if (skill == null) return;

        if (skillNameText != null) skillNameText.text = skill.skillName;
        if (powerText != null) powerText.text = skill.basePower.ToString();
        if (coinInfoText != null) coinInfoText.text = $"x{skill.coinCount} (+{skill.coinPower})";

        if (powerRangeText != null)
        {
            int min = skill.basePower;
            int max = skill.basePower + skill.coinCount * skill.coinPower;
            powerRangeText.text = $"{min}~{max}";
        }
        if (artworkImage != null)
        {
            artworkImage.sprite = skill.cardArtwork;
            artworkImage.enabled = skill.cardArtwork != null;
        }

        // 행동 타입 표시 - 긴 텍스트 대신 짧은 심볼 사용
        if (typeText != null)
        {
            switch (skill.skillType)
            {
                case SkillType.Attack:  typeText.text = "공"; break;
                case SkillType.Defense: typeText.text = "■"; break;
                case SkillType.Evade:   typeText.text = "◇"; break;
            }
        }

        // 피해 타입 표시 - 임시 아이콘형 심볼
        if (damageTypeText != null)
        {
            switch (skill.damageType)
            {
                case DamageType.Slash:  damageTypeText.text = "참"; break;
                case DamageType.Pierce: damageTypeText.text = "관"; break;
                case DamageType.Blunt:  damageTypeText.text = "타"; break;
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

        float targetScale = selected ? 1.08f : 1f;
        float targetY = selected ? 18f : 0f;
        float targetAlpha = selected ? 1f : 0.86f;

        transform.localScale = Vector3.one * targetScale;

        var rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            var pos = rt.anchoredPosition;
            pos.y = _baseY + targetY;
            rt.anchoredPosition = pos;
        }

        var cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        cg.alpha = targetAlpha;
    }

    private float _baseY;

    private void Awake()
    {
        var rt = GetComponent<RectTransform>();
        if (rt != null) _baseY = rt.anchoredPosition.y;
    }

    public void SetInteractable(bool interactable)
    {
        if (cardButton != null) cardButton.interactable = interactable;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
            OnRightClicked?.Invoke();
    }
}
