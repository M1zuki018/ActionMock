using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ゲームのリズム同期を管理するクラス
/// </summary>
public class BeatSyncManager : MonoBehaviour
{
    [Header("BPM設定")]
    [SerializeField] private float _bpm = 200f;
    [SerializeField] private int _beatsPerBar = 4; // 1小節あたりの拍数
    
    [Header("視覚的フィードバック")]
    [SerializeField] private RectTransform _beatIndicator; // 拍のビジュアルインジケーター
    [SerializeField] private Color _downbeatColor = Color.red; // 小節の頭の拍の色
    [SerializeField] private Color _beatColor = Color.white; // 通常の拍の色
    
    [Header("デバッグ")]
    [SerializeField] private bool _showDebugInfo = true;
    [SerializeField] private UnityEvent<int> _onBeat; // 拍ごとのイベント
    [SerializeField] private UnityEvent<int> _onBar; // 小節ごとのイベント
    
    // 時間制御
    private float _beatInterval; // 拍の間隔（秒）
    private float _barInterval; // 小節の間隔（秒）
    private float _songPosition; // 現在の曲の位置（秒）
    private float _songPositionInBeats; // 拍単位での曲の位置
    private float _lastBeatTime; // 最後の拍の時間
    private int _currentBeat = 0; // 現在の拍番号（0から始まる）
    private int _currentBar = 0; // 現在の小節番号（0から始まる）
    
    // イベント管理
    private Dictionary<int, List<Action>> _beatActions = new Dictionary<int, List<Action>>();
    private Dictionary<int, List<Action>> _barActions = new Dictionary<int, List<Action>>();
    
    // ビジュアルフィードバック用
    private List<RectTransform> _beatVisualizers = new List<RectTransform>();
    
    private void Awake()
    {
        // 拍と小節の間隔を計算
        _beatInterval = 60f / _bpm;
        _barInterval = _beatInterval * _beatsPerBar;
        
        // ビートビジュアライザーを初期化
        if (_beatIndicator != null)
        {
            InitializeBeatVisualizers();
        }
    }
    
    private void Start()
    {
        // ゲーム開始時のカウントダウンと同期
        StartCountdown().Forget();
    }
    
    private void Update()
    {
        // 曲の位置を更新
        _songPosition += Time.deltaTime;
        _songPositionInBeats = _songPosition / _beatInterval;
        
        // 拍の検出
        int currentBeat = Mathf.FloorToInt(_songPositionInBeats) % _beatsPerBar;
        int currentBar = Mathf.FloorToInt(_songPositionInBeats / _beatsPerBar);
        
        // 新しい拍になったかチェック
        if (currentBeat != _currentBeat)
        {
            _currentBeat = currentBeat;
            _lastBeatTime = _songPosition;
            
            // 拍のイベントを発火
            _onBeat?.Invoke(_currentBeat);
            TriggerBeatActions(_currentBeat);
            
            // ビジュアルフィードバック
            PulseBeatVisualizer(_currentBeat);
        }
        
        // 新しい小節になったかチェック
        if (currentBar != _currentBar)
        {
            _currentBar = currentBar;
            
            // 小節のイベントを発火
            _onBar?.Invoke(_currentBar);
            TriggerBarActions(_currentBar);
        }
        
        // ビート進行度を視覚的に表現
        UpdateBeatProgress();
    }
    
    /// <summary>
    /// ゲーム開始時のカウントダウン
    /// </summary>
    private async UniTask StartCountdown()
    {
        // 初期の曲位置をリセット
        _songPosition = -_beatInterval * 4; // 1小節分のカウントダウン
        
        // カウントダウンの視覚/聴覚的フィードバック
        while (_songPosition < 0)
        {
            // 1拍ごとに更新
            int beatsToStart = Mathf.CeilToInt(-_songPosition / _beatInterval);
            
            // カウントダウン表示などをここで行う
            Debug.Log($"カウントダウン: {beatsToStart}");
            
            // 次の拍まで待機
            await UniTask.Delay((int)(_beatInterval * 1000));
            _songPosition += _beatInterval;
        }
        
        // カウントダウン終了、ゲーム開始
        _songPosition = 0;
    }
    
    /// <summary>
    /// 特定の拍でアクションを登録
    /// </summary>
    public void RegisterBeatAction(int beatNumber, Action action)
    {
        // 指定された拍が辞書になければ追加
        if (!_beatActions.ContainsKey(beatNumber))
        {
            _beatActions[beatNumber] = new List<Action>();
        }
        
        // アクションを登録
        _beatActions[beatNumber].Add(action);
    }
    
    /// <summary>
    /// 特定の小節でアクションを登録
    /// </summary>
    public void RegisterBarAction(int barNumber, Action action)
    {
        if (!_barActions.ContainsKey(barNumber))
        {
            _barActions[barNumber] = new List<Action>();
        }
        
        _barActions[barNumber].Add(action);
    }
    
    /// <summary>
    /// 登録された拍アクションを実行
    /// </summary>
    private void TriggerBeatActions(int beatNumber)
    {
        if (_beatActions.ContainsKey(beatNumber))
        {
            foreach (var action in _beatActions[beatNumber])
            {
                action?.Invoke();
            }
        }
    }
    
    /// <summary>
    /// 登録された小節アクションを実行
    /// </summary>
    private void TriggerBarActions(int barNumber)
    {
        if (_barActions.ContainsKey(barNumber))
        {
            foreach (var action in _barActions[barNumber])
            {
                action?.Invoke();
            }
        }
    }
    
