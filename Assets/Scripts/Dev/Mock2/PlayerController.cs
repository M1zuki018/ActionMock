using UnityEngine;

/// <summary>
/// Playerの動きを制御する
/// </summary>
public class PlayerController : MonoBehaviour
{
    [SerializeField,Comment("前進するスピ―ド")] private float _forwardSpeed = 10f;
    [SerializeField,Comment("ジャンプにかける秒数")] private float _jumpDuration = 3f;
    [SerializeField] private Transform[] _paths; // Prop
    
    private Animator _animator;
    private Rigidbody _rb;
    
    private Transform _currentPath; // 現在目指しているPropの位置
    private int _currentPathIndex = 0;
    private float _pathTransitionTimer = 0f;
    private Vector3 _startPosition;
    private bool _isTransitioning = false;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody>();
        
        _currentPath = _paths[_currentPathIndex];
    }

    private void Update()
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
}
