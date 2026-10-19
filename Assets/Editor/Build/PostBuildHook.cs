#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using UnityEngine;

namespace UnderDeep.EditorTools
{
    public class PostBuildHook : MonoBehaviour
    {
        [PostProcessBuild(1)]
        public static void OnPostProcessBuild(BuildTarget target, string path)
        {
            if (target != BuildTarget.WebGL) return;
            Debug.Log($"[PostBuild] WebGL built to {path}");

            // 1. Копируем иконку как thumbnail для Yandex template (если нет)
            var iconSrc = "Assets/_Project/Art/icon_512.png";
            var thumbDst = Path.Combine(path, "thumbnail.png");
            if (File.Exists(iconSrc) && !File.Exists(thumbDst))
            {
                File.Copy(iconSrc, thumbDst, true);
                Debug.Log("[PostBuild] Copied icon as thumbnail");
            }

            // 2. Добавляем yandex-manifest.json (для проверки SDK)
            var manifest = @"{
  ""game"": ""Deep Lone"",
  ""version"": ""1.1.0"",
  ""sdk"": ""YandexGames v2"",
  ""features"": [""leaderboard:maxDepth"", ""adv:interstitial"", ""adv:rewarded:revive,double_loot"", ""storage:MetaSave""],
  ""build"": """ + System.DateTime.Now.ToString("yyyy-MM-dd") + @"""
}";
            File.WriteAllText(Path.Combine(path, "yandex-manifest.json"), manifest);

            // 3. Проверяем размер (лимит Яндекса 30MB Brotli)
            var buildFolder = Path.Combine(path, "Build");
            if (Directory.Exists(buildFolder))
            {
                long size = 0;
                foreach (var f in Directory.GetFiles(buildFolder, "*", SearchOption.AllDirectories))
                    size += new FileInfo(f).Length;
                Debug.Log($"[PostBuild] Build folder size: {size / 1048576f:F1} MB");
                if (size > 30 * 1024 * 1024)
                    Debug.LogWarning("[PostBuild] ⚠️ Build >30 MB — включи Code Optimization: Size, strip Engine Code, сожми текстуры!");
            }

            // 4. Подменяем заголовки для Yandex (если шаблон кастомный — уже норм)
            AssetDatabase.Refresh();
        }
    }
}
#endif
