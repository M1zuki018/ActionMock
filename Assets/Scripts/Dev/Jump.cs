using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// タイミングに合わせてジャンプする部分のテスト
/// </summary>
public class Jump : MonoBehaviour
{
    [SerializeField] private float _jumpDuration = 1f;
    
    [SerializeField] private Transform _player;
    [SerializeField] private List<GameObject> _props;
    [SerializeField] private List<GameObject> _skyProps;
    [SerializeField] private Ease _jumpEase;
    [SerializeField] private UseProp _useProp;
    private int _index = 0;
    private float _playerHeight = 2f;
    
    [MethodButtonInspector]
    public void Test()
    {
        // 使用するObjectを指定
        var props = SetProp();
        
        if (_index < _props.Count)
        {
            /*
            // プレイヤーの座標を次のプロップのポジションに移動させる
            Vector3 vector = _props[_index].transform.position;
            vector.y += 2; // プレイヤーの高さを足している
            _player.position = vector;
            */
            
            Vector3 endPos = props[_index].transform.position;
            endPos.y += _playerHeight;
            
            ParabolicMove(
                start: _player.position, 
                end: endPos,
                duration: _jumpDuration,
                height: 0.8f + _playerHeight);
        }
        _index++;
    }

    /// <summary>
    /// Enumに合わせて使用するPropを切り替える
    /// </summary>
    private List<GameObject> SetProp()
    {
        var props = _useProp switch
        {
            UseProp.Ground => _props,
            UseProp.Sky => _skyProps,
        };
        return props;
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

/// <summary>
/// 使用するプロップリスト
/// </summary>
public enum UseProp
{
    Ground,
    Sky,
}