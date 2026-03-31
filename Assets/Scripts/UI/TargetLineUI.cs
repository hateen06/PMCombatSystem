using UnityEngine;
[RequireComponent(typeof(LineRenderer))]
public class TargetLineUI : MonoBehaviour
{
    [SerializeField] private Color normalColor = new Color(1f, 0.85f, 0.25f, 0.95f);
    [SerializeField] private Color clashColor = new Color(1f, 0.35f, 0.35f, 0.98f);
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

    private void Awake()
    {
        _line = GetComponent<LineRenderer>();
        _line.positionCount = 2;
        _line.useWorldSpace = true;
        _line.material = new Material(Shader.Find("Sprites/Default"));
        Hide();
    }

    public void SetTargets(Unit from, Unit to, bool isClash)
    {
        _from = from;
        _to = to;
        _isClash = isClash;
        gameObject.SetActive(from != null && to != null);
        ApplyStyle();
        Refresh();
        UpdateClashSymbol();
    }

    public void Hide()
    {
        if (_line != null)
        {
            _line.startWidth = 0f;
            _line.endWidth = 0f;
        }
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
        Color c = _isClash ? clashColor : normalColor;
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

        if (_clashSymbol != null)
            _clashSymbol.transform.position = (p0 + p1) * 0.5f;
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
}
