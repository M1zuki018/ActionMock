using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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
    /// PERFECT評価時のエフェクト
    /// </summary>
    private async UniTask PlayPerfectEffect()
    {
        // 色収差エフェクト
        if (_chromaticAberration != null)
        {
            DOTween.To(() => _chromaticAberration.intensity.value, 
                x => _chromaticAberration.intensity.value = x, 
                0.5f, 0.1f);
            
            await UniTask.Delay(100);
            
            DOTween.To(() => _chromaticAberration.intensity.value, 
                x => _chromaticAberration.intensity.value = x, 
                0f, 0.3f);
        }
        
        // 色調整エフェクト - 明るくする
        if (_colorAdjustments != null)
        {
            float originalExposure = _colorAdjustments.postExposure.value;
            
            DOTween.To(() => _colorAdjustments.postExposure.value, 
                x => _colorAdjustments.postExposure.value = x, 
                originalExposure + 0.5f, 0.1f);
            
            await UniTask.Delay(100);
            
            DOTween.To(() => _colorAdjustments.postExposure.value, 
                x => _colorAdjustments.postExposure.value = x, 
                originalExposure, 0.3f);
        }
        
        // 画面フラッシュ
        CreateScreenFlash(Color.yellow.WithAlpha(0.2f));
    }
    
    /// <summary>
    /// SAFE評価時のエフェクト
    /// </summary>
    private async UniTask PlaySafeEffect()
    {
        // 軽い色収差エフェクト
        if (_chromaticAberration != null)
        {
            DOTween.To(() => _chromaticAberration.intensity.value, 
                x => _chromaticAberration.intensity.value = x, 
                0.2f, 0.1f);
            
            await UniTask.Delay(100);
            
            DOTween.To(() => _chromaticAberration.intensity.value, 
                x => _chromaticAberration.intensity.value = x, 
                0f, 0.2f);
        }
        
        // 画面フラッシュ（弱め）
        CreateScreenFlash(Color.cyan.WithAlpha(0.1f));
    }
    
    /// <summary>
    /// MISS評価時のエフェクト
    /// </summary>
    private async UniTask PlayMissEffect()
    {
        // ビネットを一瞬強める
        if (_vignette != null)
        {
            float originalIntensity = _vignette.intensity.value;
            
            DOTween.To(() => _vignette.intensity.value, 
                x => _vignette.intensity.value = x, 
                0.6f, 0.1f);
            
            await UniTask.Delay(200);
            
            DOTween.To(() => _vignette.intensity.value, 
                x => _vignette.intensity.value = x, 
                originalIntensity, 0.3f);
        }
        
        // 色調整 - 一瞬暗くする
        if (_colorAdjustments != null)
        {
            float originalExposure = _colorAdjustments.postExposure.value;
            
            DOTween.To(() => _colorAdjustments.postExposure.value, 
                x => _colorAdjustments.postExposure.value = x, 
                originalExposure - 0.3f, 0.1f);
            
            await UniTask.Delay(100);
            
            DOTween.To(() => _colorAdjustments.postExposure.value, 
                x => _colorAdjustments.postExposure.value = x, 
                originalExposure, 0.3f);
        }
        
        // 画面フラッシュ（赤っぽく）
        CreateScreenFlash(Color.red.WithAlpha(0.1f));
    }
    
    /// <summary>
    /// 大きなスコア獲得時のエフェクト
    /// </summary>
    public async UniTask PlayBigScoreEffect(Text scoreText)
    {
        // 画面をフラッシュさせる
        CreateScreenFlash(Color.white.WithAlpha(0.2f));
        
        // スコアテキストのアニメーション
        Sequence scoreSeq = DOTween.Sequence();
        scoreSeq.Append(scoreText.transform.DOScale(1.3f, 0.2f).SetEase(Ease.OutBack));
        scoreSeq.Append(scoreText.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBounce));
        scoreSeq.Join(scoreText.DOColor(Color.yellow, 0.2f));
        scoreSeq.Append(scoreText.DOColor(Color.white, 0.3f));
        
        // 色調整 - 明るくする
        if (_colorAdjustments != null)
        {
            float originalExposure = _colorAdjustments.postExposure.value;
            
            DOTween.To(() => _colorAdjustments.postExposure.value, 
                x => _colorAdjustments.postExposure.value = x, 
                originalExposure + 0.4f, 0.2f);
            
            await UniTask.Delay(200);
            
            DOTween.To(() => _colorAdjustments.postExposure.value, 
                x => _colorAdjustments.postExposure.value = x, 
                originalExposure, 0.4f);
        }
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

 
    /// <summary>
    /// 画面全体のフラッシュエフェクトを作成
    /// </summary>
    private void CreateScreenFlash(Color flashColor)
    {
        // 画面をフラッシュさせるためのオーバーレイImageがある場合
        Image flashImage = GameObject.Find("ScreenFlash")?.GetComponent<Image>();
        
        if (flashImage == null)
        {
            // フラッシュ用のImageがなければ作成
            GameObject flashObj = new GameObject("ScreenFlash");
            Canvas canvas = FindObjectOfType<Canvas>();
            
            if (canvas != null)
            {
                flashObj.transform.SetParent(canvas.transform, false);
                
                RectTransform rectTransform = flashObj.AddComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.sizeDelta = Vector2.zero;
                
                flashImage = flashObj.AddComponent<Image>();
                flashImage.color = Color.clear;
                flashImage.raycastTarget = false;
            }
            else
            {
                // Canvasがない場合は処理を中断
                Destroy(flashObj);
                return;
            }
        }
        
        // フラッシュアニメーション
        Sequence flashSequence = DOTween.Sequence();
        flashSequence.Append(flashImage.DOColor(flashColor, 0.1f));
        flashSequence.Append(flashImage.DOColor(Color.clear, 0.2f));
    }
    
    /// <summary>
    /// カメラシェイクエフェクト
    /// </summary>
    public async UniTask ShakeCamera(float duration = 0.2f, float strength = 0.5f)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;
        
        Vector3 originalPosition = mainCamera.transform.position;
        
        // シェイク時間内でランダムに位置を変える
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float x = originalPosition.x + UnityEngine.Random.Range(-1f, 1f) * strength;
            float y = originalPosition.y + UnityEngine.Random.Range(-1f, 1f) * strength;
            
            mainCamera.transform.position = new Vector3(x, y, originalPosition.z);
            
            elapsed += Time.deltaTime;
            await UniTask.Yield();
        }
        
        // 元の位置に戻す
        mainCamera.transform.position = originalPosition;
    }
    
    private void OnDestroy()
    {
        _disposable?.Dispose();
        _comboSubscribe?.Dispose();
        
        // 実行中のすべてのTweenをキル
        DOTween.KillAll();
        
        foreach (var sequence in _tweenSequences.Values)
        {
            sequence?.Kill();
        }
    }
}

// 拡張メソッド
public static class ColorExtensions
{
    public static Color WithAlpha(this Color color, float alpha)
    {
        return new Color(color.r, color.g, color.b, alpha);
    }
}