using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpeedTimelineUI : MonoBehaviour
{
    [SerializeField] private RectTransform container;
    [SerializeField] private float slotWidth = 120f;
    [SerializeField] private float slotSpacing = 8f;

    private readonly List<TimelineSlot> _slots = new();
    private Canvas _canvas;

    private class TimelineSlot
    {
        public Unit unit;
        public int speed;
        public bool isEnemy;
        public GameObject root;
        public Image bg;
        public TMP_Text nameText;
        public TMP_Text speedText;
        public Image skillIcon;
        public SkillData assignedSkill;
    }

    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        if (container == null) container = transform as RectTransform;
    }

    public void Clear()
    {
        foreach (var s in _slots)
            if (s.root != null) Destroy(s.root);
        _slots.Clear();
    }

    public void AddUnit(Unit unit, int speed, bool isEnemy, SkillData skill = null)
    {
        var existing = _slots.Find(s => s.unit == unit);
        if (existing != null)
        {
            existing.speed = speed;
            existing.assignedSkill = skill;
            RefreshSlotVisual(existing);
            SortAndLayout();
            return;
        }

        var slot = new TimelineSlot { unit = unit, speed = speed, isEnemy = isEnemy, assignedSkill = skill };
        CreateSlotObject(slot);
        _slots.Add(slot);
        SortAndLayout();
    }

    private void CreateSlotObject(TimelineSlot slot)
    {
        var go = new GameObject(slot.unit.UnitName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(container, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(slotWidth, 56f);

        slot.root = go;
        slot.bg = go.GetComponent<Image>();

        var nameGo = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameGo.transform.SetParent(go.transform, false);
        var nameRt = nameGo.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0f, 0.5f);
        nameRt.anchorMax = new Vector2(0.6f, 1f);
        nameRt.offsetMin = new Vector2(6f, 0f);
        nameRt.offsetMax = new Vector2(0f, -2f);
        slot.nameText = nameGo.GetComponent<TMP_Text>();
        slot.nameText.fontSize = 14;
        slot.nameText.alignment = TextAlignmentOptions.BottomLeft;

        var spdGo = new GameObject("Speed", typeof(RectTransform), typeof(TextMeshProUGUI));
        spdGo.transform.SetParent(go.transform, false);
        var spdRt = spdGo.GetComponent<RectTransform>();
        spdRt.anchorMin = new Vector2(0.6f, 0f);
        spdRt.anchorMax = new Vector2(1f, 1f);
        spdRt.offsetMin = Vector2.zero;
        spdRt.offsetMax = new Vector2(-4f, 0f);
        slot.speedText = spdGo.GetComponent<TMP_Text>();
        slot.speedText.fontSize = 26;
        slot.speedText.fontStyle = FontStyles.Bold;
        slot.speedText.alignment = TextAlignmentOptions.Center;

        var iconGo = new GameObject("SkillIcon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(go.transform, false);
        var iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0f, 0f);
        iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.offsetMin = new Vector2(6f, 2f);
        iconRt.offsetMax = new Vector2(-2f, 0f);
        slot.skillIcon = iconGo.GetComponent<Image>();
        slot.skillIcon.preserveAspect = true;

        RefreshSlotVisual(slot);
    }

    private void RefreshSlotVisual(TimelineSlot slot)
    {
        if (slot.root == null) return;

        slot.bg.color = slot.isEnemy
            ? new Color(0.45f, 0.18f, 0.18f, 0.92f)
            : new Color(0.18f, 0.22f, 0.35f, 0.92f);

        if (slot.nameText != null)
        {
            slot.nameText.text = slot.unit != null ? slot.unit.UnitName : "";
            slot.nameText.color = Color.white;
        }

        if (slot.speedText != null)
        {
            slot.speedText.text = slot.speed.ToString();
            slot.speedText.color = new Color(1f, 0.9f, 0.4f);
        }

        if (slot.skillIcon != null)
        {
            if (slot.assignedSkill != null && slot.assignedSkill.cardArtwork != null)
            {
                slot.skillIcon.sprite = slot.assignedSkill.cardArtwork;
                slot.skillIcon.color = Color.white;
            }
            else
            {
                slot.skillIcon.color = Color.clear;
            }
        }
    }

    private void SortAndLayout()
    {
        _slots.Sort((a, b) => b.speed.CompareTo(a.speed));

        float totalWidth = _slots.Count * slotWidth + Mathf.Max(0, _slots.Count - 1) * slotSpacing;
        float startX = -totalWidth * 0.5f + slotWidth * 0.5f;

        for (int i = 0; i < _slots.Count; i++)
        {
            if (_slots[i].root == null) continue;
            var rt = _slots[i].root.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(startX + i * (slotWidth + slotSpacing), 0f);
        }
    }

    public void HighlightClashPair(Unit a, Unit b)
    {
        foreach (var s in _slots)
        {
            if (s.root == null || s.bg == null) continue;
            if (s.unit == a || s.unit == b)
                s.bg.color = new Color(0.7f, 0.2f, 0.2f, 0.95f);
        }
    }
}
