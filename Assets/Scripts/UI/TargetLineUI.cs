using UnityEngine;
using TMPro;
[RequireComponent(typeof(LineRenderer))]
public class TargetLineUI : MonoBehaviour
{
    [SerializeField] private Color normalColor = new Color(1f, 0.85f, 0.25f, 0.95f);
    [SerializeField] private Color clashColor = new Color(1f, 0.35f, 0.35f, 0.98f);
    [SerializeField] private Color interceptColor = new Color(0.65f, 0.45f, 1f, 0.98f);
    [SerializeField] private Color oneSidedColor = new Color(1f, 0.65f, 0.22f, 0.95f);
    [SerializeField] private float normalWidth = 0.06f;
    [SerializeField] private float clashWidth = 0.11f;
    [SerializeField] private Vector3 startOffset = new Vector3(0f, 0.6f, 0f);
    [SerializeField] private Vector3 endOffset = new Vector3(0f, 0.6f, 0f);

    [SerializeField] private GameObject clashSymbolPrefab;

    private LineRenderer _line;
    private Unit _from;
    private Unit _to;
    private bool _isClash;
    private GameObject _clashSymbol;
    private TextMeshPro _annotationText;
    private string _annotation;

    private void Awake()
    {
        _line = GetComponent<LineRenderer>();
        _line.positionCount = 2;
        _line.useWorldSpace = true;
        _line.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        Hide();
    }

    public void SetTargets(Unit from, Unit to, bool isClash, string annotation = "")
    {
        _from = from;
        _to = to;
        _isClash = isClash;
        _annotation = annotation;
        gameObject.SetActive(from != null && to != null);
        ApplyStyle();
        Refresh();
        UpdateClashSymbol();
        UpdateAnnotation();
    }

    public void Hide()
    {
        if (_line != null)
        {
            _line.startWidth = 0f;
            _line.endWidth = 0f;
        }
        if (_annotationText != null)
            _annotationText.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (!gameObject.activeSelf) return;
        Refresh();
    }

    private void ApplyStyle()
    {
        if (_line == null) return;
        Color c = ResolveColor();
        float w = _isClash ? clashWidth : normalWidth;
        _line.startColor = c;
        _line.endColor = c;
        _line.startWidth = w;
        _line.endWidth = w;
    }

    private void Refresh()
    {
        if (_line == null || _from == null || _to == null || !_from.IsAlive || !_to.IsAlive)
        {
            Hide();
            return;
        }
        Vector3 p0 = _from.transform.position + startOffset;
        Vector3 p1 = _to.transform.position + endOffset;
        _line.SetPosition(0, p0);
        _line.SetPosition(1, p1);

        var mid = (p0 + p1) * 0.5f;
        if (_clashSymbol != null)
            _clashSymbol.transform.position = mid;
        if (_annotationText != null)
            _annotationText.transform.position = mid + Vector3.up * 0.4f;
    }

    private Color ResolveColor()
    {
        if (_annotation.Contains("가로채기")) return interceptColor;
        if (_annotation.Contains("일방")) return oneSidedColor;
        if (_isClash) return clashColor;
        return normalColor;
    }

    private void UpdateClashSymbol()
    {
        if (!_isClash) return;
        if (_clashSymbol != null) return;

        if (clashSymbolPrefab != null)
        {
            _clashSymbol = Instantiate(clashSymbolPrefab, transform);
        }
        else
        {
            _clashSymbol = new GameObject("ClashMark");
            _clashSymbol.transform.SetParent(transform, false);
            _clashSymbol.transform.localScale = Vector3.one * 0.4f;

            var sr = _clashSymbol.AddComponent<SpriteRenderer>();
            sr.color = new Color(1f, 0.3f, 0.3f, 0.9f);
            sr.sortingOrder = 10;
        }
    }

    private void UpdateAnnotation()
    {
        if (string.IsNullOrWhiteSpace(_annotation))
        {
            if (_annotationText != null)
                _annotationText.gameObject.SetActive(false);
            return;
        }

        if (_annotationText == null)
        {
            var go = new GameObject("Annotation", typeof(TextMeshPro));
            go.transform.SetParent(transform, false);
            _annotationText = go.GetComponent<TextMeshPro>();
            _annotationText.fontSize = 2.5f;
            _annotationText.alignment = TextAlignmentOptions.Center;
            _annotationText.color = ResolveColor();
            _annotationText.outlineWidth = 0.2f;
            _annotationText.outlineColor = Color.black;
            _annotationText.sortingOrder = 11;
        }

        _annotationText.text = _annotation;
        _annotationText.color = ResolveColor();
        _annotationText.gameObject.SetActive(true);
    }
}
