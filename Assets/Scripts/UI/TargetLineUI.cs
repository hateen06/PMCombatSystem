using UnityEngine;

/// <summary>
/// 유닛 간 타겟 연결선.
/// 월드 좌표 기준 LineRenderer 사용.
/// 합/일방에 따라 색과 두께를 바꾼다.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class TargetLineUI : MonoBehaviour
{
    [SerializeField] private Color normalColor = new Color(1f, 0.85f, 0.25f, 0.95f);
    [SerializeField] private Color clashColor = new Color(1f, 0.35f, 0.35f, 0.98f);
    [SerializeField] private float normalWidth = 0.06f;
    [SerializeField] private float clashWidth = 0.11f;
    [SerializeField] private Vector3 startOffset = new Vector3(0f, 0.6f, 0f);
    [SerializeField] private Vector3 endOffset = new Vector3(0f, 0.6f, 0f);

    private LineRenderer _line;
    private Unit _from;
    private Unit _to;
    private bool _isClash;

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
        _line.SetPosition(0, _from.transform.position + startOffset);
        _line.SetPosition(1, _to.transform.position + endOffset);
    }
}
