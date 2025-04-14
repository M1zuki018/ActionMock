using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UIのアニメーションと表示を管理
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private GameManager2 _gameManager;
    [SerializeField] private PlayerController _playerController;
    
    [Header("UI要素")]
    [SerializeField] private RectTransform _scorePanel;
    [SerializeField] private RectTransform _comboPanel;
    [SerializeField] private RectTransform _gameInfoPanel;
    [SerializeField] private Image _overlayPanel;
    [SerializeField] private Text _turnIndicator;
    
    [Header("ターン表示")]
    [SerializeField] private float _turnIndicatorDuration = 1.5f;
    [SerializeField] private Color _playerTurnColor = new Color(0.2f, 0.6f, 1f);
    [SerializeField] private Color _enemyTurnColor = new Color(1f, 0.3f, 0.3f);
    
    // トランジション設定
    [Header("トランジション")]
    [SerializeField] private float _transitionDuration = 0.5f;
    [SerializeField] private Ease _inEase = Ease.OutBack;
    [SerializeField] private Ease _outEase = Ease.InBack;
    
    // BPM情報
    private const float BPM = 200f;
    private const float BEAT_INTERVAL = 60f / BPM;
    
    // Tweenシーケンス管理
    private Dictionary<string, Sequence> _tweens = new Dictionary<string, Sequence>();
    private IDisposable _turnSubscription;
    private IDisposable _comboSubscription;
    private float _gameStartTime;
    
    private void Awake()
    {
        _gameStartTime = Time.time;
        
        // 初期状態でUIをセットアップ
        InitializeUI();
    }
    
    private void Start()
    {
        // ターン変更の購読
        _turnSubscription = _playerController.IsEnemyTurn.Subscribe(isEnemyTurn =>
        {
            ShowTurnIndicator(isEnemyTurn).Forget();
        });
        
        // コンボ変更の購読
        _comboSubscription = _gameManager.ComboCount.Subscribe(combo =>
        {
            PulseComboPanel(combo);
        });
        
        // ゲーム開始のアニメーション
        PlayGameStartAnimation().Forget();
    }
    
    private void Update()
    {
        // リズムに合わせたUIのアニメーション
        RhythmicUIAnimations();
    }
    
    /// <summary>
    /// UI要素の初期化
    /// </summary>
    private void InitializeUI()
    {
        // 各パネルの初期スケールとアルファを設定
        if (_scorePanel != null)
        {
            _scorePanel.localScale = Vector3.zero;
        }
        
        if (_comboPanel != null)
        {
            _comboPanel.localScale = Vector3.zero;
        }
        
        if (_gameInfoPanel != null)
        {
            _gameInfoPanel.localScale = Vector3.zero;
        }
        
        // オーバーレイパネルを設定
        if (_overlayPanel != null)
        {
            _overlayPanel.color = new Color(0, 0, 0, 1); // 黒でフェードイン開始
        }
        
        // ターン表示を初期化
        if (_turnIndicator != null)
        {
            _turnIndicator.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// ゲーム開始時のアニメーション
    /// </summary>
    private async UniTask PlayGameStartAnimation()
    {
        // 黒画面からフェードイン
        if (_overlayPanel != null)
        {
            await _overlayPanel.DOFade(0, 1f).SetEase(Ease.OutSine).AsyncWaitForCompletion();
        }
        
        // スコアパネルのアニメーション
        if (_scorePanel != null)
        {
            Sequence scoreSeq = DOTween.Sequence();
            scoreSeq.Append(_scorePanel.DOScale(1.2f, 0.5f).SetEase(Ease.OutBack));
            scoreSeq.Append(_scorePanel.DOScale(1f, 0.3f).SetEase(Ease.OutQuad));
            
            _tweens["scorePanel"] = scoreSeq;
        }
        
        await UniTask.Delay(200); // 少し待機
        
        // コンボパネルのアニメーション
        if (_comboPanel != null)
        {
            Sequence comboSeq = DOTween.Sequence();
            comboSeq.Append(_comboPanel.DOScale(1.2f, 0.5f).SetEase(Ease.OutBack));
            comboSeq.Append(_comboPanel.DOScale(1f, 0.3f).SetEase(Ease.OutQuad));
            
            _tweens["comboPanel"] = comboSeq;
        }
        
        await UniTask.Delay(200); // 少し待機
        
        // ゲーム情報パネルのアニメーション
        if (_gameInfoPanel != null)
        {
            Sequence infoSeq = DOTween.Sequence();
            infoSeq.Append(_gameInfoPanel.DOScale(1.2f, 0.5f).SetEase(Ease.OutBack));
            infoSeq.Append(_gameInfoPanel.DOScale(1f, 0.3f).SetEase(Ease.OutQuad));
            
            _tweens["infoPanel"] = infoSeq;
        }
        
        // 最初のターン表示
        await UniTask.Delay(500);
        ShowTurnIndicator(_playerController.IsEnemyTurn.Value).Forget();
    }
    
    /// <summary>
    /// ターン表示を行う
    /// </summary>
    private async UniTask ShowTurnIndicator(bool isEnemyTurn)
    {
        if (_turnIndicator == null) return;
        
        // 既存のアニメーションをキャンセル
        if (_tweens.TryGetValue("turnIndicator", out Sequence oldSeq))
        {
            oldSeq.Kill();
        }
        
        // テキストと色を設定
        _turnIndicator.text = isEnemyTurn ? "回避!" : "アタック!";
        _turnIndicator.color = isEnemyTurn ? _enemyTurnColor : _playerTurnColor;
        
        // アニメーションシーケンス
        Sequence sequence = DOTween.Sequence();
        
        // ターン表示を有効化
        _turnIndicator.gameObject.SetActive(true);
        _turnIndicator.transform.localScale = Vector3.zero;
        
        // 拡大して表示
        sequence.Append(_turnIndicator.transform.DOScale(1.3f, 0.3f).SetEase(Ease.OutBack));
        sequence.Append(_turnIndicator.transform.DOScale(1f, 0.2f).SetEase(Ease.OutQuad));
        
        // 少し待機
        sequence.AppendInterval(_turnIndicatorDuration);
        
        // 縮小して非表示
        sequence.Append(_turnIndicator.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack))
            .OnComplete(() => _turnIndicator.gameObject.SetActive(false));
        
        _tweens["turnIndicator"] = sequence;
        
        // オーバーレイでターン切り替え表現
        if (_overlayPanel != null)
        {
            Color targetColor = isEnemyTurn ? 
                new Color(_enemyTurnColor.r, _enemyTurnColor.g, _enemyTurnColor.b, 0.3f) : 
                new Color(_playerTurnColor.r, _playerTurnColor.g, _playerTurnColor.b, 0.3f);
            
            // フラッシュエフェクト
            _overlayPanel.color = Color.clear;
            await _overlayPanel.DOColor(targetColor, 0.3f).SetEase(Ease.OutQuad).AsyncWaitForCompletion();
            await _overlayPanel.DOFade(0, 0.5f).SetEase(Ease.InQuad).AsyncWaitForCompletion();
        }
    }
    
    /// <summary>
    /// コンボパネルのパルスアニメーション
    /// </summary>
    private void PulseComboPanel(int combo)
    {
        if (_comboPanel == null) return;
        
        // 既存のアニメーションをキャンセル
        if (_tweens.TryGetValue("comboPulse", out Sequence oldSeq))
        {
            oldSeq.Kill();
        }
        
        // コンボに応じた演出を変更
        float pulseScale = 1.1f;
        float pulseDuration = 0.2f;
        
        if (combo >= 50)
        {
            pulseScale = 1.3f;
            pulseDuration = 0.3f;
        }
        else if (combo >= 20)
        {
            pulseScale = 1.2f;
            pulseDuration = 0.25f;
        }
        
        // アニメーションシーケンス
        Sequence sequence = DOTween.Sequence();
        sequence.Append(_comboPanel.DOScale(pulseScale, pulseDuration * 0.5f).SetEase(Ease.OutQuad));
        sequence.Append(_comboPanel.DOScale(1f, pulseDuration * 0.5f).SetEase(Ease.InOutQuad));
        
        _tweens["comboPulse"] = sequence;
    }
    
    /// <summary>
    /// BPMに合わせた定期的なUIアニメーション
    /// </summary>
    private void RhythmicUIAnimations()
    {
        // 曲の拍に基づいてアニメーションのタイミングを計算
        float musicPosition = Time.time - _gameStartTime;
        float beatPosition = (musicPosition / BEAT_INTERVAL) % 1.0f;
    }
    
    private void OnDestroy()
    {
        // Tweenをすべて破棄
        foreach (var sequence in _tweens.Values)
        {
            sequence?.Kill();
        }
        
        // 購読解除
        _turnSubscription?.Dispose();
        _comboSubscription?.Dispose();
    }
}