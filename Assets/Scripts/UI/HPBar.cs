using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
public class HPBar : MonoBehaviour
{
    [SerializeField] private RectTransform fillRect;
    [SerializeField] private Image fillImage;
    [SerializeField] private float tweenDuration = 0.4f;

    [SerializeField] private TMP_Text shieldText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private RectTransform spRoot;
    [SerializeField] private Image spFillImage;
    [SerializeField] private TMP_Text spText;

    private Unit _target;
    private Tween _currentTween;
    private readonly System.Collections.Generic.List<RectTransform> _markers = new();

    public void Bind(Unit unit)
    {
        if (_target != null)
        {
            _target.OnHPChanged -= HandleHPChanged;
            _target.OnShieldChanged -= HandleShieldChanged;
            _target.OnSPChanged -= HandleSPChanged;
        }

        _target = unit;

        // fillRect 자동 탐색
        if (fillRect == null && fillImage != null)
            fillRect = fillImage.GetComponent<RectTransform>();
        if (fillRect == null)
        {
            var fill = transform.Find("Fill");
            if (fill != null) fillRect = fill.GetComponent<RectTransform>();
        }

        // 초기 상태: 꽉 참
        if (fillRect != null)
        {
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);
        }

        var rootImage = GetComponent<Image>();
        if (rootImage != null)
            rootImage.color = new Color(0.03f, 0.03f, 0.04f, 0.95f);

        if (fillImage != null)
            fillImage.type = Image.Type.Simple;

        EnsureSPBar();
        CreateStaggerMarkers();

        if (_target != null)
        {
            _target.OnHPChanged += HandleHPChanged;
            _target.OnShieldChanged += HandleShieldChanged;
            _target.OnSPChanged += HandleSPChanged;
        }

