using UnityEngine;
using UnityEngine.SceneManagement;
using UnderDeep.Core;

namespace UnderDeep.Utils
{
    public class SceneSetup : MonoBehaviour
    {
        [Header("Run Scene")]
        public bool autoStartRun = false;
        public int worldWidth = 50;
        public int worldHeight = 100;

        void Start()
        {
            // If we are in Run scene without Boot, create fallback save
            if (!ServiceLocator.TryGet<SaveService>(out _))
            {
                Debug.Log("[SceneSetup] Boot not found, creating Standalone fallback");
                var provider = new Platform.StandaloneProvider();
                ServiceLocator.Register<IPlatformProvider>(provider);
                var meta = new MetaSave();
                var save = new SaveService(provider, meta);
                ServiceLocator.Register(save);
                var gsmGO = new GameObject("GameStateMachine");
                gsmGO.AddComponent<GameStateMachine>();
                ServiceLocator.Get<GameStateMachine>().SetState(GameState.Run);
            }

            if (autoStartRun)
            {
                var gsm = ServiceLocator.Get<GameStateMachine>();
                gsm.SetState(GameState.Run);
            }
        }

        public void LoadBase() => SceneManager.LoadScene("Base");
        public void LoadRun() => SceneManager.LoadScene("Run");
    }
}
