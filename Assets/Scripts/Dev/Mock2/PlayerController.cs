using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Playerの動きを制御する
/// </summary>
public class PlayerController : MonoBehaviour
{
    [SerializeField,Comment("前進するスピ―ド")] private float _moveSpeed = 15f; 
    [SerializeField] private Transform[] _paths; // Prop
    
    [Header("アクション設定")]
    [SerializeField] private float _jumpForce = 500f;
    [SerializeField] private float _sideStepDistance = 3f;
    [SerializeField] private float _sideStepDuration = 0.3f;
    [SerializeField] private float _slideDuration = 0.5f;
    [SerializeField] private float _slideSpeed = 20f;
    [SerializeField] private float _actionCooldown = 0.5f;
    private float _lastActionTime = -10f; // 初期値を負の値にして、ゲーム開始直後からアクションができるようにする
    
    [Header("音")]
    [SerializeField] private AudioClip _jumpSound;
    
    public ReactiveProperty<bool> IsEnemyTurn = new ReactiveProperty<bool>(true);
    
    private Animator _animator;
    private Rigidbody _rb;
    
    // 移動処理関連の変数
    private Transform _currentPath; // 現在目指しているPropの位置
    private int _currentPathIndex = 0;
    private float _pathTransitionTimer = 0f;
    private Vector3 _startPosition;
    private bool _isTransitioning = false;
    
    // アクション状態管理
    private bool _isJumping = false;
    private bool _isSidestepping = false;
    private bool _isSliding = false;
    private Vector3 _sideStepStartPos;
    private Vector3 _sideStepEndPos;
    private float _actionTimer = 0f;

    // アクション中の位置オフセット
    private Vector3 _actionOffset = Vector3.zero;
    // リズムベースの移動計算用の位置（アクションによる位置変化を含まない）
    private Vector3 _rhythmBasedPosition;


    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody>();
        
