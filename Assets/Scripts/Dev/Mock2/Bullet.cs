using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// プレイヤーの弾
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class Bullet : MonoBehaviour
{
    [SerializeField] private float _speed = 20;
    private LockOn _lockOn;
    private Transform _target;
    private CancellationTokenSource _cts;

    public void Setup(LockOn lockOn, Transform target)
    {
        _lockOn = lockOn;
        _target = target;
        
        transform.LookAt(_target.position);
        
        // 初期化時に弾の寿命カウントダウンを開始
        _cts = new CancellationTokenSource();
        StartDestroyTimer(_cts.Token).Forget();
    }

    private void Update()
    {
        if (_target != null)
        {
            Vector3 direction = (_target.position - transform.position).normalized;
            transform.position += direction * _speed * Time.deltaTime;
        }
    }

    /// <summary>
    /// 3秒以内にターゲットに衝突しなかったら消える
    /// </summary>
    private async UniTask StartDestroyTimer(CancellationToken token)
    {
        try
        {
            await UniTask.Delay(3000, cancellationToken: token); // 3秒待機
            if (!token.IsCancellationRequested)
            {
                Destroy(gameObject);
            }
        }
        catch (OperationCanceledException)
        {
            // キャンセルされた場合は何もしない
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Target")) // ターゲットに衝突したら
        {
            _lockOn.SearchTarget(); // ターゲット更新
            Destroy(gameObject); // 自分を消す
        }
    }
    
    private void OnDestroy()
    {
        // オブジェクト破棄時にトークンをキャンセル
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
