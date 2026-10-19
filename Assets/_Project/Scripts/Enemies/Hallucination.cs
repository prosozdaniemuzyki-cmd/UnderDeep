using UnityEngine;
using UnderDeep.Core;
using UnderDeep.World;

namespace UnderDeep.Enemies
{
    /// <summary>Галлюцинация 700-900м — копирует игрока, идёт сквозь стены, наносит безумие.</summary>
    public class Hallucination : EnemyBase
    {
        public float sanityDamage = 15f;
        public float lifeTime = 8f;
        public float copyDelay = 0.4f;

        private Vector3 _lastPlayerPos;
        private float _timer;
        private WorldGrid _worldLocal;

        protected override void Awake()
        {
            base.Awake();
            _worldLocal = FindFirstObjectByType<WorldGrid>();
            hp = 1;
            damage = 0;
            moveSpeed = 4f;
        }

        public override void Tick(float dt)
        {
            if (_player == null) return;
            _timer += dt;
            if (_timer > lifeTime)
            {
                Destroy(gameObject);
                return;
            }
            // идёт сквозь стены — игнорируем коллизию, просто лерп к игроку
            _lastPlayerPos = _player.position;
            transform.position = Vector3.MoveTowards(transform.position, _lastPlayerPos, moveSpeed * dt);

            // мерцание
            float a = 0.5f + Mathf.Sin(Time.time * 10f) * 0.3f;
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr) sr.color = new Color(1,1,1,a);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent<Player.SanitySystem>(out var s))
            {
                s.AddSanity(sanityDamage);
                Destroy(gameObject);
            }
        }
    }
}
