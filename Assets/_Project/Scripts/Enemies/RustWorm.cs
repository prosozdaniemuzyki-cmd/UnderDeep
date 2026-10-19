using UnityEngine;
using UnderDeep.World;

namespace UnderDeep.Enemies
{
    /// <summary>Ржавый червь — движется по прямой, уничтожает блоки.</summary>
    public class RustWorm : EnemyBase
    {
        private Vector2Int _dir;
        private float _stepTimer;

        protected override void Awake()
        {
            base.Awake();
            _dir = Random.value > 0.5f ? Vector2Int.right : Vector2Int.left;
            if (Random.value > 0.7f) _dir = Random.value > 0.5f ? Vector2Int.up : Vector2Int.down;
        }

        public override void Tick(float dt)
        {
            _stepTimer += dt;
            if (_stepTimer < 0.4f) return;
            _stepTimer = 0;
            var gridPos = _world.WorldToGrid(transform.position);
            var next = gridPos + _dir;
            // if bedrock or out, turn
            if (!_world.InBounds(next) || _world.Get(next).type == Core.BlockType.Bedrock)
            {
                _dir = new Vector2Int(-_dir.x, -_dir.y);
                return;
            }
            // destroy block in front
            if (!_world.IsAir(next))
            {
                // instant destroy
                var data = _world.Get(next);
                // use drill tick with high power
                _world.DrillTick(next, 10f, 10f);
            }
            transform.position = _world.GridToWorld(next);
        }
    }
}
