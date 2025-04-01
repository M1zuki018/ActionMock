using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// タイミングに合わせてジャンプする部分のテスト
/// </summary>
public class Jump : MonoBehaviour
{
    [SerializeField] private Transform _player;
    [SerializeField] private List<GameObject> _props;
    private int _index;
    
    [MethodButtonInspector]
    public void Test()
    {
        if (_index < _props.Count)
        {
            // プレイヤーの座標を次のプロップのポジションに移動させる
            Vector3 vector = _props[_index].transform.position;
            vector.y += 2; // プレイヤーの高さを足している
            _player.position = vector;
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
}