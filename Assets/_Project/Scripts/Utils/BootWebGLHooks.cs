using UnityEngine;
using UnderDeep.Core;

namespace UnderDeep.Utils
{
    /// <summary>Пауза при скрытии вкладки + cursor lock для WebGL.</summary>
    public class BootWebGLHooks : MonoBehaviour
    {
        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                Time.timeScale = 0;
                if (ServiceLocator.TryGet<IPlatformProvider>(out var p)) p.GameplayStop();
                Debug.Log("[WebGL] Tab hidden — pause");
            }
            else
            {
                Time.timeScale = 1;
                if (ServiceLocator.TryGet<IPlatformProvider>(out var p)) p.GameplayStart();
            }
        }
        void OnApplicationPause(bool pause) => OnApplicationFocus(!pause);
    }
}
