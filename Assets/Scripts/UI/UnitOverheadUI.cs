using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitOverheadUI : MonoBehaviour
{
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.7f, 0f);

    private Unit _unit;
    private Canvas _canvas;
    private RectTransform _root;
    private Image _panelBg;
    private TMP_Text _nameText;
    private Image _hpFill;
    private Image _spFill;
    private TMP_Text _hpText;
    private TMP_Text _spText;

    public void Bind(Unit unit)
    {
        if (_unit != null)
        {
            _unit.OnHPChanged -= HandleHPChanged;
            _unit.OnSPChanged -= HandleSPChanged;
        }

        _unit = unit;
        EnsureUI();

        if (_unit != null)
        {
            _unit.OnHPChanged += HandleHPChanged;
            _unit.OnSPChanged += HandleSPChanged;
        }

        Refresh();
    }

    private void LateUpdate()
    {
        if (_unit == null || _root == null) return;
        transform.position = _unit.transform.position + worldOffset;
    }

    private void HandleHPChanged(int current, int max) => Refresh();
    private void HandleSPChanged(int sp) => Refresh();

    private void Refresh()
    {
        if (_unit == null) return;
        EnsureUI();

        transform.position = _unit.transform.position + worldOffset;

        if (_nameText != null)
            _nameText.text = $"{_unit.UnitName} Lv.{_unit.Level}";
        if (_hpFill != null)
            _hpFill.fillAmount = Mathf.Clamp01(_unit.HPRatio);
        if (_spFill != null)
            _spFill.fillAmount = Mathf.InverseLerp(-45f, 45f, _unit.SP);
        if (_hpText != null)
            _hpText.text = $"HP {_unit.CurrentHP}/{_unit.MaxHP}";
        if (_spText != null)
            _spText.text = $"SP {_unit.SP}";

        if (_hpFill != null)
            _hpFill.color = _unit.IsStaggered ? new Color(1f, 0.68f, 0.34f, 1f) : new Color(0.9f, 0.16f, 0.16f, 1f);
        if (_spFill != null)
            _spFill.color = _unit.SP >= 0 ? new Color(0.32f, 0.68f, 1f, 1f) : new Color(0.98f, 0.42f, 0.42f, 1f);
    }

    private void EnsureUI()
    {
        if (_canvas != null) return;

        _canvas = gameObject.GetComponent<Canvas>();
        if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.overrideSorting = true;
        _canvas.sortingOrder = 300;
        if (gameObject.GetComponent<CanvasScaler>() == null) gameObject.AddComponent<CanvasScaler>();
        if (gameObject.GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

        _root = gameObject.GetComponent<RectTransform>();
        _root.sizeDelta = new Vector2(260f, 108f);
        transform.localScale = Vector3.one * 0.013f;

        var bgGo = new GameObject("PanelBG", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bgGo.transform.SetParent(transform, false);
        var bgRt = bgGo.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0.5f, 0.5f);
        bgRt.anchorMax = new Vector2(0.5f, 0.5f);
        bgRt.pivot = new Vector2(0.5f, 0.5f);
        bgRt.anchoredPosition = new Vector2(0f, 2f);
        bgRt.sizeDelta = new Vector2(190f, 82f);
        _panelBg = bgGo.GetComponent<Image>();
        _panelBg.color = new Color(0.03f, 0.03f, 0.05f, 0.72f);

        _nameText = CreateText("NameText", new Vector2(0f, 40f), new Vector2(180f, 20f), 11f, new Color(1f, 0.93f, 0.82f, 1f));
        CreateBar("HPBarBG", new Vector2(0f, 16f), new Vector2(168f, 18f), new Color(0.03f, 0.03f, 0.04f, 0.96f), out _, out _hpFill);
        CreateBar("SPBarBG", new Vector2(0f, -10f), new Vector2(168f, 13f), new Color(0.03f, 0.03f, 0.04f, 0.96f), out _, out _spFill);
        _hpText = CreateText("HPText", new Vector2(0f, 18f), new Vector2(188f, 20f), 13f, new Color(1f, 0.95f, 0.95f, 1f));
        _spText = CreateText("SPText", new Vector2(0f, -28f), new Vector2(152f, 20f), 11f, new Color(0.78f, 0.9f, 1f, 1f));
    }

    private void CreateBar(string name, Vector2 pos, Vector2 size, Color bgColor, out Image bg, out Image fill)
    {
        var bgGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bgGo.transform.SetParent(transform, false);
        var bgRt = bgGo.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0.5f, 0.5f);
        bgRt.anchorMax = new Vector2(0.5f, 0.5f);
        bgRt.pivot = new Vector2(0.5f, 0.5f);
        bgRt.anchoredPosition = pos;
        bgRt.sizeDelta = size;
        bg = bgGo.GetComponent<Image>();
        bg.color = bgColor;

        var fillGo = new GameObject(name + "Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillGo.transform.SetParent(bgGo.transform, false);
        var fillRt = fillGo.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(1f, 1f);
        fillRt.offsetMin = new Vector2(2f, 2f);
        fillRt.offsetMax = new Vector2(-2f, -2f);
        fill = fillGo.GetComponent<Image>();
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
    }

    private TMP_Text CreateText(string name, Vector2 pos, Vector2 size, float fontSize, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.outlineWidth = 0.24f;
        text.outlineColor = new Color32(0, 0, 0, 240);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.fontStyle = FontStyles.Bold;
        return text;
    }
}