        Refresh();
        UpdateSPDisplay();
        UpdateShieldDisplay();
    }
    private void CreateStaggerMarkers()
    {
        foreach (var m in _markers)
            if (m != null) Destroy(m.gameObject);
        _markers.Clear();

        if (_target == null) return;

        float[] thresholds = {
            _target.StaggerThreshold1,
            _target.StaggerThreshold2,
            _target.StaggerThreshold3
        };

        foreach (float t in thresholds)
        {
            if (t <= 0f || t >= 1f) continue;

            var markerGO = new GameObject("StaggerMarker", typeof(RectTransform), typeof(Image));
            markerGO.transform.SetParent(transform, false);

            var rt = markerGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(t, 0f);
            rt.anchorMax = new Vector2(t, 1f);
            rt.sizeDelta = new Vector2(2f, 0f); // 2px 너비
            rt.anchoredPosition = Vector2.zero;

            var img = markerGO.GetComponent<Image>();
            img.color = new Color(1f, 0.85f, 0.2f, 0.8f); // 노란색 반투명

            _markers.Add(rt);
        }
    }

    private void HandleHPChanged(int current, int max) => Refresh();
    private void HandleShieldChanged(int shield) => UpdateShieldDisplay();
    private void HandleSPChanged(int sp) => UpdateSPDisplay();

    private void UpdateShieldDisplay()
    {
        if (shieldText == null)
        {
            var go = new GameObject("ShieldText", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(34f, 10f);
            rt.sizeDelta = new Vector2(64f, 22f);
            shieldText = go.GetComponent<TMPro.TextMeshProUGUI>();
            shieldText.fontSize = 14;
            shieldText.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            shieldText.color = new Color(0.55f, 0.82f, 1f);
            shieldText.outlineWidth = 0.18f;
            shieldText.outlineColor = new Color32(0, 0, 0, 220);
            shieldText.textWrappingMode = TextWrappingModes.NoWrap;
        }

        if (_target == null || _target.Shield <= 0)
        {
            shieldText.gameObject.SetActive(false);
            return;
        }
        shieldText.text = $"SH { _target.Shield }";
        shieldText.gameObject.SetActive(true);
    }
    public void Refresh()
    {
        if (_target == null || fillRect == null) return;

        EnsureHPText();
        if (hpText != null)
            hpText.text = $"HP {_target.CurrentHP}/{_target.MaxHP}";

        float targetRatio = Mathf.Clamp01(_target.HPRatio);

        _currentTween?.Kill();

        _currentTween = DOTween.To(
            () => fillRect.anchorMax.x,
            x => {
                var a = fillRect.anchorMax;
                a.x = x;
                fillRect.anchorMax = a;
            },
            targetRatio,
            tweenDuration
        ).SetEase(Ease.OutQuad);

        if (fillImage != null)
            fillImage.color = _target.IsStaggered ? new Color(1f, 0.65f, 0.32f, 1f) : new Color(0.82f, 0.18f, 0.18f, 1f);

        UpdateSPDisplay();
    }
    private void EnsureHPText()
    {
        if (hpText != null) return;

        var go = new GameObject("HPText", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, 22f);
        rt.sizeDelta = new Vector2(132f, 20f);

        hpText = go.GetComponent<TextMeshProUGUI>();
        hpText.fontSize = 14;
        hpText.alignment = TextAlignmentOptions.Center;
        hpText.color = new Color(1f, 0.95f, 0.95f, 1f);
        hpText.outlineWidth = 0.22f;
        hpText.outlineColor = new Color32(0, 0, 0, 235);
        hpText.textWrappingMode = TextWrappingModes.NoWrap;
        hpText.fontStyle = FontStyles.Bold;
        hpText.text = string.Empty;
    }

    private void EnsureSPBar()
    {
        if (spRoot != null && spFillImage != null && spText != null) return;

        var rootGO = new GameObject("SPBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        rootGO.transform.SetParent(transform, false);
        spRoot = rootGO.GetComponent<RectTransform>();
        spRoot.anchorMin = new Vector2(0.5f, 1f);
        spRoot.anchorMax = new Vector2(0.5f, 1f);
        spRoot.pivot = new Vector2(0.5f, 0.5f);
        spRoot.anchoredPosition = new Vector2(0f, -10f);
        spRoot.sizeDelta = new Vector2(112f, 8f);

        var bg = rootGO.GetComponent<Image>();
        bg.color = new Color(0.06f, 0.08f, 0.11f, 0.92f);
        bg.raycastTarget = false;

        var fillGO = new GameObject("SPFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillGO.transform.SetParent(spRoot, false);
        var fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = new Vector2(0f, 0f);
        fillRT.anchorMax = new Vector2(1f, 1f);
        fillRT.offsetMin = new Vector2(1f, 1f);
        fillRT.offsetMax = new Vector2(-1f, -1f);
        spFillImage = fillGO.GetComponent<Image>();
        spFillImage.color = new Color(0.28f, 0.62f, 1f, 1f);
        spFillImage.raycastTarget = false;

        var textGO = new GameObject("SPText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(transform, false);
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0.5f, 1f);
        textRT.anchorMax = new Vector2(0.5f, 1f);
        textRT.pivot = new Vector2(0.5f, 0.5f);
        textRT.anchoredPosition = new Vector2(0f, -28f);
        textRT.sizeDelta = new Vector2(128f, 22f);
        spText = textGO.GetComponent<TextMeshProUGUI>();
        spText.fontSize = 13;
        spText.alignment = TextAlignmentOptions.Center;
        spText.color = new Color(0.78f, 0.9f, 1f, 1f);
        spText.outlineWidth = 0.22f;
        spText.outlineColor = new Color32(0, 0, 0, 235);
        spText.textWrappingMode = TextWrappingModes.NoWrap;
        spText.fontStyle = FontStyles.Bold;
    }

    private void UpdateSPDisplay()
    {
        EnsureSPBar();
        if (_target == null || spRoot == null || spFillImage == null || spText == null) return;

        float ratio = Mathf.InverseLerp(-45f, 45f, _target.SP);
        var fillRT = spFillImage.rectTransform;
        var anchorMax = fillRT.anchorMax;
        anchorMax.x = ratio;
        fillRT.anchorMax = anchorMax;
        spFillImage.color = _target.SP >= 0 ? new Color(0.28f, 0.62f, 1f, 1f) : new Color(0.95f, 0.38f, 0.38f, 1f);
        spText.text = $"SP {_target.SP}";
        spText.color = _target.SP >= 0 ? new Color(0.75f, 0.88f, 1f, 1f) : new Color(1f, 0.7f, 0.7f, 1f);
    }

    public void OnHit()
    {
        Refresh();
        transform.DOShakePosition(0.2f, 5f, 20)
            .SetEase(Ease.OutQuad);
    }
}
