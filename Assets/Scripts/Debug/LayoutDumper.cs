using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

public class LayoutDumper : MonoBehaviour
{
    [SerializeField] private Camera worldCamera;
    [SerializeField] private bool includeInactive;
    [SerializeField] private string outputFileName = "layout-dump.json";
    [SerializeField] private KeyCode dumpKey = KeyCode.F8;
    [SerializeField] private KeyCode overlayKey = KeyCode.F9;

    public LayoutDumpSnapshot LastSnapshot { get; private set; }

    private LayoutDumpOverlay _overlay;

    private void Awake()
    {
        if (worldCamera == null) worldCamera = Camera.main;
        _overlay = GetComponent<LayoutDumpOverlay>();
        if (_overlay == null) _overlay = gameObject.AddComponent<LayoutDumpOverlay>();
    }

    private void Update()
    {
        if (WasPressedThisFrame(dumpKey)) Dump();
        if (WasPressedThisFrame(overlayKey) && _overlay != null) _overlay.Toggle();
    }

    public void Dump()
    {
        if (worldCamera == null) worldCamera = Camera.main;

        var snapshot = new LayoutDumpSnapshot
        {
            screenWidth = Screen.width,
            screenHeight = Screen.height
        };

        CollectUi(snapshot.items);
        CollectWorld(snapshot.items);
        Validate(snapshot);
        LastSnapshot = snapshot;

        var json = JsonUtility.ToJson(snapshot, true);
        Debug.Log(json);
        Debug.Log(ToSummary(snapshot));
        Save(json);
    }

    private void CollectUi(List<LayoutDumpItem> items)
    {
        var rects = FindObjectsByType<RectTransform>(FindObjectsSortMode.None);
        var screenArea = Mathf.Max(1f, Screen.width * Screen.height);

        for (int i = 0; i < rects.Length; i++)
        {
            var rt = rects[i];
            if (rt == null) continue;
            if (!includeInactive && !rt.gameObject.activeInHierarchy) continue;
            if (rt.GetComponent<Canvas>() != null) continue;

            var rect = GetUiScreenRect(rt);
            items.Add(new LayoutDumpItem
            {
                name = rt.name,
                path = GetPath(rt.transform),
                kind = "UI",
                active = rt.gameObject.activeInHierarchy,
                behindCamera = false,
                visibleCornerCount = 4,
                screenCoveragePercent = rect.width * rect.height / screenArea,
                overlapSeverity = 0f,
                visibility = Classify(rect, false, 4),
                screenRect = rect
            });
        }
    }

    private void CollectWorld(List<LayoutDumpItem> items)
    {
        var renderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
        var screenArea = Mathf.Max(1f, Screen.width * Screen.height);

        for (int i = 0; i < renderers.Length; i++)
        {
            var sr = renderers[i];
            if (sr == null) continue;
            if (!includeInactive && !sr.gameObject.activeInHierarchy) continue;
            if (worldCamera == null) continue;

            var bounds = sr.bounds;
            var corners = new[]
            {
                new Vector3(bounds.min.x, bounds.min.y, bounds.min.z),
                new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),
                new Vector3(bounds.min.x, bounds.max.y, bounds.min.z),
                new Vector3(bounds.min.x, bounds.max.y, bounds.max.z),
                new Vector3(bounds.max.x, bounds.min.y, bounds.min.z),
                new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),
                new Vector3(bounds.max.x, bounds.max.y, bounds.min.z),
                new Vector3(bounds.max.x, bounds.max.y, bounds.max.z)
            };

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            int visibleCornerCount = 0;
            bool behind = true;

            for (int c = 0; c < corners.Length; c++)
            {
                var screen = worldCamera.WorldToScreenPoint(corners[c]);
                if (screen.z > 0f)
                {
                    behind = false;
                    visibleCornerCount++;
                }
                if (screen.x < minX) minX = screen.x;
                if (screen.y < minY) minY = screen.y;
                if (screen.x > maxX) maxX = screen.x;
                if (screen.y > maxY) maxY = screen.y;
            }

