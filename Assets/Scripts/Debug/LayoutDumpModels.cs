using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LayoutDumpItem
{
    public string name;
    public string path;
    public string kind;
    public bool active;
    public bool behindCamera;
    public int visibleCornerCount;
    public float screenCoveragePercent;
    public float overlapSeverity;
    public string visibility;
    public Rect screenRect;
}

[Serializable]
public class LayoutDumpIssue
{
    public string type;
    public string a;
    public string b;
    public string message;
    public float severity;
}

[Serializable]
public class LayoutDumpSnapshot
{
    public int screenWidth;
    public int screenHeight;
    public List<LayoutDumpItem> items = new();
    public List<LayoutDumpIssue> issues = new();
}
