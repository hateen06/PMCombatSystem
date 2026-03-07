using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// 에디터 유틸리티 도구.
/// Unity 메뉴 Tools/PM 에서 접근 가능.
/// </summary>
public static class PMTools
{
    [MenuItem("Tools/PM/프로젝트 상태 확인")]
    public static void Status()
    {
        var scene = EditorSceneManager.GetActiveScene();
        var goCount = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length;
        Debug.Log($"Scene: {scene.name} | Objects: {goCount} | Compile Error: {EditorUtility.scriptCompilationFailed}");
    }

    [MenuItem("Tools/PM/스프라이트 FullRect 수정")]
    public static void SpriteFixAll()
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
        Debug.Log($"Fixed {count} sprites to FullRect");
    }

    [MenuItem("Tools/PM/씬 저장")]
    public static void Save()
    {
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Saved");
    }
}
