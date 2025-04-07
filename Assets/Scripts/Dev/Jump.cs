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
    [SerializeField] private List<GameObject> _lane1;
    [SerializeField] private List<GameObject> _lane2;
    [SerializeField] private Ease _jumpEase;
    [SerializeField] private UseProp _useProp;
    private int _index = 0;
    private float _playerHeight = 2f;

    private bool _isLane1Course;
    
    [MethodButtonInspector]
    public void Test()
    {
        // 使用するObjectを指定
        var props = SetProp();
        
        if (_index < _props.Count)
        {
            Vector3 endPos = props[_index].transform.position;
            endPos.y += _playerHeight;
            
            ParabolicMove(
                start: _player.position, 
                end: endPos,
                duration: _jumpDuration,
                height: 0.8f + _playerHeight);
        }
        else
        {
            Vector3 endPos = new Vector3();
            
            if (_isLane1Course)
            {
                if (_index == 8 || _index == 10)
                {
                    SlideMove(_lane1[_index - 4].transform);
                    _index++;
                    Debug.Log(_index + ", " + _isLane1Course);
                    return;
                }
                
                endPos = _lane1[_index - 4].transform.position;
            }
            else
            {
                endPos = _lane2[_index % 4].transform.position;
            }
            
            endPos.y += _playerHeight;
            
            ParabolicMove(
                start: _player.position, 
                end: endPos,
                duration: _index != 15 ? _jumpDuration : _jumpDuration * 2, // 15個目のオブジェクトなら二倍の時間かけて跳ぶ
                height: 0.8f + _playerHeight);
        }
        
        _index++;
        Debug.Log(_index + ", " + _isLane1Course);

        // 空中ブロックへ
        if (_index == 16)
        {
            _useProp = UseProp.Sky;
            SetProp();
        }
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

        // レーン①か②を切り替える
        if (Input.GetKeyDown(KeyCode.D))
        {
            _isLane1Course = true; // Dキーを押したらレーン1へ
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            _isLane1Course = false; // Aキーを押したらレーン2へ
        }
    }

    /// <summary>
    /// 滑りあがる動き
    /// </summary>
    private void SlideMove(Transform target)
    {
        Vector3 startPos = target.position + Vector3.forward * -2;
        Vector3 endPos = startPos + Vector3.up * 10 + Vector3.forward * -2;
        
        Vector3[] path = { startPos, endPos };
        _player.DOPath(path, _jumpDuration / 2f, PathType.Linear).SetEase(Ease.Linear);
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