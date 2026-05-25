using UnityEditor;
using System.IO;

public class Builder
{
    // === 菜单入口（手动构建） ===
    [MenuItem("Build/Windows Standalone")]
    static void BuildWindows()
    {
        DoBuild("Builds/Game.exe", BuildTarget.StandaloneWindows);
    }

    [MenuItem("Build/Windows Standalone (Development)")]
    static void BuildWindowsDev()
    {
        DoBuild("Builds/Game_Dev.exe", BuildTarget.StandaloneWindows, BuildOptions.Development);
    }

    // === batchmode 入口 ===
    static void DoBuild()
    {
        DoBuild("Builds/Game.exe", BuildTarget.StandaloneWindows);
    }

    static void DoBuild(string outputPath, BuildTarget target, BuildOptions options = BuildOptions.None)
    {
        // 确保输出目录存在
        string dir = Path.GetDirectoryName(outputPath);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        // 收集场景列表
        string[] scenes = new string[EditorBuildSettings.scenes.Length];
        for (int i = 0; i < scenes.Length; i++)
        {
            scenes[i] = EditorBuildSettings.scenes[i].path;
        }

        if (scenes.Length == 0)
        {
            EditorUtility.DisplayDialog("Builder Error",
                "No scenes in Build Settings. Please add scenes via File > Build Settings.",
                "OK");
            return;
        }

        UnityEngine.Debug.Log("[Builder] Starting build: " + outputPath);

        string error = BuildPipeline.BuildPlayer(scenes, outputPath, target, options);

        if (string.IsNullOrEmpty(error))
        {
            UnityEngine.Debug.Log("[Builder] Build succeeded: " + outputPath);
        }
        else
        {
            UnityEngine.Debug.LogError("[Builder] Build failed: " + error);
        }
    }

    // === 辅助：设置场景列表 ===
    [MenuItem("Build/Set Scenes from Assets/Scenes")]
    static void SetScenesFromFolder()
    {
        string[] guids = AssetDatabase.FindAssets("t:scene", new string[] { "Assets/Scenes" });
        EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[guids.Length];

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            scenes[i] = new EditorBuildSettingsScene(path, true);
        }

        EditorBuildSettings.scenes = scenes;
        UnityEngine.Debug.Log("[Builder] Set " + scenes.Length + " scene(s) from Assets/Scenes");
    }
}