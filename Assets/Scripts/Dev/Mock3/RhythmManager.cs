using System.Collections.Generic;
using UniRx;
using UnityEngine;

namespace Mock3
{
    public class RhythmManager : MonoBehaviour
{
    [SerializeField] private float _bpm = 200f;
    
    // 現在の小節内の位置（0～1）
    public float CurrentBarPosition => GetCurrentBarPosition();
    
    // 1小節の長さ（秒）
    public float BarDuration => 60f / _bpm * 4f;
    
    // 最後のビート時間
    private float _lastBeatTime = 0f;
    
    // 現在のビート番号
    private int _currentBeat = 0;
    
    // 敵の攻撃タイミング情報
    private List<float> _enemyAttackTimings = new List<float>();
    
    private void Update()
    {
        // 現在の小節位置を計算
        float barPos = GetCurrentBarPosition();
        
        // ビート検出（4分音符ごと）
        int currentBeatInBar = Mathf.FloorToInt(barPos * 4);
        if (currentBeatInBar != _currentBeat)
        {
            _currentBeat = currentBeatInBar;
            _lastBeatTime = Time.time;
            
            // ビートイベント発火
            OnBeat.OnNext(_currentBeat);
        }
    }
    
    // 現在の小節内の位置を取得（0～1）
    private float GetCurrentBarPosition()
    {
        float position = Mathf.Repeat(Time.time, BarDuration) / BarDuration;
        return position;
    }
    
    // 直近のビートからの時間差を取得（秒）
    public float GetTimingAccuracy()
    {
        float timeSinceLastBeat = Time.time - _lastBeatTime;
        float beatDuration = BarDuration / 4f; // 4分音符の長さ
        
        // 次のビートまでの時間
        float timeToNextBeat = beatDuration - timeSinceLastBeat;
        
        // 近い方の絶対値を返す
        return Mathf.Min(timeSinceLastBeat, timeToNextBeat);
    }
    
    // 敵の攻撃タイミングを登録
    public void RegisterEnemyAttackTimings(float[] timings)
    {
        _enemyAttackTimings.Clear();
        foreach (var timing in timings)
        {
            if (timing > 0) // 0は攻撃なしを表す
            {
                _enemyAttackTimings.Add(timing);
            }
        }
    }
    
    // 敵の攻撃タイミングと現在時間の差を取得
    public bool IsInEnemyAttackTiming(float threshold)
    {
        float currentBarPos = GetCurrentBarPosition();
        
        foreach (var timing in _enemyAttackTimings)
        {
            float diff = Mathf.Abs(currentBarPos - timing);
            diff = Mathf.Min(diff, 1 - diff); // 小節の境界をまたぐ場合
            
            if (diff * BarDuration <= threshold)
            {
                return true;
            }
        }
        
        return false;
    }
    
    // ビートイベント
    public Subject<int> OnBeat = new Subject<int>();
}
}
