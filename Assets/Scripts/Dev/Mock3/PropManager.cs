using UniRx;
using UnityEngine;
using System.Collections.Generic;

namespace Mock3
{
    /// <summary>
    /// プロップとバトルフェーズを管理するクラス
    /// </summary>
    public class PropManager : MonoBehaviour
    {
        [System.Serializable]
        public class PropSection
        {
            public List<GameObject> props = new List<GameObject>();
            public int battleDurationBars;
            [Comment("このセクションがバトルポイントかどうか")] public bool isBattlePoint = true;
        }

        [SerializeField] private List<PropSection> _propSections = new List<PropSection>();
        [SerializeField] private float _rotationSpeed = 2f; // 回転速度

        [Header("音楽設定")]
        [SerializeField] private AudioClip _transitionSound;

        // 現在のプロップセクションインデックス
        private int _currentSectionIndex = 0;
        // 現在のプロップインデックス
        private int _currentPropIndex = 0;

        // プレイヤーの参照
        [SerializeField] private GameObject _player;
        private Transform _playerTransform;

        // ゲームの状態
        public enum GameState
        {
            Moving,      // 自動移動中
            Battle,      // バトル中
            WaitingForBattle, // バトル開始待機中
            WaitingAfterBattle // バトル終了後待機中
        }

        // 現在のゲーム状態
        public ReactiveProperty<GameState> CurrentState = new ReactiveProperty<GameState>(GameState.Moving);

        // 敵ターンフラグ
        public ReactiveProperty<bool> IsEnemyTurn = new ReactiveProperty<bool>(false);

        // イベント通知
        public Subject<int> OnBattleStart = new Subject<int>();
        public Subject<int> OnBattleEnd = new Subject<int>();
        public Subject<GameState> OnStateChanged = new Subject<GameState>();

        // 移動処理関連の変数
        private Vector3 _startPosition;
        private float _pathTransitionTimer = 0f;
        private bool _isTransitioning = false;

        // 待機小節カウンター
        private int _waitBarCounter = 0;
        private int _battleBarCounter = 0;
        private float _lastBarTime = 0f;

        // 1小節の時間（秒）
        private float _oneBarDuration => 60f / GameConst.BPM * 4f;

        // 現在の小節位置（0～1）
        private float _currentBarPosition => Mathf.Repeat(Time.time, _oneBarDuration) / _oneBarDuration;

        private void Awake()
        {
            _playerTransform = _player.transform;
            _startPosition = _playerTransform.position;
        }

        private void Start()
        {
            // 状態変化を監視
            CurrentState
                .Skip(1) // 初期値をスキップ
                .Subscribe(state =>
                {
                    OnStateChanged.OnNext(state);
                    Debug.Log($"ゲーム状態変更: {state}");

                    // 状態に応じた処理
                    switch (state)
                    {
                        case GameState.Battle:
                            IsEnemyTurn.Value = true;
                            OnBattleStart.OnNext(_currentSectionIndex);
                            // バトル継続小節数を設定
                            _battleBarCounter = _propSections[_currentSectionIndex].battleDurationBars;
                            break;

                        case GameState.Moving:
                            IsEnemyTurn.Value = false;
                            break;
                    }
                })
                .AddTo(this);

            // バトル終了検出
            IsEnemyTurn
                .Skip(1) // 初期値をスキップ
                .Subscribe(isEnemyTurn =>
                {
                    // 敵ターンからプレイヤーターンに切り替わったらバトル終了
                    if (!isEnemyTurn && CurrentState.Value == GameState.Battle)
                    {
                        EndBattle();
                    }
                })
                .AddTo(this);
        }

        private void Update()
        {
            // 小節の変わり目を検出
            DetectBarChange();

            // 現在の状態に応じた処理
            switch (CurrentState.Value)
            {
                case GameState.Moving:
                    UpdateMovement();
                    break;

                case GameState.Battle:
                    // バトル小節カウント完了でバトル終了
                    if (_battleBarCounter <= 0)
                    {
                        EndBattle();
                    }
                    break;
                
                case GameState.WaitingForBattle:
                    // 待機小節カウント完了でバトル開始
                    if (_waitBarCounter <= 0)
                    {
                        CurrentState.Value = GameState.Battle;
                    }
                    break;

                case GameState.WaitingAfterBattle:
                    // 待機小節カウント完了で次のセクションへ
                    if (_waitBarCounter <= 0)
                    {
                        MoveToNextSection();
                        CurrentState.Value = GameState.Moving;
                    }
                    break;
            }

            // 常に進行方向を向く
            UpdatePlayerRotation();
        }

        /// <summary>
        /// 小節の変わり目を検出する
        /// </summary>
        private void DetectBarChange()
        {
            float currentTime = Time.time;
            float barPosition = _currentBarPosition;

            // 小節の始まり（0～0.1）で、前回の記録から十分に時間が経っている場合
            if (barPosition < 0.1f && currentTime - _lastBarTime > _oneBarDuration * 0.5f)
            {
                _lastBarTime = currentTime;

                // 状態に応じたカウンター処理
                switch (CurrentState.Value)
                {
                    case GameState.WaitingForBattle:
                    case GameState.WaitingAfterBattle:
                        _waitBarCounter--;
                        Debug.Log($"待機小節カウント: {_waitBarCounter}");
                        break;
                        
                    case GameState.Battle:
                        _battleBarCounter--;
                        Debug.Log($"バトル小節カウント: {_battleBarCounter}");
                        break;
                }
            }
        }

