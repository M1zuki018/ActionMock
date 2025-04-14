using System;
using DG.Tweening;
using UniRx;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class Effect : MonoBehaviour
{
    [SerializeField] private GameManager2 _gameManager;
    [SerializeField] private PlayerController _playerCon;
    [SerializeField] private Volume _volume;
    
    [SerializeField] private Image _shine;
    [SerializeField] private Text _comboText;
    [SerializeField] private Text _evaluationText;
    
    private IDisposable _disposable;
    
    private void Start()
    {
       // エネミーターン＝回避パートで光のフレームを非表示、攻撃パートで光のフレームを表示。
        _disposable = _playerCon.IsEnemyTrun.Subscribe(isEnemyTurn =>
        {
            _shine.DOFade(isEnemyTurn ? 0 : 0.1f, 0.3f); // フレーム
            // TODO: GlobalVolumeを使った色収差エフェクト
        });
        
        // コンボ数の書き換え　購読解除処理を追加して
        _gameManager.ComboCount.Subscribe(combo => _comboText.text = combo.ToString("00"));
    }

    /// <summary>
    /// 評価のテキストを書き換える。適切なメソッドから使ってほしい
    /// </summary>
    private void EvaluationText(EvaluationEnum evaluation)
    {
        if (evaluation == EvaluationEnum.Perfect)
        {
            _evaluationText.color = Color.yellow;
        }
        else if (evaluation == EvaluationEnum.Safe)
        {
            _evaluationText.color = Color.cyan;
        }
        else
        {
            _evaluationText.color = Color.gray;
        }
        
        _comboText.text = evaluation.ToString();
    }

    private void OnDestroy()
    {
        _disposable?.Dispose();
    }
}