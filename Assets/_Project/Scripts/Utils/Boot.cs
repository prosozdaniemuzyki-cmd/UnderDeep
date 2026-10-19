using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnderDeep.Core;
using UnderDeep.Platform;

namespace UnderDeep.Utils
{
    /// <summary>Entry point. Must be first scene in Build Settings.</summary>
    public class Boot : MonoBehaviour
    {
        [SerializeField] private bool useYandex = false;
        [SerializeField] private bool useVK = false;

        async void Start()
        {
            DontDestroyOnLoad(gameObject);
            Debug.Log("[Boot] Starting...");

            // Create platform provider
            var provider = PlatformFactory.Create(useYandex, useVK);
            ServiceLocator.Register<IPlatformProvider>(provider);
            await provider.InitAsync();

            // Load meta save
            var json = await provider.LoadAsync();
            MetaSave meta = null;
            if (!string.IsNullOrEmpty(json))
                try { meta = JsonUtility.FromJson<MetaSave>(json); } catch { }
            meta ??= new MetaSave();
            var saveService = new SaveService(provider, meta);
            ServiceLocator.Register(saveService);

            // GameState
            var gsmGO = new GameObject("GameStateMachine");
            DontDestroyOnLoad(gsmGO);
            gsmGO.AddComponent<GameStateMachine>();

            provider.GameReady();
            Debug.Log($"[Boot] Ready. Platform={provider.Id} Lang={provider.Language} SaveDepth={meta.maxDepthReached}");

            // Go to Base (shop) scene
            ServiceLocator.Get<GameStateMachine>().SetState(GameState.Base);
            SceneManager.LoadScene("Base");
        }

        void OnApplicationFocus(bool focus)
        {
            if (ServiceLocator.TryGet<IPlatformProvider>(out var p))
            {
                if (focus) p.GameplayStart(); else p.GameplayStop();
            }
        }
    }
}
