using UnityEngine;
using UniRx;
using System.Collections.Generic;
using System;

namespace Mock3
{
    /// <summary>
    /// 敵の攻撃システムを管理するクラス
    /// </summary>
    public class EnemyAttackSystem : MonoBehaviour
    {
        [System.Serializable]
        public class AttackPhaseInfo
        {
            [Tooltip("攻撃フェーズの名前")] 
            public string phaseName;
            
            [Tooltip("このフェーズでの攻撃パターン")] 
            public List<AttackInfo> attackPatterns = new List<AttackInfo>();
            
            [Tooltip("HPがこの割合以下になると次のフェーズに移行（0～1）")] 
            [Range(0, 1)] public float nextPhaseThreshold = 0.5f;
            
            [Tooltip("フェーズ切り替え時のエフェクト")] 
            public GameObject phaseTransitionEffect;
        }
        
        [System.Serializable]
        public class AttackInfo
        {
            [Tooltip("攻撃の名前")] 
            public string attackName;
            
            [Tooltip("基本ダメージ")] 
            public int baseDamage = 10;
            
            [Tooltip("攻撃の予備動作時間（小節の割合 0～1）")] 
            [Range(0, 1)] public float telegraphTime = 0.25f;
            
            [Header("4小節のリズムパターン")]
            [Tooltip("1小節目 (16分音符×16)")]
            public bool[] bar1Pattern = new bool[16];
            
            [Tooltip("2小節目 (16分音符×16)")]
            public bool[] bar2Pattern = new bool[16];
            
            [Tooltip("3小節目 (16分音符×16)")]
            public bool[] bar3Pattern = new bool[16];
            
            [Tooltip("4小節目 (16分音符×16)")]
            public bool[] bar4Pattern = new bool[16];
            
            [Header("攻撃エフェクト")]
            [Tooltip("予備動作エフェクト")] 
            public GameObject telegraphEffect;
            
            [Tooltip("攻撃エフェクト")] 
            public GameObject attackEffect;
            
            [Tooltip("攻撃範囲（メートル）")] 
            public float attackRange = 3f;
            
            [Tooltip("攻撃の形状 (0: 球形, 1: 円錐形, 2: 直線)")] 
            [Range(0, 2)] public int attackShape = 0;
            
            [Header("攻撃サウンド")]
            [Tooltip("予備動作サウンド")] 
            public AudioClip telegraphSound;
            
            [Tooltip("攻撃サウンド")] 
            public AudioClip attackSound;
            
            // 攻撃パターン全体を取得（64要素の配列に変換）
            public bool[] GetFullPattern()
            {
                bool[] fullPattern = new bool[64];
                Array.Copy(bar1Pattern, 0, fullPattern, 0, 16);
                Array.Copy(bar2Pattern, 0, fullPattern, 16, 16);
                Array.Copy(bar3Pattern, 0, fullPattern, 32, 16);
                Array.Copy(bar4Pattern, 0, fullPattern, 48, 16);
                return fullPattern;
            }
        }
        
        [Header("攻撃フェーズ設定")]
        [SerializeField] private List<AttackPhaseInfo> _attackPhases = new List<AttackPhaseInfo>();
        
        [Header("攻撃対象")]
        [SerializeField] private Transform _player;
        [SerializeField] private PlayerHealth _playerHealth;
        
        [Header("攻撃設定")]
        [SerializeField] private float _attackCooldown = 0.1f; // 連続攻撃の間隔
        
        // RhythmManagerの参照
        private RhythmManager _rhythmManager;
        
        // PropManagerの参照
        private PropManager _propManager;
        
        // 現在のフェーズインデックス
        private int _currentPhaseIndex = 0;
        
        // 現在のパターンインデックス
        private int _currentPatternIndex = 0;
        
        // 現在の小節内位置
        private int _currentNoteIndex = 0;
        
        // 前回のノートインデックス
        private int _previousNoteIndex = -1;
        
        // 攻撃済みフラグ
        private bool[] _attackedFlags = new bool[64];
        
