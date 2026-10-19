using UnityEngine;
using UnderDeep.Core;
using UnderDeep.World;

namespace UnderDeep.Enemies
{
    public abstract class EnemyBase : MonoBehaviour, IPoolable
    {
        [Header("Enemy")]
        public int hp = 2;
        public float moveSpeed = 2f;
        public int damage = 1;
        protected WorldGrid _world;
        protected Transform _player;

        protected virtual void Awake()
        {
            _world = FindFirstObjectByType<WorldGrid>();
        }

        protected virtual void Start()
        {
            if (ServiceLocator.TryGet<Player.DroneController>(out var d)) _player = d.transform;
        }

        public abstract void Tick(float dt);

        void Update()
        {
            if (ServiceLocator.TryGet<GameStateMachine>(out var gsm) && gsm.Current != GameState.Run) return;
            Tick(Time.deltaTime);
        }

        public void TakeDamage(int dmg)
        {
            hp -= dmg;
            if (hp <= 0) Despawn();
        }

        protected virtual void Despawn()
        {
            gameObject.SetActive(false);
        }

        public void OnSpawn() { hp = 2; gameObject.SetActive(true); }
        public void OnDespawn() { gameObject.SetActive(false); }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent<Player.HealthSystem>(out var h)) h.TakeDamage(damage);
        }
    }
}
