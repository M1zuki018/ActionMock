using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// タイミングに合わせてジャンプする部分のテスト
/// </summary>
public class Jump : MonoBehaviour
{
    [SerializeField] private Transform _player;
    [SerializeField] private List<GameObject> _props;
    [SerializeField] private Ease _jumpEase;
    private int _index = 0;
    private float _playerHeight = 2f;
    
    [MethodButtonInspector]
    public void Test()
    {
        if (_index < _props.Count)
        {
            /*
            // プレイヤーの座標を次のプロップのポジションに移動させる
            Vector3 vector = _props[_index].transform.position;
            vector.y += 2; // プレイヤーの高さを足している
            _player.position = vector;
            */
            
            Vector3 endPos = _props[_index].transform.position;
            endPos.y += _playerHeight;
            
            ParabolicMove(
                start: _player.position, 
                end: endPos,
                duration: 1f,
                height: 0.8f + _playerHeight);
        }
        _index++;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Test(); // キーボードからの発火
        }
    }

    /// <summary>
    /// 放物戦を描くようなジャンプ軌道
    /// </summary>
    private void ParabolicMove(Vector3 start, Vector3 end, float duration, float height)
    {
        _player.position = start;
        
        // 中間点を計算
        Vector3 midPoint = (start + end) / 2f;
        midPoint.y += height;
        
        // 経路
        Vector3[] path = {midPoint, end};
        
        _player.DOPath(path, duration, PathType.CatmullRom).SetEase(_jumpEase);
    }
}