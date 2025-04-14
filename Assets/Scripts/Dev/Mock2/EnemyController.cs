using System;
using UniRx;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 15f;
    [SerializeField] private Transform _player;
    [SerializeField] private float _randomMovementRange = 5f;
    [SerializeField] private float _randomMovementSpeed = 2f;
    [SerializeField] private float _minDistanceFromPlayer = 40f;
    [SerializeField] private float _maxDistanceFromPlayer = 50f;
    [SerializeField] private GameObject _bulletPrefab;
    
    private Vector3 _randomOffset;
    private float _randomTimer;
    private PlayerController _playerCon;
    private IDisposable _playerDisposable;

    private void Awake()
    {
        _playerCon = _player.GetComponent<PlayerController>();
    }
    
    private void Start()
    {
        _playerDisposable = _playerCon.IsEnemyTrun.Subscribe(_ => Attack()); // 自分のターンの時に攻撃を行う
        GenerateNewRandomOffset(); // 初期のランダムオフセットを設定
    }
    
    private void Update()
    {
        Move();
    }

    #region 移動

    /// <summary>
    /// 移動処理
    /// </summary>
    private void Move()
    {
        // ランダムオフセットを定期的に更新
        _randomTimer -= Time.deltaTime;
        if (_randomTimer <= 0)
        {
            GenerateNewRandomOffset();
            _randomTimer = Random.Range(1f, 3f); // 1〜3秒ごとに新しいランダム方向を設定
        }
        
        // プレイヤーと自分の距離を計算
        float distanceToPlayer = transform.position.x - _player.position.x;
        
        // 目標位置を計算（プレイヤーの左側＋ランダムオフセット）
        Vector3 targetPosition = _player.position + new Vector3(-45f, 0f, 0f) + _randomOffset;
        
        // プレイヤーとの距離に基づいて移動速度を調整
        float speedMultiplier = 1f;
        if (distanceToPlayer < _minDistanceFromPlayer)
        {
            // プレイヤーに近すぎる場合は少し後退
            speedMultiplier = 2f;
            targetPosition += new Vector3(-15f, 0f, 0f);
        }
        else if (distanceToPlayer > _maxDistanceFromPlayer)
        {
            // プレイヤーから遠すぎる場合は少し速く追いかける
            speedMultiplier = 0.5f;
        }
        
        // エネミーをターゲット位置に向かって移動
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            _moveSpeed * speedMultiplier * Time.deltaTime
        );
    }

    /// <summary>
    /// 移動地点を設定する
    /// </summary>
    private void GenerateNewRandomOffset()
    {
        var rand = Random.Range(-_randomMovementRange, _randomMovementRange);
        
        var x = rand - 50;
        var y = rand > -12f ? rand : -12f; // 下限
        var z = rand < 5 ? rand : 5f; // 右側の限界点
        
        _randomOffset = new Vector3(x, y, z);
    }

    #endregion
    

    /// <summary>
    /// 攻撃
    /// </summary>
    private void Attack()
    {
        // 敵はBPM200の拍に合うように弾を発射する
        // １・２・３・休、１・２・３・休、2小節使って弾を召喚する
        // 3小節目、4小節目で１・２・３・休、１・２・３・休のタイミングでプレイヤーが回避できるように弾が飛んでくる
        // ここで拍に合うように回避出来たか判定をとる。Perfect/Safe/Missの3段階とする
        // Perfectが拍にかなり正確にあっていた状態。Missはぶつかった場合。それ以外は全てSafeとする
        // 弾はプレイヤーにある程度近付くまでは追尾。そこからは直線に飛ぶようにする
        // オブジェクトプールを使わなくていいので、時間でDestroyしてほしい
    }

    private void OnDestroy()
    {
        _playerDisposable?.Dispose();
    }
}