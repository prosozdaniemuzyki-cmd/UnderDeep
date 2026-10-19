using System.Threading.Tasks;
using UnityEngine;

namespace UnderDeep.Platform
{
    using Core;

    public static class PlatformFactory
    {
        public static IPlatformProvider Create(bool forceYandex=false, bool forceVK=false)
        {
            // In Editor always Standalone unless forced
            #if UNITY_WEBGL && !UNITY_EDITOR
            if (forceYandex || IsYandex()) return new YandexProvider();
            if (forceVK || IsVK()) return new VKProvider();
            return new StandaloneProvider();
            #else
            if (forceYandex) return new YandexProvider();
            if (forceVK) return new VKProvider();
            return new StandaloneProvider();
            #endif
        }

        static bool IsYandex()
        {
#if UNITY_WEBGL
            try { return Application.absoluteURL.Contains("yandex"); } catch { return false; }
#else
            return false;
#endif
        }
        static bool IsVK()
        {
#if UNITY_WEBGL
            try { return Application.absoluteURL.Contains("vk.com"); } catch { return false; }
#else
            return false;
#endif
        }
    }

    // ---- Standalone ----
    public class StandaloneProvider : IPlatformProvider
    {
        public PlatformId Id => PlatformId.Standalone;
        public string Language => Application.systemLanguage==SystemLanguage.Russian? "ru":"en";
        public Task InitAsync(){ Debug.Log("[Platform] Standalone init"); return Task.CompletedTask; }
        public Task SaveAsync(string json){ PlayerPrefs.SetString("meta_save", json); PlayerPrefs.Save(); return Task.CompletedTask; }
        public Task<string> LoadAsync(){ return Task.FromResult(PlayerPrefs.GetString("meta_save","")); }
        public Task ShowInterstitialAsync(){ Debug.Log("[Ads] Interstitial (stub)"); return Task.CompletedTask; }
        public Task<bool> ShowRewardedAsync(string placement){ Debug.Log($"[Ads] Rewarded {placement} (stub success in Editor)"); return Task.FromResult(Application.isEditor); }
        public Task SetLeaderboardAsync(string board,int score){ Debug.Log($"[LB] {board}={score}"); PlayerPrefs.SetInt($"lb_{board}", score); return Task.CompletedTask; }
        public void GameReady(){ Debug.Log("[Platform] GameReady"); }
        public void GameplayStart(){}
        public void GameplayStop(){}
    }

    // ---- Yandex ----
    public class YandexProvider : IPlatformProvider
    {
        public PlatformId Id => PlatformId.YandexGames;
        public string Language { get; private set; } = "ru";
        public async Task InitAsync()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // call jslib
            Language = GetLangFromJS();
            Debug.Log($"[Yandex] Init lang={Language}");
            await Task.Yield();
#else
            Debug.Log("[Yandex] Editor stub init");
            await Task.CompletedTask;
#endif
        }
        string GetLangFromJS()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // jslib will set
            return Application.systemLanguage.ToString();
#else
            return "ru";
#endif
        }
        public Task SaveAsync(string json)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            YandexSDK.Save(json);
#endif
            PlayerPrefs.SetString("meta_save", json);
            return Task.CompletedTask;
        }
        public Task<string> LoadAsync()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var s = YandexSDK.Load();
            if(!string.IsNullOrEmpty(s)) return Task.FromResult(s);
#endif
            return Task.FromResult(PlayerPrefs.GetString("meta_save",""));
        }
        public async Task ShowInterstitialAsync()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            YandexSDK.ShowInterstitial();
            await Task.Delay(1000);
#else
            await Task.CompletedTask;
#endif
        }
        public async Task<bool> ShowRewardedAsync(string placement)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return YandexSDK.ShowRewarded(placement);
#else
            Debug.Log($"[Yandex stub] Rewarded {placement}");
            await Task.Delay(500);
            return true;
#endif
        }
        public Task SetLeaderboardAsync(string board,int score)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            YandexSDK.SetLeaderboard(board, score);
#endif
            return Task.CompletedTask;
        }
        public void GameReady(){
#if UNITY_WEBGL && !UNITY_EDITOR
            YandexSDK.GameReady();
#endif
        }
        public void GameplayStart(){
#if UNITY_WEBGL && !UNITY_EDITOR
            YandexSDK.GameplayStart();
#endif
        }
        public void GameplayStop(){
#if UNITY_WEBGL && !UNITY_EDITOR
            YandexSDK.GameplayStop();
#endif
        }
    }

    // ---- VK ----
    public class VKProvider : IPlatformProvider
    {
        public PlatformId Id => PlatformId.VKPlay;
        public string Language => "ru";
        public Task InitAsync(){ Debug.Log("[VK] Init"); return Task.CompletedTask; }
        public Task SaveAsync(string json){ PlayerPrefs.SetString("meta_save", json); return Task.CompletedTask; }
        public Task<string> LoadAsync(){ return Task.FromResult(PlayerPrefs.GetString("meta_save","")); }
        public Task ShowInterstitialAsync(){ Debug.Log("[VK] Interstitial"); return Task.CompletedTask; }
        public Task<bool> ShowRewardedAsync(string placement){ Debug.Log($"[VK] Rewarded {placement}"); return Task.FromResult(Application.isEditor); }
        public Task SetLeaderboardAsync(string board,int score){ return Task.CompletedTask; }
        public void GameReady(){}
        public void GameplayStart(){}
        public void GameplayStop(){}
    }

#if UNITY_WEBGL
    internal static class YandexSDK
    {
        [System.Runtime.InteropServices.DllImport("__Internal")] internal static extern void Save(string json);
        [System.Runtime.InteropServices.DllImport("__Internal")] internal static extern string Load();
        [System.Runtime.InteropServices.DllImport("__Internal")] internal static extern void ShowInterstitial();
        [System.Runtime.InteropServices.DllImport("__Internal")] internal static extern bool ShowRewarded(string placement);
        [System.Runtime.InteropServices.DllImport("__Internal")] internal static extern void SetLeaderboard(string board,int score);
        [System.Runtime.InteropServices.DllImport("__Internal")] internal static extern void GameReady();
        [System.Runtime.InteropServices.DllImport("__Internal")] internal static extern void GameplayStart();
        [System.Runtime.InteropServices.DllImport("__Internal")] internal static extern void GameplayStop();
        // stub for editor
        internal static void SaveStub(string j){ PlayerPrefs.SetString("meta_save", j); }
    }
#endif
}
