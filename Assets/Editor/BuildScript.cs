using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Batchmode builds: Unity.exe -batchmode -quit -projectPath . -executeMethod BuildScript.BuildWindows -logFile build.log
public static class BuildScript
{
    static string[] Scenes() =>
        EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

    public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Builds/Windows/TimeSkip.exe");

    public static void BuildWebGL()
    {
        // Lets the compressed build run from any static server (localhost, itch) without Content-Encoding headers
        PlayerSettings.WebGL.decompressionFallback = true;
        Build(BuildTarget.WebGL, "Builds/WebGL");
    }

    static void Build(BuildTarget target, string path)
    {
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = Scenes(),
            locationPathName = path,
            target = target,
            options = BuildOptions.None
        });
        Debug.Log($"[BuildScript] {target} -> {path}: {report.summary.result}, {report.summary.totalErrors} errors");
        if (Application.isBatchMode)
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}
