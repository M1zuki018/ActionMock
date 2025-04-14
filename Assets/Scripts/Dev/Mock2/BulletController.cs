
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 敵の攻撃クラス
/// </summary>
public class BulletController : MonoBehaviour
{
    private Transform _player;
    private Transform _enemy;
    private GameManager2 _gameManager;
    
    [Header("移動設定")]
    [SerializeField] private float _speed = 50f;
    [SerializeField] private float _followDistance = 20f; // この距離以下になると追尾をやめて直線移動
    [SerializeField] private AnimationCurve _speedCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("視覚エフェクト")]
    [SerializeField] private TrailRenderer _trailRenderer;
    [SerializeField] private Light _bulletLight;
    [SerializeField] private Material _bulletMaterial;
    
    // 移動状態
    private bool _isFollowing = true;
    private bool _isMoving = false;
    private Vector3 _direction;
    private float _currentSpeed;
    
    // 時間管理
    private float _spawnTime;
    private float _moveStartTime;
    private bool _hasCollided = false;
    private bool _hasUpdatedScore = false;
    
    // BPM情報
    private const float BPM = 200f;
    private const float BEAT_INTERVAL = 60f / BPM;
    
    // エフェクト用
    private Effect _effectController;
    private Color _originalEmissionColor;
    private Sequence _pulseSequence;
    
    /// <summary>
    /// 弾の初期化
    /// </summary>
    public void Initialize(Transform player, Transform enemy)
    {
        _player = player;
        _enemy = enemy;
        _spawnTime = Time.time;
        _gameManager = FindObjectOfType<GameManager2>();
        _effectController = FindObjectOfType<Effect>();
        
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
        
        // トレイルレンダラーの設定
        if (_trailRenderer != null)
        {
            _trailRenderer.emitting = false; // 最初は無効
        }
        
        // 弾のライト設定
        if (_bulletLight != null)
        {
            _bulletLight.intensity = 0f;
        }
        
        // マテリアルのエミッションを保存
        if (_bulletMaterial != null && _bulletMaterial.HasProperty("_EmissionColor"))
        {
            _originalEmissionColor = _bulletMaterial.GetColor("_EmissionColor");
            _bulletMaterial.SetColor("_EmissionColor", _originalEmissionColor * 0.1f);
        }
        
        StartBulletSequence().Forget();
    }

    private async UniTask StartBulletSequence()
    {
        // 8拍待ってから動き始める
        for (int i = 0; i < 8; i++)
        {
            // 1拍ごとのパルスエフェクト
            PulseBullet();
            await UniTask.Delay((int)(BEAT_INTERVAL * 1000));
        }
        
        // 弾の発射
        _isMoving = true;
        _moveStartTime = Time.time;
        
        // エフェクト開始
        if (_trailRenderer != null)
        {
            _trailRenderer.emitting = true;
        }
        
        // 発光エフェクト
        if (_bulletMaterial != null && _bulletMaterial.HasProperty("_EmissionColor"))
        {
            DOTween.To(() => 0.1f, x => _bulletMaterial.SetColor("_EmissionColor", _originalEmissionColor * x), 2f, 0.5f)
                .SetEase(Ease.OutQuad);
        }
        
        // ライトを点灯
        if (_bulletLight != null)
        {
            _bulletLight.DOIntensity(3f, 0.3f).SetEase(Ease.OutQuad);
        }
        
        // パルス演出を停止
        _pulseSequence?.Kill();
    } 
    
    /// <summary>
    /// 弾のパルスエフェクト
    /// </summary>
    private void PulseBullet()
    {
        _pulseSequence?.Kill();
        _pulseSequence = DOTween.Sequence();
        
        // スケールのパルス
        _pulseSequence.Append(transform.DOScale(1.2f, BEAT_INTERVAL * 0.3f).SetEase(Ease.OutQuad));
        _pulseSequence.Append(transform.DOScale(1f, BEAT_INTERVAL * 0.2f).SetEase(Ease.InQuad));
        
        // 光のパルス
        if (_bulletLight != null)
        {
            _pulseSequence.Join(_bulletLight.DOIntensity(1f, BEAT_INTERVAL * 0.3f).SetEase(Ease.OutQuad));
            _pulseSequence.Join(_bulletLight.DOIntensity(0.2f, BEAT_INTERVAL * 0.2f).SetEase(Ease.InQuad));
        }
        
        // マテリアル発光のパルス
        if (_bulletMaterial != null && _bulletMaterial.HasProperty("_EmissionColor"))
        {
            _pulseSequence.Join(
                DOTween.To(() => 0.1f, 
                    x => _bulletMaterial.SetColor("_EmissionColor", _originalEmissionColor * x), 
                    0.5f, BEAT_INTERVAL * 0.3f).SetEase(Ease.OutQuad)
            );
            _pulseSequence.Join(
                DOTween.To(() => 0.5f, 
                    x => _bulletMaterial.SetColor("_EmissionColor", _originalEmissionColor * x), 
                    0.1f, BEAT_INTERVAL * 0.2f).SetEase(Ease.InQuad)
            );
        }
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
                // 直線移動に切り替え時のエフェクト
                PlayDirectionLockEffect().Forget();
            }
        }

        if (!_hasUpdatedScore && transform.position.x - _player.position.x > 0)
        {
            _hasUpdatedScore = true;
            UpdateScoreAndCombo(_gameManager, EvaluationEnum.Safe);
        }
        
        // 発射からの時間に基づいて速度を調整
        float moveTime = Time.time - _moveStartTime;
        float normalizedTime = Mathf.Clamp01(moveTime / 2f); // 2秒かけて加速
        _currentSpeed = _speed * _speedCurve.Evaluate(normalizedTime);
        
        // 弾を移動させる
        transform.position += _direction * _speed * Time.deltaTime;
    }
    
    /// <summary>
    /// 直線移動に切り替わる時のエフェクト
    /// </summary>
    private async UniTask PlayDirectionLockEffect()
    {
        // 方向固定時のフラッシュ
        if (_bulletLight != null)
        {
            _bulletLight.DOIntensity(5f, 0.1f).SetEase(Ease.OutFlash);
            await UniTask.Delay(100);
            _bulletLight.DOIntensity(2f, 0.3f).SetEase(Ease.OutQuad);
        }
        
        // トレイルの色を変更
        if (_trailRenderer != null)
        {
            _trailRenderer.startColor = new Color(1f, 0.5f, 0f); // オレンジ色に
        }
        
        // スケールを一瞬大きくして戻す
        transform.DOScale(1.5f, 0.1f).SetEase(Ease.OutQuad);
        await UniTask.Delay(100);
        transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack);
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
            
            // 弾の消滅エフェクト
            PlayDestroyEffect(evaluation).Forget();
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
        if (_effectController != null)
        {
            // 評価テキストを表示
            _effectController.EvaluationText(evaluation);
            
            // カメラシェイク
            if (evaluation == EvaluationEnum.Perfect)
            {
                _effectController.ShakeCamera(0.2f, 0.8f).Forget();
            }
            else if (evaluation == EvaluationEnum.Safe)
            {
                _effectController.ShakeCamera(0.1f, 0.3f).Forget();
            }
        }
    }
    
    /// <summary>
    /// 弾の消滅エフェクト
    /// </summary>
    private async UniTask PlayDestroyEffect(EvaluationEnum evaluation)
    {
        // トレイルレンダラーを無効化
        if (_trailRenderer != null)
        {
            _trailRenderer.emitting = false;
        }
        
        // ライトのフェードアウト
        if (_bulletLight != null)
        {
            _bulletLight.DOIntensity(0f, 0.2f);
        }
        
        // 弾のメッシュをフェードアウト
        if (_bulletMaterial != null && _bulletMaterial.HasProperty("_Color"))
        {
            Color originalColor = _bulletMaterial.GetColor("_Color");
            DOTween.To(() => originalColor, x => _bulletMaterial.SetColor("_Color", x), 
                new Color(originalColor.r, originalColor.g, originalColor.b, 0f), 0.3f);
            
            // エミッションも消す
            if (_bulletMaterial.HasProperty("_EmissionColor"))
            {
                DOTween.To(() => 1f, x => _bulletMaterial.SetColor("_EmissionColor", _originalEmissionColor * x), 
                    0f, 0.3f);
            }
        }
        
        // スケールを小さくして消える
        transform.DOScale(0f, 0.5f).SetEase(Ease.InBack);
        
        // 少し待ってから破棄
        await UniTask.Delay(500);
        Destroy(gameObject);
    }
}
