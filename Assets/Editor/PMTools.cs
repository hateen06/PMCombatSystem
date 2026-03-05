using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// unity-cli 커스텀 도구.
/// 터미널에서 unity-cli run pm_xxx 형태로 호출 가능.
/// </summary>
[UnityCliTool]
public static class PMTools
{
    [UnityCliCommand("pm_status", "프로젝트 상태 확인")]
    public static string Status()
    {
        var scene = EditorSceneManager.GetActiveScene();
        var goCount = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length;
        var scripts = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Length;
        return $"Scene: {scene.name} | Objects: {goCount} | Scripts: {scripts} | Compile Error: {EditorUtility.scriptCompilationFailed}";
    }

    [UnityCliCommand("pm_sprite_fix", "스프라이트 Mesh Type을 Full Rect로 변경")]
    public static string SpriteFixAll()
    {
        var guids = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Sprites" });
        int count = 0;
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) continue;

            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            if (settings.spriteMeshType != SpriteMeshType.FullRect)
            {
                settings.spriteMeshType = SpriteMeshType.FullRect;
                imp.SetTextureSettings(settings);
                imp.SaveAndReimport();
                count++;
            }
        }
        return $"Fixed {count} sprites to FullRect";
    }

    [UnityCliCommand("pm_scene_info", "씬 오브젝트 목록")]
    public static string SceneInfo()
    {
        var roots = EditorSceneManager.GetActiveScene().GetRootGameObjects();
        var result = new List<string>();
        foreach (var go in roots)
        {
            var comps = go.GetComponents<Component>();
            var compNames = new List<string>();
            foreach (var c in comps)
                if (c != null && !(c is Transform))
                    compNames.Add(c.GetType().Name);
            result.Add($"{go.name} [{string.Join(", ", compNames)}]");
        }
        return string.Join("\n", result);
    }

    [UnityCliCommand("pm_save", "씬 저장")]
    public static string Save()
    {
        EditorSceneManager.SaveOpenScenes();
        return "Saved";
    }

    [UnityCliCommand("pm_play", "플레이 모드 시작/정지")]
    public static string TogglePlay()
    {
        EditorApplication.isPlaying = !EditorApplication.isPlaying;
        return EditorApplication.isPlaying ? "Playing" : "Stopped";
    }

    [UnityCliCommand("pm_move", "오브젝트 위치 이동 (name x y)")]
    public static string Move(string name, float x, float y)
    {
        var go = GameObject.Find(name);
        if (go == null) return $"'{name}' not found";
        go.transform.position = new Vector3(x, y, go.transform.position.z);
        EditorSceneManager.SaveOpenScenes();
        return $"Moved {name} to ({x}, {y})";
    }

    [UnityCliCommand("pm_scale", "오브젝트 스케일 변경 (name scale)")]
    public static string Scale(string name, float scale)
    {
        var go = GameObject.Find(name);
        if (go == null) return $"'{name}' not found";
        go.transform.localScale = new Vector3(scale, scale, 1f);
        EditorSceneManager.SaveOpenScenes();
        return $"Scaled {name} to {scale}";
    }
}