        _currentPath = _paths[_currentPathIndex];
        _rhythmBasedPosition = transform.position;
    }

    private void Update()
    {
        // 入力処理
        ProcessInput();
        
        // リズムベースの位置計算（常に行う）
        UpdateRhythmBasedPosition();
        
        // アクションの更新処理
        UpdateActionStates();
        
        // 最終的な位置を設定
        ApplyFinalPosition();
        
        // 常に進行方向を向く
        if (_currentPath != null)
        {
            Vector3 directionToNode = (_currentPath.position - transform.position).normalized;
            directionToNode.y = 0f;
            if (directionToNode != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToNode);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 2f * Time.deltaTime);
            }
        }
    }
    
    /// <summary>
    /// 入力処理
    /// </summary>
    private void ProcessInput()
    {
        // アクションのクールダウンチェック
        bool canPerformAction = Time.time > _lastActionTime + _actionCooldown;
        
        if (canPerformAction && !_isJumping && !_isSidestepping && !_isSliding)
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                Jump();
            }
            else if (Input.GetKeyDown(KeyCode.S)) 
            {
                SideStep();
            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                Slide();
            }
        }
    }
    
    /// <summary>
    /// リズムベースの位置を更新する（BPMに同期した移動）
    /// </summary>
    private void UpdateRhythmBasedPosition()
    {
        if (_currentPath != null)
        {
            if (!_isTransitioning)
            {
                // 新しい経路への遷移開始
                _startPosition = transform.position; // アクションオフセットを含まない位置から計算
                _pathTransitionTimer = 0f;
                _isTransitioning = true;
            }
        
            // 240BPMの一小節は1秒
            float oneBarDuration = 60f / GameConst.BPM * 4f;  // 1分 / BPM * 4拍
        
            // タイマー更新
            _pathTransitionTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_pathTransitionTimer / oneBarDuration);
    
            // 現在位置から目標点への方向ベクトルを計算
            Vector3 directionToTarget = (_currentPath.position - _startPosition).normalized;
        
            // 現在の経過時間に基づいて進行距離を計算
            float totalDistance = Vector3.Distance(_startPosition, _currentPath.position);
            float currentDistance = totalDistance * t;
        
            // 新しい位置 = 開始位置 + (方向ベクトル × 距離)
            _rhythmBasedPosition = _startPosition + (directionToTarget * currentDistance);
        
            // 一小節経過または十分近づいたら次の目標地点へ
            if (t >= 1.0f || Vector3.Distance(_rhythmBasedPosition, _currentPath.position) < 0.1f)
            {
                NextPath();
                _isTransitioning = false;
            }
        }
    }
    
    /// <summary>
    /// アクション後に移動遷移を初期化する
    /// </summary>
    private void ResetPathTransition()
    {
        // 現在の位置から新しく補間を開始
        _startPosition = _rhythmBasedPosition;
        _pathTransitionTimer = 0f;
        _isTransitioning = true;
        
        // タイマーをBPMに合わせた適切な位置に調整
        // これにより、アクション後も音楽のビートに合わせた移動を維持
        float oneBarDuration = 60f / GameConst.BPM * 4f;
        float beatFraction = Mathf.Repeat(Time.time, oneBarDuration) / oneBarDuration;
        _pathTransitionTimer = beatFraction * oneBarDuration;
        
        // デバッグログ
        Debug.Log($"移動を再開始 - 現在位置: {_rhythmBasedPosition}, 目標: {_currentPath.position}, ビート位置: {beatFraction:F2}");
    }
    
    /// <summary>
    /// アクションによる位置の変化を更新
    /// </summary>
    private void UpdateActionStates()
    {
        // アクション実行中でなければオフセットをリセット
        if (!_isJumping && !_isSidestepping && !_isSliding)
        {
            _actionOffset = Vector3.zero;
            return;
        }
        
        // サイドステップ処理
        if (_isSidestepping)
        {
            _actionTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_actionTimer / _sideStepDuration);
            
            // イージング関数を適用して滑らかな動きに
            float smoothT = Mathf.SmoothStep(0, 1, t);
            
            // サイドステップのオフセットを計算
            Vector3 sideStepOffset = Vector3.Lerp(Vector3.zero, _sideStepEndPos - _sideStepStartPos, smoothT);
            _actionOffset = sideStepOffset;
            
            if (t >= 1.0f)
            {
                // サイドステップ完了後、新しい位置をリズムベースの位置に反映
                _rhythmBasedPosition += _actionOffset;
                _actionOffset = Vector3.zero;
                _isSidestepping = false;
                
                // 移動遷移を新しい位置から再開始
                ResetPathTransition();
            }
        }
        
        // スライディング処理
        if (_isSliding)
        {
            _actionTimer += Time.deltaTime;
            
            // スライディングによる前方への移動をオフセットとして計算
            Vector3 slideOffset = transform.forward * _slideSpeed * Time.deltaTime;
            _actionOffset += slideOffset;
            
            if (_actionTimer >= _slideDuration)
            {
                // スライディング完了後、新しい位置をリズムベースの位置に反映
                _rhythmBasedPosition += _actionOffset;
                _actionOffset = Vector3.zero;
                _isSliding = false;
                
                // 移動遷移を新しい位置から再開始
                ResetPathTransition();
            }
        }
        
        // ジャンプ処理
        if (_isJumping)
        {
            _actionTimer += Time.deltaTime;
            
            // 上方向への移動をオフセットとして計算
            Vector3 jumpOffset = transform.up * _jumpForce * Time.deltaTime;
            _actionOffset += jumpOffset;
            _currentPath.position += jumpOffset;
            
            if (_actionTimer >= _slideDuration)
            {
                _isJumping = false;
                
                // 移動遷移を新しい位置から再開始
                ResetPathTransition();
            }
        }
    }
    
    /// <summary>
    /// 最終的な位置を適用する
    /// </summary>
    private void ApplyFinalPosition()
    {
        // リズムベースの位置 + アクションによるオフセット = 最終位置
        transform.position = _rhythmBasedPosition + _actionOffset;
    }

    /// <summary>
    /// 目的地を更新する
    /// </summary>
    private void NextPath()
    {
        // 8小節ごとにターン切り替え
        if (_currentPathIndex % 8 == 7)
        {
            IsEnemyTurn.Value = !IsEnemyTurn.Value; // 回避パートとアタックパートを入れ替える
            _animator.SetBool("Attack", !IsEnemyTurn.Value);
            Debug.LogWarning(IsEnemyTurn.Value ? "敵のターン" : "自分のターン");
        }
            
        _currentPathIndex++;
        if (_currentPathIndex < _paths.Length)
        {
            _currentPath = _paths[_currentPathIndex];
        }
    }
    
    /// <summary>
    /// ジャンプアクション
    /// </summary>
    private void Jump()
    {
        _isJumping = true;
        _actionTimer = 0f;
        _lastActionTime = Time.time;
        
        // カメラエフェクト追加
        CameraController2 cameraController = FindObjectOfType<CameraController2>();
        if (cameraController != null)
        {
            // FOVを広げてジャンプ感を演出
            cameraController.ChangeFOV(75f, 0.3f).Forget();
        }
    
        // 効果音再生
        AudioController.Instance.PlaySE(_jumpSound);
        
        Debug.Log("ジャンプ！");
    }
    
    /// <summary>
    /// サイドステップアクション
    /// </summary>
    private void SideStep()
    {
        _isSidestepping = true;
        _actionTimer = 0f;
        _lastActionTime = Time.time;
        
        // ランダムに左右どちらかにサイドステップ
        Vector3 sideDirection = Random.value > 0.5f ? transform.right : -transform.right;
        
        _sideStepStartPos = transform.position;
        _sideStepEndPos = transform.position + sideDirection * _sideStepDistance;
        
        _currentPath.position +=  sideDirection * _sideStepDistance;
        
        Debug.Log("サイドステップ！" + (sideDirection == transform.right ? "右" : "左"));
    }
    
    /// <summary>
    /// スライディングアクション
    /// </summary>
    private void Slide()
    {
        _isSliding = true;
        _actionTimer = 0f;
        _lastActionTime = Time.time;
        
        Debug.Log("スライディング！");
    }
}