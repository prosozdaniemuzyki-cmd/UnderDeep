using UnityEngine;
using UnderDeep.Core;

namespace UnderDeep.Enemies
{
    /// <summary>Кристальный паук 400-700м — прячется в кристаллах, выпрыгивает при приближении.</summary>
    public class CrystalSpider : EnemyBase
    {
        public float detectRadius = 4f;
        public float jumpForce = 7f;
        public float hideAlpha = 0.3f;
        public float ambushCooldown = 3f;

        private bool _hidden = true;
        private float _cooldown;
        private SpriteRenderer _sr;

        protected override void Awake()
        {
            base.Awake();
            _sr = GetComponentInChildren<SpriteRenderer>();
            moveSpeed = 3.5f;
        }

        public override void Tick(float dt)
        {
            if (_player == null) return;
            float dist = Vector2.Distance(transform.position, _player.position);

            if (_hidden)
            {
                if (_sr) _sr.color = new Color(1,1,1, hideAlpha);
                if (dist <= detectRadius)
                {
                    // выпрыгивает
                    _hidden = false;
                    _cooldown = ambushCooldown;
                    var dir = (_player.position - transform.position).normalized;
                    transform.position += dir * 1.2f; // рывок
                    if (_sr) _sr.color = Color.white;
                    // эффект крика
                    Debug.Log("[Spider] Ambush!");
                }
            }
            else
            {
                _cooldown -= dt;
                // преследование
                var step = (_player.position - transform.position).normalized * moveSpeed * dt;
                transform.position += step;
                if (_cooldown <= 0)
                {
                    // снова прячется если игрок далеко
                    if (dist > detectRadius * 1.5f)
                    {
                        _hidden = true;
                    }
                }
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!_hidden && other.TryGetComponent<Player.HealthSystem>(out var h))
                h.TakeDamage(damage);
        }
    }
}