        // 予備動作中フラグ
        private bool[] _telegraphFlags = new bool[64];
        
        // 最後に攻撃した時間
        private float _lastAttackTime = -100f;
        
        // 敵のHPの参照
        private EnemyHealth _enemyHealth;
        
        // 現在の攻撃サイクル（0～3 = 4小節分）
        private int _currentAttackCycle = 0;
        
        // バトル開始フラグ
        private bool _battleStarted = false;
        
        // イベント
        public Subject<int> OnAttackStarted = new Subject<int>();
        public Subject<int> OnAttackEnded = new Subject<int>();
        public Subject<int> OnPhaseChanged = new Subject<int>();
        
        private void Awake()
        {
            _enemyHealth = GetComponent<EnemyHealth>();
            if (_enemyHealth == null)
            {
                Debug.LogError("EnemyHealthコンポーネントが見つかりません");
            }
        }
        
        private void Start()
        {
            // RhythmManagerを取得
            _rhythmManager = FindObjectOfType<RhythmManager>();
            if (_rhythmManager == null)
            {
                Debug.LogError("RhythmManagerが見つかりません");
                enabled = false;
                return;
            }
            
            // PropManagerを取得
            _propManager = FindObjectOfType<PropManager>();
            if (_propManager == null)
            {
                Debug.LogError("PropManagerが見つかりません");
                enabled = false;
                return;
            }
            
            // PropManagerの状態変化を監視
            _propManager.CurrentState
                .Subscribe(state =>
                {
                    if (state == PropManager.GameState.Battle && !_battleStarted)
                    {
                        StartBattle();
                    }
                    else if (state != PropManager.GameState.Battle && _battleStarted)
                    {
                        EndBattle();
                    }
                })
                .AddTo(this);
            
            // HPの変化を監視してフェーズ切り替え
            if (_enemyHealth != null)
            {
                _enemyHealth.OnDamaged
                    .Subscribe(_ => CheckPhaseTransition())
                    .AddTo(this);
                
                _enemyHealth.OnDeath
                    .Subscribe(_ => OnEnemyDeath())
                    .AddTo(this);
            }
            
            // ビート検出を監視
            _rhythmManager.OnBeat
                .Subscribe(_ => OnBeat())
                .AddTo(this);
        }
        
        private void Update()
        {
            if (!_battleStarted) return;
            
            // 現在の16分音符位置を計算
            float barPosition = _rhythmManager.CurrentBarPosition;
            int noteIndex = Mathf.FloorToInt(barPosition * 16);
            
            // 小節をまたぐと4小節で64ノートになる
            int globalNoteIndex = _currentAttackCycle * 16 + noteIndex;
            
            // ノートが変わったときだけ処理
            if (noteIndex != _previousNoteIndex)
            {
                _previousNoteIndex = noteIndex;
                _currentNoteIndex = globalNoteIndex % 64; // 64ノートでループ
                
                // 現在のパターンを取得
                bool[] pattern = GetCurrentPattern();
                
                // 攻撃予備動作のチェック
                CheckTelegraph(pattern);
                
                // 攻撃判定
                CheckAttack(pattern);
                
                // 小節の最後でサイクルカウンターの更新
                if (noteIndex == 15)
                {
                    _currentAttackCycle = (_currentAttackCycle + 1) % 4;
                    
                    // 4小節が経過したらパターン変更
                    if (_currentAttackCycle == 0)
                    {
                        ChangeAttackPattern();
                    }
                }
            }
        }
        
        /// <summary>
        /// バトル開始処理
        /// </summary>
        private void StartBattle()
        {
            _battleStarted = true;
            _currentPhaseIndex = 0;
            _currentPatternIndex = 0;
            _currentAttackCycle = 0;
            
            // 攻撃フラグリセット
            ResetAttackFlags();
            
            Debug.Log("敵の攻撃システム: バトル開始");
        }
        
