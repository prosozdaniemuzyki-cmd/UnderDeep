using UnityEngine;

namespace UnderDeep.Core
{
    public class GameStateMachine : MonoBehaviour
    {
        public GameState Current { get; private set; } = GameState.Boot;
        public int MaxDepthThisRun { get; private set; }

        void Awake() => ServiceLocator.Register(this);

        public void SetState(GameState next)
        {
            if (next == Current) return;
            var prev = Current;
            Current = next;
            EventBus.Publish(new GameStateChanged(prev, next));
            Debug.Log($"[State] {prev} -> {next}");

            // Web platform hooks
            if (ServiceLocator.TryGet<IPlatformProvider>(out var p))
            {
                if (next == GameState.Run) p.GameplayStart();
                if (prev == GameState.Run) p.GameplayStop();
            }

            if (next == GameState.Run) MaxDepthThisRun = 0;
        }

        public void ReportDepth(int depth)
        {
            if (depth > MaxDepthThisRun) MaxDepthThisRun = depth;
            EventBus.Publish(new DepthChanged(depth, MaxDepthThisRun));
            // biome event
            var biome = DepthToBiome(depth);
            EventBus.Publish(new BiomeEntered(biome, depth));
        }

        public static BiomeId DepthToBiome(int depth)
        {
            if (depth < 10) return BiomeId.Surface;
            if (depth < 20) return BiomeId.Metal;
            if (depth < 40) return BiomeId.BioCaves;
            if (depth < 70) return BiomeId.Crystal;
            if (depth < 90) return BiomeId.Infection;
            return BiomeId.Core;
        }
    }
}
