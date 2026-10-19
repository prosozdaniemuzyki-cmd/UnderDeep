#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.Build.Reporting;

namespace UnderDeep.EditorTools
{
    public static class WebGLBuilder
    {
        const string ScenesBoot = "Assets/_Project/Scenes/Boot.unity";
        const string ScenesBase = "Assets/_Project/Scenes/Base.unity";
        const string ScenesRun  = "Assets/_Project/Scenes/Run.unity";

        [MenuItem("UnderDeep/Build/Настроить PlayerSettings для WebGL (Yandex/VK)")]
        public static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "UnderDeep Studio";
            PlayerSettings.productName = "Deep Lone — Ядро Коры-7";
            PlayerSettings.defaultScreenWidth = 960;
            PlayerSettings.defaultScreenHeight = 600;
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;

            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.memorySize = 32;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
            PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
            PlayerSettings.WebGL.threadsSupport = false;
            PlayerSettings.WebGL.showDiagnostics = false;
            PlayerSettings.WebGL.template = "Application:YandexGames"; // дефолт

            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.gcIncremental = true;

            EditorUtility.DisplayDialog("UnderDeep", "PlayerSettings для WebGL настроены!\nBrotli, Decompression Fallback, Wasm, 960×600, no logo", "ОК");
            Debug.Log("[Build] PlayerSettings configured for WebGL");
        }

        [MenuItem("UnderDeep/Build/Выставить сцены Boot/Base/Run в Build Settings")]
        public static void SetScenes()
        {
            var scenes = new[]
            {
                new EditorBuildSettingsScene(ScenesBoot, true),
                new EditorBuildSettingsScene(ScenesBase, true),
                new EditorBuildSettingsScene(ScenesRun, true),
            };
            EditorBuildSettings.scenes = scenes;
            Debug.Log("[Build] Scenes set: Boot, Base, Run");
        }

        [MenuItem("UnderDeep/Build/Билд — Yandex Games (Build/Yandex)")]
        public static void BuildYandex()
        {
            ConfigurePlayerSettings();
            PlayerSettings.WebGL.template = "PROJECT:YandexGames";
            SetScenes();
            var opts = new BuildPlayerOptions
            {
                scenes = new[]{ ScenesBoot, ScenesBase, ScenesRun },
                locationPathName = "Build/Yandex",
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(opts);
            HandleReport(report, "Yandex");
        }

        [MenuItem("UnderDeep/Build/Билд — VK Play (Build/VK)")]
        public static void BuildVK()
        {
            ConfigurePlayerSettings();
            PlayerSettings.WebGL.template = "PROJECT:VKPlay";
            SetScenes();
            var opts = new BuildPlayerOptions
            {
                scenes = new[]{ ScenesBoot, ScenesBase, ScenesRun },
                locationPathName = "Build/VK",
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(opts);
            HandleReport(report, "VK");
        }

        [MenuItem("UnderDeep/Build/Билд — Standalone itch.io (Build/Standalone)")]
        public static void BuildStandalone()
        {
            ConfigurePlayerSettings();
            PlayerSettings.WebGL.template = "APPLICATION:Default";
            SetScenes();
            var opts = new BuildPlayerOptions
            {
                scenes = new[]{ ScenesBoot, ScenesBase, ScenesRun },
                locationPathName = "Build/Standalone",
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(opts);
            HandleReport(report, "Standalone");
        }

        static void HandleReport(BuildReport report, string platform)
        {
            var sum = report.summary;
            if (sum.result == BuildResult.Succeeded)
            {
                Debug.Log($"[Build] {platform} succeeded: {sum.totalSize / 1048576} MB в {sum.totalTime.TotalSeconds:F1}s → {sum.outputPath}");
                EditorUtility.DisplayDialog("Билд готов", $"{platform}: {sum.outputPath}\nРазмер {sum.totalSize/1048576} МБ", "Супер");
                AddPostBuildReadme(sum.outputPath, platform);
            }
            else
            {
                Debug.LogError($"[Build] {platform} failed: {sum.result}");
            }
        }

        static void AddPostBuildReadme(string path, string platform)
        {
            var txt = $"# Build {platform} — {System.DateTime.Now:yyyy-MM-dd HH:mm}\n\n" +
                      $"- Залей содержимое папки на хостинг/Яндекс Консоль\n" +
                      $"- Проверь index.html и Build/*.data\n" +
                      $"- Yandex: укажи в консоли SDK → GameReady проверь\n" +
                      $"- VK: подключи vk-bridge, Storage fallback уже есть\n";
            System.IO.File.WriteAllText(System.IO.Path.Combine(path, "README_BUILD.txt"), txt);
        }
    }
}
#endif
