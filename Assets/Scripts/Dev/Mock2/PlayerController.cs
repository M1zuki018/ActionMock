using UnityEngine;

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
                _startPosition = _rhythmBasedPosition; // アクションオフセットを含まない位置から計算
                _pathTransitionTimer = 0f;
                _isTransitioning = true;
            }
        
            // 240BPMの一小節は1秒
            float oneBarDuration = 60f / 200f * 4f;  // 1分 / BPM * 4拍
        
            // タイマー更新
            _pathTransitionTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_pathTransitionTimer / oneBarDuration);
        
            // 線形補間で移動（リズムベースの位置のみ更新）
            _rhythmBasedPosition = Vector3.Lerp(_startPosition, _currentPath.position, t);
        
            // 一小節経過または十分近づいたら次の目標地点へ
            if (t >= 1.0f || Vector3.Distance(_rhythmBasedPosition, _currentPath.position) < 0.1f)
            {
                NextPath();
                _isTransitioning = false;
            }
        }
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
            }
        }
        
        // ジャンプ処理
        if (_isJumping)
        {
            _actionTimer += Time.deltaTime;
            
            // 上方向への移動をオフセットとして計算
            Vector3 jumpOffset = transform.up * _jumpForce * Time.deltaTime;
            _actionOffset += jumpOffset;
            
            // 一定時間後に下降
            if (_actionTimer > _slideDuration * 0.5f)
            {
                // 落下
                _actionOffset -= transform.up * _jumpForce * 1.5f * Time.deltaTime;
            }
            
            if (_actionTimer >= _slideDuration)
            {
                // ジャンプ完了後、Y軸方向の位置を元に戻す（水平方向の移動は保持）
                _actionOffset.y = 0;
                _rhythmBasedPosition += new Vector3(_actionOffset.x, 0, _actionOffset.z);
                _actionOffset = Vector3.zero;
                _isJumping = false;
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