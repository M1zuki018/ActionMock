using System;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Mock3
{


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
        private bool _isFirst;

        private void Awake()
        {
            _playerCon = _player.GetComponent<PlayerController>();
        }

        private void Start()
        {
            _playerDisposable = _playerCon.IsEnemyTurn.Subscribe(isEnemyTurn =>
            {
                if (isEnemyTurn)
                {
                    _isFirst = true;
                    Attack(); // 自分のターンの時に攻撃を行う
                }
            });

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
        private async void Attack()
        {
            float beatInterval = 60f / GameConst.BPM;

            // 弾の生成タイミングを設定（最初の2小節で弾を召喚）
            // 1小節目: 1・2・3・休
            await SpawnBulletWithDelay(beatInterval * 0); // 1拍目
            await SpawnBulletWithDelay(beatInterval); // 2拍目
            await SpawnBulletWithDelay(beatInterval); // 3拍目
            await UniTask.WaitForSeconds(beatInterval); // 休符
            // 2小節目: 1・2・3・休
            await SpawnBulletWithDelay(beatInterval); // 1拍目
            await SpawnBulletWithDelay(beatInterval); // 2拍目
            await SpawnBulletWithDelay(beatInterval); // 3拍目
            await UniTask.WaitForSeconds(beatInterval); // 休符

            await UniTask.WaitForSeconds(beatInterval * 8); // 休符

            if (_isFirst)
            {
                _isFirst = false;
                Attack(); // 1回目だったらもう一度繰り返す
            }
        }

        /// <summary>
        /// 指定した遅延時間後に弾を生成する
        /// </summary>
        private async UniTask SpawnBulletWithDelay(float delay)
        {
            await UniTask.WaitForSeconds(delay); // UniTaskで遅延処理
            SpawnBullet(); // 弾を生成
        }

        /// <summary>
        /// 弾を生成する
        /// </summary>
        private void SpawnBullet()
        {
            // 弾をインスタンス化
            GameObject bullet = Instantiate(_bulletPrefab, transform.position, Quaternion.identity);
            bullet.transform.localScale = Vector3.one / 2;

            // BulletControllerをアタッチ
            BulletController bulletController = bullet.GetComponent<BulletController>();
            if (bulletController == null)
            {
                // BulletControllerがない場合は追加
                bulletController = bullet.AddComponent<BulletController>();
            }

            // 弾のパラメータを設定
            bulletController.Initialize(_player, transform); // プレイヤー、速度、追尾距離を設定

            // 10秒後に弾を破棄
            Destroy(bullet, 10f);
        }

        private void OnDestroy()
        {
            _playerDisposable?.Dispose();
        }
    }
}