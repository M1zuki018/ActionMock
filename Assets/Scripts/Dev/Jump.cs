using System.Collections.Generic;
using DG.Tweening;
using UniRx;
using UnityEngine;

/// <summary>
/// タイミングに合わせてジャンプする部分のテスト
/// </summary>
public class Jump : MonoBehaviour
{
    [SerializeField] private float _jumpDuration = 1f;
    
    [SerializeField] private Transform _player;
    [SerializeField] private List<GameObject> _props;
    [SerializeField] private Ease _jumpEase;
    private ReactiveProperty<UseProp> _useProp = new ReactiveProperty<UseProp>(global::UseProp.Ground);
    public ReactiveProperty<UseProp> UseProp => _useProp;
    
    private int _index = 0;
    private float _playerHeight = 2f;

    private bool _isLane1Course; // TODO: 失敗成功判定をここに
    private List<int> _doubleTimeDurationNam = new List<int>{15};  
    
    [MethodButtonInspector]
    public void Test()
    {
        if (_index < _props.Count)
        {
            if (_index == 8 || _index == 10)
            {
                SlideMove(_props[_index].transform);
                _index++;
                Debug.Log(_index + ", " + _isLane1Course);
                return;
            }
            
            Vector3 endPos = _props[_index].transform.position;
            endPos.y += _playerHeight;
            
            ParabolicMove(
                start: _player.position, 
                end: endPos,
                duration: DoubleNamCheck() ? _jumpDuration : _jumpDuration * 2, // 15個目のオブジェクトなら二倍の時間かけて跳ぶ,
                height: 0.8f + _playerHeight);
        }
        
        _index++;
        Debug.Log(_index + ", " + _isLane1Course);
        
        // 空中ブロックへ
        if (_index == 15)
        {
            _useProp.Value = global::UseProp.Sky;
        }
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
    /// 二倍時間をかけて跳ぶオブジェクトのリストに登録されているか判定する
    /// </summary>
    private bool DoubleNamCheck()
    {
        foreach (var nam in _doubleTimeDurationNam)
        {
            if(nam == _index) return false;
        }
        
        return true;
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