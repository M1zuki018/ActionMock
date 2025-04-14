using System;
using DG.Tweening;
using UniRx;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class Effect : MonoBehaviour
{
    [SerializeField] private PlayerController _playerCon;
    [SerializeField] private Image _shine;
    [SerializeField] private Volume _volume;
    private IDisposable _shineDisposable;
    
    private void Start()
    {
        // エネミーターン＝回避パートで光のフレームを非表示、攻撃パートで光のフレームを表示。
        _shineDisposable = _playerCon.IsEnemyTrun.Subscribe(isEnemyTurn =>
        {
            _shine.DOFade(isEnemyTurn ? 0 : 0.1f, 0.3f); // フレーム
            // TODO: 色収差エフェクト
        });
    }

    private void OnDestroy()
    {
        _shineDisposable?.Dispose();
    }
}