            var rect = Rect.MinMaxRect(minX, minY, maxX, maxY);
            items.Add(new LayoutDumpItem
            {
                name = sr.name,
                path = GetPath(sr.transform),
                kind = "World",
                active = sr.gameObject.activeInHierarchy,
                behindCamera = behind,
                visibleCornerCount = visibleCornerCount,
                screenCoveragePercent = rect.width * rect.height / screenArea,
                overlapSeverity = 0f,
                visibility = Classify(rect, behind, visibleCornerCount),
                screenRect = rect
            });
        }
    }

    private void Validate(LayoutDumpSnapshot snapshot)
    {
        var screen = new Rect(0f, 0f, snapshot.screenWidth, snapshot.screenHeight);

        for (int i = 0; i < snapshot.items.Count; i++)
        {
            var item = snapshot.items[i];

            if (item.behindCamera)
            {
                snapshot.issues.Add(new LayoutDumpIssue
                {
                    type = "BehindCamera",
                    a = item.name,
                    message = item.name + " is behind camera",
                    severity = 1f
                });
                continue;
            }

            if (!screen.Overlaps(item.screenRect))
            {
                snapshot.issues.Add(new LayoutDumpIssue
                {
                    type = "OffScreen",
                    a = item.name,
                    message = item.name + " is outside screen",
                    severity = 1f
                });
            }
        }

        for (int i = 0; i < snapshot.items.Count; i++)
        {
            for (int j = i + 1; j < snapshot.items.Count; j++)
            {
                var a = snapshot.items[i];
                var b = snapshot.items[j];
                if (!a.active || !b.active) continue;
                if (!a.screenRect.Overlaps(b.screenRect)) continue;
                if (a.kind == "World" && b.kind == "World") continue;

                float overlap = GetOverlapArea(a.screenRect, b.screenRect);
                float baseArea = Mathf.Min(a.screenRect.width * a.screenRect.height, b.screenRect.width * b.screenRect.height);
                float severity = baseArea > 0f ? overlap / baseArea : 0f;
                a.overlapSeverity = Mathf.Max(a.overlapSeverity, severity);
                b.overlapSeverity = Mathf.Max(b.overlapSeverity, severity);

                snapshot.issues.Add(new LayoutDumpIssue
                {
                    type = "Overlap",
                    a = a.name,
                    b = b.name,
                    message = a.name + " overlaps " + b.name,
                    severity = severity
                });
            }
        }
    }

    private Rect GetUiScreenRect(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);

        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;

        for (int i = 0; i < 4; i++)
        {
            var screen = RectTransformUtility.WorldToScreenPoint(null, corners[i]);
            if (screen.x < minX) minX = screen.x;
            if (screen.y < minY) minY = screen.y;
            if (screen.x > maxX) maxX = screen.x;
            if (screen.y > maxY) maxY = screen.y;
        }

        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    private string Classify(Rect rect, bool behindCamera, int visibleCornerCount)
    {
        if (behindCamera) return "Behind";
        var screen = new Rect(0f, 0f, Screen.width, Screen.height);
        if (!screen.Overlaps(rect)) return "OffScreen";
        if (visibleCornerCount < 8 && visibleCornerCount > 0) return "Partial";
        return "Visible";
    }

    private float GetOverlapArea(Rect a, Rect b)
    {
        float xMin = Mathf.Max(a.xMin, b.xMin);
        float yMin = Mathf.Max(a.yMin, b.yMin);
        float xMax = Mathf.Min(a.xMax, b.xMax);
        float yMax = Mathf.Min(a.yMax, b.yMax);
        if (xMax <= xMin || yMax <= yMin) return 0f;
        return (xMax - xMin) * (yMax - yMin);
    }

    private void Save(string json)
    {
        try
        {
            var path = Path.Combine(Application.dataPath, "..", outputFileName);
            File.WriteAllText(path, json, Encoding.UTF8);
            Debug.Log("[LayoutDumper] saved: " + path);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[LayoutDumper] save failed: " + e.Message);
        }
    }

    private string ToSummary(LayoutDumpSnapshot snapshot)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[LayoutDumper]");
        sb.AppendLine("items=" + snapshot.items.Count + ", issues=" + snapshot.issues.Count);
        for (int i = 0; i < snapshot.issues.Count; i++)
        {
            var issue = snapshot.issues[i];
            sb.AppendLine("- " + issue.type + " (" + issue.severity.ToString("0.00") + "): " + issue.message);
        }
        return sb.ToString();
    }

    private bool WasPressedThisFrame(KeyCode keyCode)
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return false;

        return keyCode switch
        {
            KeyCode.F8 => keyboard.f8Key.wasPressedThisFrame,
            KeyCode.F9 => keyboard.f9Key.wasPressedThisFrame,
            _ => false,
        };
    }

    private string GetPath(Transform target)
    {
        if (target == null) return string.Empty;
        var parts = new List<string>();
        var current = target;
        while (current != null)
        {
            parts.Add(current.name);
            current = current.parent;
        }
        parts.Reverse();
        return string.Join("/", parts);
    }
}
