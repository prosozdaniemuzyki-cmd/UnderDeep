using UnityEngine;
using UnderDeep.Core;

namespace UnderDeep.Enemies
{
    /// <summary>Светляк-страж 200-400м — летает вокруг игрока по орбите, при контакте отбрасывает и даёт sanity.</summary>
    public class FireflyGuardian : EnemyBase
    {
        public float orbitRadius = 3f;
        public float orbitSpeed = 60f;
        public float chaseSpeed = 3f;
        public float knockbackForce = 5f;

        private float _angle;

        public override void Tick(float dt)
        {
            if (_player == null) return;
            _angle += orbitSpeed * dt * Mathf.Deg2Rad;

            float dist = Vector2.Distance(transform.position, _player.position);
            Vector3 targetPos;
            if (dist > orbitRadius * 1.5f)
            {
                // подлетает
                targetPos = Vector3.MoveTowards(transform.position, _player.position, chaseSpeed * dt);
            }
            else
            {
                // орбита
                targetPos = _player.position + new Vector3(Mathf.Cos(_angle), Mathf.Sin(_angle), 0) * orbitRadius;
                targetPos = Vector3.Lerp(transform.position, targetPos, dt * 2f);
            }
            transform.position = targetPos;

            // пульсация света (если есть Light2D)
            float s = 1f + Mathf.Sin(Time.time * 4f) * 0.15f;
            transform.localScale = Vector3.one * s;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent<Player.DroneController>(out var drone))
            {
                var rb = drone.GetComponent<Rigidbody2D>();
                if (rb) rb.AddForce((drone.transform.position - transform.position).normalized * knockbackForce, ForceMode2D.Impulse);
                if (drone.TryGetComponent<Player.SanitySystem>(out var sanity))
                    sanity.AddSanity(8f);
                // отброс и урон 0, но дезориентация
            }
        }
    }
}