        /// <summary>
        /// バトル終了処理
        /// </summary>
        private void EndBattle()
        {
            _battleStarted = false;
            Debug.Log("敵の攻撃システム: バトル終了");
        }
        
        /// <summary>
        /// ビート検出時の処理
        /// </summary>
        private void OnBeat()
        {
            // ビート時の処理（必要に応じて実装）
        }
        
        /// <summary>
        /// 敵の死亡時処理
        /// </summary>
        private void OnEnemyDeath()
        {
            // バトル終了通知（PropManagerがリアクションする）
            _propManager.IsEnemyTurn.Value = false;
        }
        
        /// <summary>
        /// 現在のパターンを取得
        /// </summary>
        private bool[] GetCurrentPattern()
        {
            if (_attackPhases.Count == 0) return new bool[64];
            if (_currentPhaseIndex >= _attackPhases.Count) return new bool[64];
            
            var currentPhase = _attackPhases[_currentPhaseIndex];
            if (currentPhase.attackPatterns.Count == 0) return new bool[64];
            
            if (_currentPatternIndex >= currentPhase.attackPatterns.Count)
            {
                _currentPatternIndex = 0; // 循環
            }
            
            return currentPhase.attackPatterns[_currentPatternIndex].GetFullPattern();
        }
        
        /// <summary>
        /// 現在のアタックインフォを取得
        /// </summary>
        private AttackInfo GetCurrentAttackInfo()
        {
            if (_attackPhases.Count == 0) return null;
            if (_currentPhaseIndex >= _attackPhases.Count) return null;
            
            var currentPhase = _attackPhases[_currentPhaseIndex];
            if (currentPhase.attackPatterns.Count == 0) return null;
            
            if (_currentPatternIndex >= currentPhase.attackPatterns.Count)
            {
                _currentPatternIndex = 0; // 循環
            }
            
            return currentPhase.attackPatterns[_currentPatternIndex];
        }
        
        /// <summary>
        /// 攻撃フラグをリセット
        /// </summary>
        private void ResetAttackFlags()
        {
            for (int i = 0; i < _attackedFlags.Length; i++)
            {
                _attackedFlags[i] = false;
                _telegraphFlags[i] = false;
            }
        }
        
        /// <summary>
        /// 攻撃パターンを変更
        /// </summary>
        private void ChangeAttackPattern()
        {
            if (_attackPhases.Count == 0) return;
            if (_currentPhaseIndex >= _attackPhases.Count) return;
            
            var currentPhase = _attackPhases[_currentPhaseIndex];
            if (currentPhase.attackPatterns.Count <= 1) return;
            
            // 次のパターンにランダムに変更（現在と同じにならないように）
            int nextPattern;
            do
            {
                nextPattern = UnityEngine.Random.Range(0, currentPhase.attackPatterns.Count);
            } while (nextPattern == _currentPatternIndex && currentPhase.attackPatterns.Count > 1);
            
            _currentPatternIndex = nextPattern;
            ResetAttackFlags();
            
            Debug.Log($"攻撃パターン変更: {currentPhase.attackPatterns[_currentPatternIndex].attackName}");
        }
        
        /// <summary>
        /// フェーズ遷移をチェック
        /// </summary>
        private void CheckPhaseTransition()
        {
            if (_enemyHealth == null) return;
            if (_attackPhases.Count <= 1) return;
            if (_currentPhaseIndex >= _attackPhases.Count - 1) return;
            
            // 現在のHP比率を計算
            float healthRatio = 0;//(float)_enemyHealth.CurrentHealth / _enemyHealth.MaxHealth;
            
            // 次のフェーズの閾値と比較
            if (healthRatio <= _attackPhases[_currentPhaseIndex].nextPhaseThreshold)
            {
                // フェーズ移行
                _currentPhaseIndex++;
                _currentPatternIndex = 0;
                ResetAttackFlags();
                
                // エフェクト生成
                if (_attackPhases[_currentPhaseIndex - 1].phaseTransitionEffect != null)
                {
                    Instantiate(
                        _attackPhases[_currentPhaseIndex - 1].phaseTransitionEffect,
                        transform.position,
                        Quaternion.identity
                    );
                }
                
                // イベント発火
                OnPhaseChanged.OnNext(_currentPhaseIndex);
                
                Debug.Log($"敵フェーズ変更: {_attackPhases[_currentPhaseIndex].phaseName}");
            }
        }
        
