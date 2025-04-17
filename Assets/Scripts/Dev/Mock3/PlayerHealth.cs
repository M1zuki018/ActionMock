using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Mock3
{
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private int _maxHealth = 100;
        [SerializeField] private GameObject _hitEffect;
        [SerializeField] private AudioClip _hitSound;
        [SerializeField] private AudioClip _healSound;

        private int _currentHealth;

        // 無敵時間
        [SerializeField] private float _invincibilityTime = 1.0f;
        private float _lastHitTime = -100f;

        // 回避中フラグ
        private bool _isDodging = false;

        // HPバー参照
        [SerializeField] private Image _healthBarImage;

        // ダメージ視覚効果
        [SerializeField] private Image _damageOverlay;

        // イベント
        public Subject<int> OnDamaged = new Subject<int>();
        public Subject<int> OnHealed = new Subject<int>();
        public Subject<Unit> OnDeath = new Subject<Unit>();

        private void Start()
        {
            _currentHealth = _maxHealth;
            UpdateHealthBar();
        }

        private void Update()
        {
            // ダメージオーバーレイのフェードアウト
            if (_damageOverlay != null)
            {
                Color overlayColor = _damageOverlay.color;
                if (overlayColor.a > 0)
                {
                    overlayColor.a = Mathf.Max(0, overlayColor.a - Time.deltaTime);
                    _damageOverlay.color = overlayColor;
                }
            }
        }

        public void TakeDamage(int damage)
        {
            // 無敵時間中または回避中は無効
            if (Time.time - _lastHitTime < _invincibilityTime || _isDodging)
                return;

            _lastHitTime = Time.time;

            // ダメージ適用
            _currentHealth = Mathf.Max(0, _currentHealth - damage);

            // エフェクト生成
            if (_hitEffect != null)
            {
                Instantiate(_hitEffect, transform.position, Quaternion.identity);
            }

            // サウンド再生
            if (AudioController.Instance != null && _hitSound != null)
            {
                AudioController.Instance.PlaySE(_hitSound);
            }

            // ダメージオーバーレイ
            if (_damageOverlay != null)
            {
                Color overlayColor = _damageOverlay.color;
                overlayColor.a = 0.5f; // ダメージを受けたときの透明度
                _damageOverlay.color = overlayColor;
            }

            // UI更新
            UpdateHealthBar();

            // イベント発火
            OnDamaged.OnNext(damage);

            // 死亡判定
            if (_currentHealth <= 0)
            {
                OnDeath.OnNext(Unit.Default);
                // ゲームオーバー処理
            }
        }

        public void Heal(int amount)
        {
            int oldHealth = _currentHealth;
            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);

            // 実際に回復した量
            int healedAmount = _currentHealth - oldHealth;

            if (healedAmount > 0)
            {
                // サウンド再生
                if (AudioController.Instance != null && _healSound != null)
                {
                    AudioController.Instance.PlaySE(_healSound);
                }

                // UI更新
                UpdateHealthBar();

                // イベント発火
                OnHealed.OnNext(healedAmount);
            }
        }

        public void SetDodging(bool isDodging)
        {
            _isDodging = isDodging;
        }

        private void UpdateHealthBar()
        {
            if (_healthBarImage != null)
            {
                _healthBarImage.fillAmount = (float)_currentHealth / _maxHealth;
            }
        }
    }
}