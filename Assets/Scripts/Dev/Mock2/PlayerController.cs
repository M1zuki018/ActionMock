using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Playerの動きを制御する
/// </summary>
public class PlayerController : MonoBehaviour
{
    [SerializeField,Comment("前進するスピ―ド")] private float _forwardSpeed = 10f;
    [SerializeField,Comment("ジャンプにかける秒数")] private float _jumpDuration = 3f;
    [SerializeField] private Transform[] _paths; // Prop
    private Transform _currentPath; // 現在目指しているPropの位置
    private int _currentPathIndex = 0;
    
    private Animator _animator;
    private Rigidbody _rb;

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
        transform.position += transform.forward * _forwardSpeed * Time.deltaTime;

        if (_currentPath != null)
        {
            // ベクトルを求める
            Vector3 directionToNode = (_currentPath.position - transform.position).normalized;
            directionToNode.y = 0f; // 並行移動のみ。上下の移動は自動には行わないようにしてみる
            
            // 回転
            if (directionToNode != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToNode);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 2f * Time.deltaTime); // 徐々に
            }
            
            // 目標地点更新
            if (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), 
                    new Vector3(_currentPath.position.x, 0, _currentPath.position.z)) < 5f)
            {
                _currentPathIndex++;
                _currentPath = _paths[_currentPathIndex];
            }
        }
        
        // 平行移動
        Vector3 right = Vector3.Cross(Vector3.up, transform.forward).normalized;
        Vector3 horizontalMove = right * _forwardSpeed * Time.deltaTime;
        transform.position += horizontalMove;
    }
}
