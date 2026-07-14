// WebGLBuilder.cs — SortGemsのAssets/Editor/WebGLBuilder.csと同じパターン。
// リポジトリルートの docs/ に出力し、GitHub Pages（source: main branch, /docs）でそのまま公開できるようにする。

using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

public class WebGLBuilder
{
    [MenuItem("Tools/ChillBlocks/Build WebGL")]
    public static void Build()
    {
        string projectDir = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        // リポジトリのルートにある docs フォルダを出力先にする
        string buildPath = Path.GetFullPath(Path.Combine(projectDir, "../docs"));

        // ディレクトリをクリーンアップ
        if (Directory.Exists(buildPath))
        {
            try
            {
                Directory.Delete(buildPath, true);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[WebGLBuilder] 既存のdocsフォルダ削除中に例外が発生しました（無視して続行します）: {ex.Message}");
            }
        }
        Directory.CreateDirectory(buildPath);

        // ビルドターゲットを WebGL に切り替える
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

        // GitHub Pages向けに圧縮を無効化（解凍ヘッダー不整合によるWebGL起動エラーを防止）
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;

        BuildPlayerOptions options = new BuildPlayerOptions();
        options.scenes = new[] { "Assets/Scenes/GameScene.unity" };
        options.locationPathName = buildPath;
        options.target = BuildTarget.WebGL;
        options.options = BuildOptions.None;

        Debug.Log($"[WebGLBuilder] WebGLビルドを開始します。出力先: {buildPath}");
        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            // GitHub PagesがデフォルトでJekyllを通そうとしてUnity WebGL出力と衝突するのを防ぐ
            // （テーマのSCSSレンダリングで "dir_chdir0" エラーが出て公開に失敗する事例があったため）。
            File.WriteAllText(Path.Combine(buildPath, ".nojekyll"), string.Empty);
            Debug.Log($"[WebGLBuilder] WebGLビルド成功！サイズ: {summary.totalSize} bytes");
        }
        else
        {
            Debug.LogError($"[WebGLBuilder] WebGLビルド失敗！エラー数: {summary.totalErrors}");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