        /// <summary>
        /// プレイヤーの移動を更新する
        /// </summary>
        private void UpdateMovement()
        {
            if (_currentSectionIndex >= _propSections.Count) return;
            if (_currentPropIndex >= _propSections[_currentSectionIndex].props.Count) return;

            GameObject currentProp = _propSections[_currentSectionIndex].props[_currentPropIndex];
            
            if (currentProp == null) return;

            if (!_isTransitioning)
            {
                // 新しいプロップへの移動開始
                _startPosition = _playerTransform.position;
                _pathTransitionTimer = 0f;
                _isTransitioning = true;
            }

            // 一小節の時間
            float oneBarDuration = _oneBarDuration;

            // タイマー更新
            _pathTransitionTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_pathTransitionTimer / oneBarDuration);

            // 現在位置から目標点への方向ベクトルを計算
            Vector3 targetPosition = currentProp.transform.position;
            Vector3 directionToTarget = (targetPosition - _startPosition).normalized;

            // 現在の経過時間に基づいて進行距離を計算
            float totalDistance = Vector3.Distance(_startPosition, targetPosition);
            float currentDistance = totalDistance * t;

            // 新しい位置 = 開始位置 + (方向ベクトル × 距離)
            Vector3 newPosition = _startPosition + (directionToTarget * currentDistance);
            _playerTransform.position = newPosition;

            // 一小節経過または十分近づいたらプロップ更新
            if (t >= 1.0f || Vector3.Distance(newPosition, targetPosition) < 0.1f)
            {
                _isTransitioning = false;
                UpdatePropTarget();
            }
        }

        /// <summary>
        /// 次のプロップターゲットを更新する
        /// </summary>
        private void UpdatePropTarget()
        {
            _currentPropIndex++;

            // 現在のセクションのプロップをすべて通過した場合
            if (_currentPropIndex >= _propSections[_currentSectionIndex].props.Count)
            {
                // バトルポイントなら戦闘開始準備
                if (_propSections[_currentSectionIndex].isBattlePoint)
                {
                    StartBattle();
                }
                else
                {
                    // バトルポイントでなければ次のセクションへ
                    MoveToNextSection();
                }
            }
        }

        /// <summary>
        /// プレイヤーの回転を更新する
        /// </summary>
        private void UpdatePlayerRotation()
        {
            if (_currentSectionIndex >= _propSections.Count) return;
            if (_currentPropIndex >= _propSections[_currentSectionIndex].props.Count) return;

            GameObject currentProp = _propSections[_currentSectionIndex].props[_currentPropIndex];
            
            if (currentProp == null) return;

            // 目標への方向ベクトル
            Vector3 targetPosition = currentProp.transform.position;
            Vector3 directionToTarget = (targetPosition - _playerTransform.position).normalized;
            directionToTarget.y = 0f; // Y軸は無視

            if (directionToTarget != Vector3.zero)
            {
                // ターゲット方向を向くクォータニオンを計算
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                
                // スムーズに回転
                _playerTransform.rotation = Quaternion.Slerp(
                    _playerTransform.rotation, 
                    targetRotation, 
                    _rotationSpeed * Time.deltaTime
                );
            }
        }

        /// <summary>
        /// バトル開始準備
        /// </summary>
        private void StartBattle()
        {
            if (CurrentState.Value != GameState.Moving) return;

            // トランジション音を再生
            if (AudioController.Instance != null && _transitionSound != null)
            {
                AudioController.Instance.PlaySE(_transitionSound);
            }
            
            // 待機フェーズへ移行
            CurrentState.Value = GameState.WaitingForBattle;
            
            Debug.Log($"バトル開始準備 - {_waitBarCounter}小節待機");
        }

        /// <summary>
        /// バトル終了処理
        /// </summary>
        private void EndBattle()
        {
            // バトル終了イベント発火
            OnBattleEnd.OnNext(_currentSectionIndex);
            
            // トランジション音を再生
            if (AudioController.Instance != null && _transitionSound != null)
            {
                AudioController.Instance.PlaySE(_transitionSound);
            }
            
            // 待機フェーズへ移行
            CurrentState.Value = GameState.WaitingAfterBattle;
            
            Debug.Log($"バトル終了 - {_waitBarCounter}小節待機");
        }

        /// <summary>
        /// 次のセクションに移動
        /// </summary>
        private void MoveToNextSection()
        {
            _currentSectionIndex++;
            _currentPropIndex = 0;
            _isTransitioning = false;
            
            // 全セクション終了チェック
            if (_currentSectionIndex >= _propSections.Count)
            {
                Debug.Log("全セクション完了！");
                // ここでゲームクリア演出など
                enabled = false; // このコンポーネントを無効化
                return;
            }
            
            Debug.Log($"次のセクションへ移動: {_currentSectionIndex}");
        }
    }
}