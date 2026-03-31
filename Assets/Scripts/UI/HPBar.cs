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

    private Unit _target;
    private Tween _currentTween;
    private readonly System.Collections.Generic.List<RectTransform> _markers = new();

    public void Bind(Unit unit)
    {
        if (_target != null)
        {
            _target.OnHPChanged -= HandleHPChanged;
            _target.OnShieldChanged -= HandleShieldChanged;
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

        // fillImage는 Simple 타입으로 (Filled 안 씀)
        if (fillImage != null)
            fillImage.type = Image.Type.Simple;

        // 흐트러짐 구간 마커 생성
        CreateStaggerMarkers();

        if (_target != null)
        {
            _target.OnHPChanged += HandleHPChanged;
            _target.OnShieldChanged += HandleShieldChanged;
        }

        Refresh();
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

    private void UpdateShieldDisplay()
    {
        if (shieldText == null)
        {
            var go = new GameObject("ShieldText", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(35f, 0f);
            rt.sizeDelta = new Vector2(60f, 24f);
            shieldText = go.GetComponent<TMPro.TextMeshProUGUI>();
            shieldText.fontSize = 16;
            shieldText.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            shieldText.color = new Color(0.3f, 0.7f, 1f);
        }

        if (_target == null || _target.Shield <= 0)
        {
            shieldText.gameObject.SetActive(false);
            return;
        }
        shieldText.text = $"🛡{_target.Shield}";
        shieldText.gameObject.SetActive(true);
    }
    public void Refresh()
    {
        if (_target == null || fillRect == null) return;

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
    }
    public void OnHit()
    {
        Refresh();
        transform.DOShakePosition(0.2f, 5f, 20)
            .SetEase(Ease.OutQuad);
    }
}
