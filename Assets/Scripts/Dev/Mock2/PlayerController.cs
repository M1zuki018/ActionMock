using UnityEngine;

/// <summary>
/// Playerの動きを制御する
/// </summary>
public class PlayerController : MonoBehaviour
{
    [SerializeField,Comment("前進するスピ―ド")] private float _forwardSpeed = 10f;
    [SerializeField,Comment("ジャンプにかける秒数")] private float _jumpDuration = 3f;
    
    private Animator _animator;
    private Rigidbody _rb;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        
    }
}
