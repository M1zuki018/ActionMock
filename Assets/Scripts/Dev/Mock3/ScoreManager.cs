using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Mock3
{

    /// <summary>
    /// スコアManager
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        [SerializeField] private GameManager2 _gameManager;
        [SerializeField] private Effect _effect;
        [SerializeField] private Text _scoreText;
        private IDisposable _scoreSubscription;
        private int _lastScore = 0;

        private void Start()
        {
            _scoreSubscription = _gameManager.Score.Subscribe(HandleScoreText);
        }

        /// <summary>
        /// スコアを更新する
        /// </summary>
        private void HandleScoreText(int score)
        {
            int scoreDiff = score - _lastScore;

            // スコア差分が大きい場合は特別な演出
            if (scoreDiff >= 500)
            {
                // 大きなスコア獲得時の演出
                _effect.PlayBigScoreEffect(_scoreText).Forget();
            }

            // スコアテキストを更新（数字がカウントアップするアニメーション）
            DOTween.To(() => _lastScore, x =>
            {
                _lastScore = x;
                _scoreText.text = x.ToString("000000");
            }, score, 0.5f).SetEase(Ease.OutQuad);
        }

        private void OnDestroy()
        {
            _scoreSubscription.Dispose();
        }
    }
}