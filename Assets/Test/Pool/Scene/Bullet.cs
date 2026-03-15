using GoveKits.Runtime.Core.Pool;
using UnityEngine;

namespace GoveKits.Test.Pool.Scene
{
    public class Bullet : MonoBehaviour, IPoolable
    {
        [SerializeField] private float speed = 12f;
        [SerializeField] private float lifeTime = 2f;

        private Vector3 _direction;
        private float _lifeTimer;
        private bool _isFlying;

        public void Fire(Vector3 position, Vector3 direction)
        {
            transform.position = position;
            _direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.forward;
            _lifeTimer = lifeTime;
            _isFlying = true;
        }

        private void Update()
        {
            if (!_isFlying) return;

            transform.position += _direction * speed * Time.deltaTime;
            _lifeTimer -= Time.deltaTime;

            if (_lifeTimer <= 0f)
            {
                PoolCore.Return(gameObject);
            }
        }

        public void OnRecycle()
        {
            _direction = Vector3.zero;
            _lifeTimer = 0f;
            _isFlying = false;
        }
    }
}