        /// <summary>
        /// 攻撃予備動作のチェック
        /// </summary>
        private void CheckTelegraph(bool[] pattern)
        {
            AttackInfo attackInfo = GetCurrentAttackInfo();
            if (attackInfo == null) return;
            
            // 各ノートに対して予備動作を確認
            for (int i = 0; i < 64; i++)
            {
                if (!pattern[i] || _telegraphFlags[i]) continue;
                
                // 予備動作の開始タイミング計算
                float telegraphBars = attackInfo.telegraphTime; // 小節単位
                int telegraphNotes = Mathf.RoundToInt(telegraphBars * 16); // 16分音符単位
                
                // 予備動作の開始ノートインデックスを計算
                int telegraphIndex = (i - telegraphNotes) % 64;
                if (telegraphIndex < 0) telegraphIndex += 64;
                
                // 現在のノートが予備動作開始ノートと一致する場合
                if (_currentNoteIndex == telegraphIndex)
                {
                    // 予備動作開始
                    StartTelegraph(i, attackInfo);
                    _telegraphFlags[i] = true;
                }
            }
        }
        
        /// <summary>
        /// 攻撃判定
        /// </summary>
        private void CheckAttack(bool[] pattern)
        {
            if (pattern == null) return;
            if (Time.time - _lastAttackTime < _attackCooldown) return;
            
            // 現在のノートで攻撃があるか確認
            if (_currentNoteIndex < pattern.Length && 
                pattern[_currentNoteIndex] && 
                !_attackedFlags[_currentNoteIndex])
            {
                // 攻撃実行
                PerformAttack(_currentNoteIndex);
                _attackedFlags[_currentNoteIndex] = true;
                _lastAttackTime = Time.time;
            }
        }
        
        /// <summary>
        /// 予備動作開始
        /// </summary>
        private void StartTelegraph(int noteIndex, AttackInfo attackInfo)
        {
            if (attackInfo == null) return;
            
            // 予備動作エフェクト生成
            if (attackInfo.telegraphEffect != null)
            {
                // 攻撃が当たる位置を予測
                Vector3 targetPosition = PredictAttackPosition(attackInfo);
                
                // エフェクト生成
                GameObject effect = Instantiate(
                    attackInfo.telegraphEffect,
                    targetPosition,
                    Quaternion.identity
                );
                
                // エフェクトの大きさを攻撃範囲に合わせる
                effect.transform.localScale = new Vector3(
                    attackInfo.attackRange,
                    attackInfo.attackRange,
                    attackInfo.attackRange
                );
                
                // 予備動作の持続時間
                float duration = attackInfo.telegraphTime * _rhythmManager.BarDuration;
                Destroy(effect, duration);
            }
            
            // 予備動作サウンド再生
            if (AudioController.Instance != null && attackInfo.telegraphSound != null)
            {
                AudioController.Instance.PlaySE(attackInfo.telegraphSound);
            }
            
            Debug.Log($"攻撃予備動作開始: {attackInfo.attackName} (ノート:{noteIndex})");
        }
        
