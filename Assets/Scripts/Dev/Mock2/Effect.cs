using System;
using System.Collections.Generic;
using DG.Tweening;
using UniRx;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class Effect : MonoBehaviour
{
    [SerializeField] private GameManager2 _gameManager;
    [SerializeField] private PlayerController _playerCon;
    [SerializeField] private Volume _volume;
    
    [SerializeField] private Image _shine;
    [Header("コンボ")]
    [SerializeField] private Text _comboText;
    [SerializeField] private RectTransform _comboContainer;
    [SerializeField] private float _comboPopDuration = 0.3f;
    [SerializeField] private Text _evaluationText;
    
    // ポストプロセスエフェクト用
    private ChromaticAberration _chromaticAberration;
    private Vignette _vignette;
    private DepthOfField _depthOfField;
    private ColorAdjustments _colorAdjustments;
    
    
    private Sequence _comboSequence;
    private IDisposable _disposable;
    private IDisposable _comboSubscribe;
    
    // DOTweenアニメーション用のSequenceを保持
    private Dictionary<string, Sequence> _tweenSequences = new Dictionary<string, Sequence>();
    
    private void Start()
    {
        InitializePostProcessing();
        InitializeUI();
        SubscribeToEvents();
    }
    
    /// <summary>
    /// UIの初期化
    /// </summary>
    private void InitializeUI()
    {
        _comboText.DOFade(0, 0.2f);
        // コンボテキストの初期状態
        if (_comboContainer != null)
        {
            _comboContainer.localScale = Vector3.one;
        }
    }

    /// <summary>
    /// イベント購読の設定
    /// </summary>
    private void SubscribeToEvents()
    {
        // エネミーターン＝回避パートで光のフレームを非表示、攻撃パートで光のフレームを表示。
        _disposable = _playerCon.IsEnemyTurn.Subscribe(isEnemyTurn =>
        {
            _shine.DOFade(isEnemyTurn ? 0 : 0.1f, 0.3f); // フレーム
            // TODO: GlobalVolumeを使った色収差エフェクト
        });
        
        // コンボ数の書き換え　購読解除処理を追加して
        _comboSubscribe = _gameManager.ComboCount.Subscribe(UpdateComboText);
    }

    /// <summary>
    /// ポストプロセスエフェクトの初期化
    /// </summary>
    private void InitializePostProcessing()
    {
        // ボリュームからエフェクトコンポーネントを取得
        if (_volume != null)
        {
            _volume.profile.TryGet(out _chromaticAberration);
            _volume.profile.TryGet(out _vignette);
            _volume.profile.TryGet(out _depthOfField);
            _volume.profile.TryGet(out _colorAdjustments);
            
            // 初期状態を設定
            if (_chromaticAberration != null) _chromaticAberration.intensity.value = 0f;
            if (_vignette != null) _vignette.intensity.value = 0.2f;
        }
    }
    
    /// <summary>
    /// コンボテキストの更新とアニメーション
    /// </summary>
    private void UpdateComboText(int combo)
    {
        // テキスト更新
        _comboText.text = combo.ToString("00");
        
        // 既存のアニメーションをキル
        _comboSequence?.Kill();
        
        // シーケンスを作成
        _comboSequence = DOTween.Sequence();
        
        // ポップアニメーション
        _comboSequence.Append(_comboContainer.DOScale(1.05f, _comboPopDuration * 0.5f).SetEase(Ease.OutBack));
        _comboSequence.Append(_comboContainer.DOScale(1f, _comboPopDuration * 0.5f).SetEase(Ease.InOutQuad));
        
        // コンボ数に応じて色を変える
        Color targetColor = Color.white;
        if (combo >= 50) targetColor = Color.yellow;
        else if (combo >= 30) targetColor = Color.cyan;
        else if (combo >= 10) targetColor = Color.green;
        
        _comboSequence.Join(_comboText.DOColor(targetColor, _comboPopDuration).SetEase(Ease.OutFlash));
        
        // シーケンス実行
        _comboSequence.Play();
    }

    /// <summary>
    /// 評価のテキストを書き換える。適切なメソッドから使ってほしい
    /// </summary>
    public void EvaluationText(EvaluationEnum evaluation)
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
        
        _evaluationText.text = evaluation.ToString();
        _evaluationText.DOFade(1, 0.2f); // テキスト表示
    }

    private void OnDestroy()
    {
        _disposable?.Dispose();
        _comboSubscribe?.Dispose();
    }
}