    /// <summary>
    /// ビートビジュアライザーの初期化
    /// </summary>
    private void InitializeBeatVisualizers()
    {
        if (_beatIndicator == null) return;
        
        // 元のビジュアライザーを親として、各拍用のビジュアライザーを作成
        _beatIndicator.gameObject.SetActive(false); // オリジナルは非表示
        
        for (int i = 0; i < _beatsPerBar; i++)
        {
            RectTransform beatVisualizer = Instantiate(_beatIndicator, _beatIndicator.parent);
            beatVisualizer.name = $"BeatVisualizer_{i}";
            beatVisualizer.gameObject.SetActive(true);
            
            // 小節の先頭拍は特別な色に
            if (i == 0)
            {
                beatVisualizer.GetComponent<UnityEngine.UI.Image>().color = _downbeatColor;
            }
            else
            {
                beatVisualizer.GetComponent<UnityEngine.UI.Image>().color = _beatColor;
            }
            
            // 円形に配置
            float angle = i * (360f / _beatsPerBar);
            float radius = 100f; // 配置半径
            float x = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;
            float y = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
            
            beatVisualizer.anchoredPosition = new Vector2(x, y);
            _beatVisualizers.Add(beatVisualizer);
        }
    }
    
    /// <summary>
    /// 特定の拍のビジュアライザーをパルス
    /// </summary>
    private void PulseBeatVisualizer(int beatIndex)
    {
        if (_beatVisualizers.Count == 0) return;
        
        foreach (var visualizer in _beatVisualizers)
        {
            // すべてのビジュアライザーを通常サイズに戻す
            visualizer.localScale = Vector3.one;
        }
        
        // 現在の拍のビジュアライザーをパルス
        _beatVisualizers[beatIndex].DOScale(1.5f, _beatInterval * 0.3f).SetEase(Ease.OutBack)
            .OnComplete(() => _beatVisualizers[beatIndex].DOScale(1f, _beatInterval * 0.2f));
        
        // 小節の先頭の場合は特別なエフェクト
        if (beatIndex == 0)
        {
            foreach (var visualizer in _beatVisualizers)
            {
                visualizer.DOPunchRotation(new Vector3(0, 0, 10f), _beatInterval * 0.5f, 2, 0.5f);
            }
        }
    }
    
    /// <summary>
    /// 拍の進行状況を視覚的に表示
    /// </summary>
    private void UpdateBeatProgress()
    {
        if (_beatVisualizers.Count == 0) return;
        
        // 拍間の正確な位置（0〜1）
        float beatProgress = (_songPosition - _lastBeatTime) / _beatInterval;
        
        // 進行状況に応じてビジュアライザーを更新
        for (int i = 0; i < _beatVisualizers.Count; i++)
        {
            if (i == _currentBeat)
            {
                // 現在の拍のビジュアライザーを更新
                float scale = Mathf.Lerp(1.2f, 1f, beatProgress);
                _beatVisualizers[i].localScale = new Vector3(scale, scale, 1f);
            }
        }
    }
    
    /// <summary>
    /// 現在の拍情報を取得
    /// </summary>
    public (int beat, int bar, float progress) GetCurrentBeatInfo()
    {
        return (_currentBeat, _currentBar, (_songPosition - _lastBeatTime) / _beatInterval);
    }
    
    /// <summary>
    /// 特定の拍数だけ待機するユーティリティメソッド
    /// </summary>
    public async UniTask WaitForBeats(int beats)
    {
        float waitTime = _beatInterval * beats;
        await UniTask.Delay((int)(waitTime * 1000));
    }
    
    /// <summary>
    /// 特定の小節数だけ待機するユーティリティメソッド
    /// </summary>
    public async UniTask WaitForBars(int bars)
    {
        float waitTime = _barInterval * bars;
        await UniTask.Delay((int)(waitTime * 1000));
    }
    
    /// <summary>
    /// 拍に同期して実行するユーティリティメソッド
    /// </summary>
    public async UniTask ExecuteOnNextBeat(Action action)
    {
        // 次の拍までの時間を計算
        float timeToNextBeat = _beatInterval - (_songPosition % _beatInterval);
        await UniTask.Delay((int)(timeToNextBeat * 1000));
        
        // 拍に合わせてアクションを実行
        action?.Invoke();
    }
    
    /// <summary>
    /// 一時的にゲームの速度を変更（スローモーションなど）
    /// </summary>
    public async UniTask SetTemporaryTimeScale(float scale, float duration)
    {
        // オリジナルのタイムスケールを保存
        float originalTimeScale = Time.timeScale;
        
        // 新しいタイムスケールを設定
        Time.timeScale = scale;
        
        // 指定時間待機（リアルタイムで）
        await UniTask.Delay((int)(duration * 1000), ignoreTimeScale: true);
        
        // タイムスケールを徐々に元に戻す
        DOTween.To(() => Time.timeScale, x => Time.timeScale = x, originalTimeScale, 0.5f)
            .SetUpdate(true); // ignoreTimeScale = true
    }
    
#if UNITY_EDITOR
    private void OnGUI()
    {
        if (!_showDebugInfo) return;
        
        GUI.Box(new Rect(10, 10, 200, 90), "Beat Info");
        GUI.Label(new Rect(20, 30, 180, 20), $"Beat: {_currentBeat + 1}/{_beatsPerBar}");
        GUI.Label(new Rect(20, 50, 180, 20), $"Bar: {_currentBar + 1}");
        
        float progress = (_songPosition - _lastBeatTime) / _beatInterval;
        GUI.Label(new Rect(20, 70, 180, 20), $"Progress: {progress:F2}");
    }
#endif
}