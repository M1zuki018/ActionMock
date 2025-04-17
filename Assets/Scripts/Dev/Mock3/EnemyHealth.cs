using System.Collections.Generic;
using UniRx;
using UnityEngine;

namespace Mock3
{
    /// <summary>
    /// 敵のHealthクラス
    /// </summary>
    public class EnemyHealth : MonoBehaviour
    {
        [SerializeField] private int _maxHealth = 100;
        [SerializeField] private AudioClip _hitSound;
        [SerializeField] private AudioClip _deathSound;

        private int _currentHealth;

        // 無敵時間（連続ヒット防止）
        [SerializeField] private float _invincibilityTime = 0.2f;
        private float _lastHitTime = -100f;

        // 弱点部位（追加ダメージ）
        [SerializeField] private List<Transform> _weakPoints = new List<Transform>();
        [SerializeField] private float _weakPointDamageMultiplier = 1.5f;

        // イベント
        public Subject<int> OnDamaged = new Subject<int>();
        public Subject<Unit> OnDeath = new Subject<Unit>();

        private void Start()
        {
            _currentHealth = _maxHealth;
        }

        public bool TakeDamage(int damage, Vector3 hitPosition)
        {
            // 無敵時間中は無効
            if (Time.time - _lastHitTime < _invincibilityTime)
                return false;

            _lastHitTime = Time.time;

            // 弱点ヒット判定
            float multiplier = 1.0f;
            foreach (var weakPoint in _weakPoints)
            {
                if (Vector3.Distance(weakPoint.position, hitPosition) < 0.5f)
                {
                    multiplier = _weakPointDamageMultiplier;
                    break;
                }
            }

            // ダメージ計算と適用
            int actualDamage = Mathf.RoundToInt(damage * multiplier);
            _currentHealth = Mathf.Max(0, _currentHealth - actualDamage);

            // サウンド再生
            if (AudioController.Instance != null)
            {
                AudioController.Instance.PlaySE(_hitSound);
            }

            // イベント発火
            OnDamaged.OnNext(actualDamage);

            // 死亡判定
            if (_currentHealth <= 0)
            {
                if (AudioController.Instance != null)
                {
                    AudioController.Instance.PlaySE(_deathSound);
                }

                OnDeath.OnNext(Unit.Default);
                return true; // 敵を倒した
            }

            return false; // 敵はまだ生きている
        }
    }
}