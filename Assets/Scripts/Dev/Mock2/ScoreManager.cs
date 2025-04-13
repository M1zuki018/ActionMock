using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    [SerializeField] private GameManager2 _gameManager;
    [SerializeField] private Text _scoreText;
    private IDisposable _scoreSubscription;

    private void Start()
    {
        _scoreSubscription = _gameManager.Score.Subscribe(HandleScoreText);
    }

    /// <summary>
    /// スコアを更新する
    /// </summary>
    private void HandleScoreText(int score)
    {
        _scoreText.text = score.ToString("000000");
    }

    private void OnDestroy()
    {
        _scoreSubscription.Dispose();
    }
}
