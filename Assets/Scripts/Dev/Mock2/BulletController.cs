
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 敵の攻撃クラス
/// </summary>
public class BulletController : MonoBehaviour
{
    private Transform _player;
    private Transform _enemy;
    
    private float _speed = 50f;
    private float _followDistance = 20f; // この距離以下になると追尾をやめて直線移動
    private bool _isFollowing = true;
    private bool _isMoving = false;
    private Vector3 _direction;
    
    private float _spawnTime;
    private bool _hasCollided = false;
    
    // BPM情報
    private const float BPM = 200f;
    private const float BEAT_INTERVAL = 60f / BPM;
    
    /// <summary>
    /// 弾の初期化
    /// </summary>
    public void Initialize(Transform player, Transform enemy)
    {
        _player = player;
        _enemy = enemy;
        _spawnTime = Time.time;
        
        // 初期の方向を設定
        if (_player != null)
        {
            _direction = (_player.position - transform.position).normalized;
        }
        else
        {
            _direction = Vector3.right; // デフォルトは右方向
        }
        
        // 弾の向きを進行方向に合わせる
        transform.forward = _direction;
        
        MoveMode().Forget();
    }

    private async UniTask MoveMode()
    {
        await UniTask.WaitForSeconds(BEAT_INTERVAL * 8); // 8拍待って動く
        _isMoving = true;
    } 
    
    private void Update()
    {
        if (_player == null || _hasCollided) return;

        if (!_isMoving)
        {
            transform.position = new Vector3(_enemy.transform.position.x, transform.position.y, transform.position.z);
        }
        
        // プレイヤーとの距離を計算
        float distanceToPlayer = Vector3.Distance(transform.position, _player.position);
        
        // 追尾モード中はプレイヤーの方向を常に更新
        if (_isFollowing)
        {
            _direction = (_player.position - transform.position).normalized;
            transform.forward = _direction;
            
            // 設定距離以下になったら追尾をやめて直線移動に切り替え
            if (distanceToPlayer <= _followDistance)
            {
                _isFollowing = false;
            }
        }
        
        // 弾を移動させる
        transform.position += _direction * _speed * Time.deltaTime;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (_hasCollided) return;
        
        // プレイヤーとの衝突を検出
        if (other.CompareTag("Player"))
        {
            _hasCollided = true;
            
            // 拍のタイミングとの差を計算して評価
            EvaluationEnum evaluation = EvaluateHitTiming();
            
            // GameManagerを取得
            GameManager2 gameManager = GameObject.FindObjectOfType<GameManager2>();
            if (gameManager != null)
            {
                // 評価に応じてスコアとコンボを更新
                UpdateScoreAndCombo(gameManager, evaluation);
            }
            
            // 衝突時のエフェクトなどを表示
            ShowHitEffect(evaluation);
            
            // 弾を非表示にする
            gameObject.SetActive(false);
            
            // 弾を破棄
            Destroy(gameObject, 0.1f);
        }
    }
    
    /// <summary>
    /// 拍のタイミングとの差から評価を算出
    /// </summary>
    private EvaluationEnum EvaluateHitTiming()
    {
        // 経過時間から何拍目かを計算
        float elapsedTime = Time.time - _spawnTime;
        
        // 2小節（8拍）待った後、3小節目からが回避タイミング
        float avoidanceStartTime = BEAT_INTERVAL * 8;
        
        // 回避タイミングからの経過時間
        float timeFromAvoidance = elapsedTime - avoidanceStartTime;
        
        // 最も近い拍のタイミングを計算
        float beatPosition = timeFromAvoidance / BEAT_INTERVAL;
        int nearestBeat = Mathf.RoundToInt(beatPosition);
        float timeDifference = Mathf.Abs(beatPosition - nearestBeat) * BEAT_INTERVAL;
        
        // 拍のタイミングとの差から評価を決定
        // Perfect: 0.05秒以内
        // Miss: 衝突した場合（この関数が呼ばれた時点でMiss）
        // Safe: それ以外
        
        if (timeDifference <= 0.05f)
        {
            return EvaluationEnum.Perfect;
        }
        else
        {
            return EvaluationEnum.Safe;
        }
    }
    
    /// <summary>
    /// 評価に応じてスコアとコンボを更新
    /// </summary>
    private void UpdateScoreAndCombo(GameManager2 gameManager, EvaluationEnum evaluation)
    {
        switch (evaluation)
        {
            case EvaluationEnum.Perfect:
                // Perfectの場合、スコア加算とコンボ増加
                gameManager.Score.Value += 100 * (gameManager.ComboCount.Value + 1);
                gameManager.ComboCount.Value++;
                break;
                
            case EvaluationEnum.Safe:
                // Safeの場合、少しスコア加算とコンボ維持
                gameManager.Score.Value += 50 * (gameManager.ComboCount.Value + 1);
                gameManager.ComboCount.Value++;
                break;
                
            default: // Miss
                // Missの場合、コンボリセット
                gameManager.ComboCount.Value = 0;
                break;
        }
    }
    
    /// <summary>
    /// 衝突時のエフェクト表示
    /// </summary>
    private void ShowHitEffect(EvaluationEnum evaluation)
    {
        // エフェクトがあればここで表示
        // Effect コンポーネントを探してテキスト更新
        Effect effect = FindObjectOfType<Effect>();
        if (effect != null)
        {
            // EvaluationTextメソッドがprivateなので、リフレクションでアクセス
            effect.EvaluationText(evaluation);
        }
    }
}
