using UniRx;
using UnityEngine;

namespace Mock3
{
    /// <summary>
    /// Playerの動きを制御する
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        public ReactiveProperty<bool> IsEnemyTurn = new ReactiveProperty<bool>(true);

        private Animator _animator;
        private Rigidbody _rb;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _rb = GetComponent<Rigidbody>();
        }
    }
}