        /// <summary>
        /// 攻撃実行
        /// </summary>
        private void PerformAttack(int noteIndex)
        {
            AttackInfo attackInfo = GetCurrentAttackInfo();
            if (attackInfo == null) return;
            
            // 攻撃位置を計算
            Vector3 attackPosition = PredictAttackPosition(attackInfo);
            
            // 攻撃判定処理
            PerformDamageCheck(attackPosition, attackInfo);
            
            // 攻撃エフェクト生成
            if (attackInfo.attackEffect != null)
            {
                GameObject effect = Instantiate(
                    attackInfo.attackEffect,
                    attackPosition,
                    Quaternion.identity
                );
                
                // エフェクトの大きさを攻撃範囲に合わせる
                effect.transform.localScale = new Vector3(
                    attackInfo.attackRange,
                    attackInfo.attackRange,
                    attackInfo.attackRange
                );
                
                // 短時間で消える
                Destroy(effect, 1.0f);
            }
            
            // 攻撃サウンド再生
            if (AudioController.Instance != null && attackInfo.attackSound != null)
            {
                AudioController.Instance.PlaySE(attackInfo.attackSound);
            }
            
            // イベント発火
            OnAttackStarted.OnNext(noteIndex);
            
            Debug.Log($"攻撃実行: {attackInfo.attackName} (ノート:{noteIndex})");
        }
        
        /// <summary>
        /// 攻撃位置を予測
        /// </summary>
        private Vector3 PredictAttackPosition(AttackInfo attackInfo)
        {
            if (_player == null) return transform.position;
            
            // 攻撃形状に応じた位置計算
            switch (attackInfo.attackShape)
            {
                case 0: // 球形（プレイヤー位置に直接）
                    return _player.position;
                    
                case 1: // 円錐形（敵から見てプレイヤー方向）
                    Vector3 dirToPlayer = (_player.position - transform.position).normalized;
                    return transform.position + dirToPlayer * attackInfo.attackRange * 0.5f;
                    
                case 2: // 直線（敵から見てプレイヤー方向の直線）
                    Vector3 direction = (_player.position - transform.position).normalized;
                    return transform.position + direction * attackInfo.attackRange * 0.5f;
                    
                default:
                    return _player.position;
            }
        }
        
        /// <summary>
        /// ダメージ判定
        /// </summary>
        private void PerformDamageCheck(Vector3 attackPosition, AttackInfo attackInfo)
        {
            if (_player == null || _playerHealth == null) return;
            
            bool hitPlayer = false;
            
            // 攻撃形状に応じた判定
            switch (attackInfo.attackShape)
            {
                case 0: // 球形
                    float distance = Vector3.Distance(_player.position, attackPosition);
                    hitPlayer = distance <= attackInfo.attackRange;
                    break;
                    
                case 1: // 円錐形
                    Vector3 dirFromEnemy = (_player.position - transform.position).normalized;
                    Vector3 attackDir = (attackPosition - transform.position).normalized;
                    float angleToPlayer = Vector3.Angle(dirFromEnemy, attackDir);
                    float distanceToPlayer = Vector3.Distance(_player.position, transform.position);
                    
                    // 円錐の角度は攻撃範囲に比例
                    float coneAngle = 30f; // 基本角度
                    hitPlayer = angleToPlayer <= coneAngle && distanceToPlayer <= attackInfo.attackRange;
                    break;
                    
                case 2: // 直線
                    Vector3 lineStart = transform.position;
                    Vector3 lineEnd = attackPosition + (attackPosition - transform.position).normalized * attackInfo.attackRange;
                    
                    // 直線とプレイヤーの最短距離
                    Vector3 lineDirection = (lineEnd - lineStart).normalized;
                    Vector3 playerToLineStart = _player.position - lineStart;
                    float dotProduct = Vector3.Dot(playerToLineStart, lineDirection);
                    
                    // プレイヤーが線分の範囲内にいるか
                    if (dotProduct >= 0 && dotProduct <= Vector3.Distance(lineStart, lineEnd))
                    {
                        // 最短距離を計算
                        Vector3 closestPoint = lineStart + lineDirection * dotProduct;
                        float shortestDistance = Vector3.Distance(_player.position, closestPoint);
                        
                        // 攻撃範囲の半分の距離以内ならヒット
                        hitPlayer = shortestDistance <= (attackInfo.attackRange * 0.5f);
                    }
                    break;
            }
            
            // プレイヤーにダメージ
            if (hitPlayer)
            {
                _playerHealth.TakeDamage(attackInfo.baseDamage);
            }
        }
    }
}