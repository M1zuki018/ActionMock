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


    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody>();
        
        _currentPath = _paths[_currentPathIndex];
    }

    private void Update()
    {
        // 入力処理
        ProcessInput();
        
        // アクションの更新処理
        UpdateActionStates();
        
        // アクション中でなければ通常移動を処理
        if (!_isJumping && !_isSidestepping && !_isSliding)
        {
            // 自動前進
            MoveTowardPath();
        }
        else
        {
            // アクション中でも常に次のパスの方向を向く
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
    /// アクション状態の更新
    /// </summary>
    private void UpdateActionStates()
    {
        // サイドステップ処理
        if (_isSidestepping)
        {
            _actionTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_actionTimer / _sideStepDuration);
            
            // イージング関数を適用して滑らかな動きに
            float smoothT = Mathf.SmoothStep(0, 1, t);
            transform.position = Vector3.Lerp(_sideStepStartPos, _sideStepEndPos, smoothT);
            
            if (t >= 1.0f)
            {
                _isSidestepping = false;
                _isTransitioning = false;
            }
        }
        
        // スライディング処理
        if (_isSliding)
        {
            _actionTimer += Time.deltaTime;
            
            // 前方に加速移動
            transform.position += transform.forward * _slideSpeed * Time.deltaTime;
            
            if (_actionTimer >= _slideDuration)
            {
                _isSliding = false;
                _isTransitioning = false;
            }
        }
        
        // ジャンプ処理
        if (_isJumping)
        {
            _actionTimer += Time.deltaTime;
            
            // 上に力を加える
            transform.position += transform.up * _slideSpeed * Time.deltaTime;
            
            if (_actionTimer >= _slideDuration)
            {
                _isJumping = false;
                _isTransitioning = false;
            }
        }
    }
    

    /// <summary>
    /// 目的地への移動処理
    /// </summary>
    private void MoveTowardPath()
    {
        // 自動前進
        // TODO: フローゾーンなどで速度変化が突く場合ここでスピードを変える分岐を書く
        if (_currentPath != null)
        {
            if (!_isTransitioning)
            {
                // 新しい経路への遷移開始
                _startPosition = transform.position;
                _pathTransitionTimer = 0f;
                _isTransitioning = true;
            }
        
            // 240BPMの一小節は1秒
            float oneBarDuration = 60f / 200f * 4f;  // 1分 / BPM * 4拍
        
            // タイマー更新
            _pathTransitionTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_pathTransitionTimer / oneBarDuration);
        
            // 線形補間で移動
            transform.position = Vector3.Lerp(_startPosition, _currentPath.position, t);
        
            // 回転
            Vector3 directionToNode = (_currentPath.position - transform.position).normalized;
            directionToNode.y = 0f;
            if (directionToNode != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToNode);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 2f * Time.deltaTime);
            }
        
            // 一小節経過または十分近づいたら次の目標地点へ
            if (t >= 1.0f || Vector3.Distance(transform.position, _currentPath.position) < 0.1f)
            {
                NextPath();
                _isTransitioning = false;
            }
        }
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
        // ジャンプ状態にする
        _isJumping = true;
        _lastActionTime = Time.time;
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
        
        // アニメーター設定があれば、サイドステップアニメを再生
        if (_animator != null)
        {
            // _animator.SetTrigger("SideStep");
            Debug.Log("サイドステップ！" + (sideDirection == transform.right ? "右" : "左"));
        }
    }
    
    /// <summary>
    /// スライディングアクション
    /// </summary>
    private void Slide()
    {
        _isSliding = true;
        _actionTimer = 0f;
        _lastActionTime = Time.time;
    }
}
