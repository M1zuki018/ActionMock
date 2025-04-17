using UnityEngine;

namespace Mock3
{
public class PlayerAttackController : MonoBehaviour
{
    [SerializeField] private Transform _attackOrigin;
    [SerializeField] private float _attackRange = 2.0f;
    [SerializeField] private LayerMask _enemyLayer;
    
    [Header("攻撃設定")]
    [SerializeField] private int _normalAttackDamage = 10;
    [SerializeField] private int _rhythmAttackDamage = 25;
    [SerializeField] private int _counterAttackDamage = 50;
    [SerializeField] private int _specialAttackDamage = 100;
    
    [Header("タイミング報酬")]
    [SerializeField] private float _perfectTimingWindow = 0.1f; // パーフェクトタイミングの許容範囲（秒）
    [SerializeField] private float _goodTimingWindow = 0.2f;    // グッドタイミングの許容範囲（秒）
    [SerializeField] private float _perfectDamageMultiplier = 2.0f;
    [SerializeField] private float _goodDamageMultiplier = 1.5f;
    
    [Header("コンボ設定")]
    [SerializeField] private float _comboResetTime = 1.5f;      // コンボがリセットされる時間
    [SerializeField] private float _maxComboMultiplier = 2.0f;  // 最大コンボ時の倍率
    [SerializeField] private int _maxComboCount = 10;           // 最大コンボ数
    
    [Header("エフェクト&サウンド")]
    [SerializeField] private AudioClip[] _attackSounds;
    [SerializeField] private AudioClip _perfectSound;
    [SerializeField] private AudioClip _goodSound;
    
    // コンボ関連変数
    private int _comboCount = 0;
    private float _lastAttackTime = -100f;
    
    // リズム情報の参照
    private RhythmManager _rhythmManager;
    
    // 攻撃のクールダウン
    private float _attackCooldown = 0.2f;
    private float _lastNormalAttackTime = -100f;
    
    // 回避とカウンター状態
    private bool _isInJustDodge = false;
    private float _counterWindowTime = 0.5f;
    private float _justDodgeTime = -100f;
    
    // 入力ボタン
    private string _attackButton = "Fire1";
    private string _rhythmAttackButton = "Fire2";
    private string _dodgeButton = "Jump";
    private string _specialButton = "Fire3";
    
    // 特殊技用のアイテム所持数
    private int _specialItemCount = 0;
    
    private void Start()
    {
        _rhythmManager = FindObjectOfType<RhythmManager>();
        if (_rhythmManager == null)
        {
            Debug.LogError("RhythmManagerが見つかりません");
        }
    }
    
    private void Update()
    {
        // コンボのリセットチェック
        if (Time.time - _lastAttackTime > _comboResetTime && _comboCount > 0)
        {
            _comboCount = 0;
        }
        
        // カウンターウィンドウの更新
        _isInJustDodge = (Time.time - _justDodgeTime < _counterWindowTime);
        
        // 入力処理
        HandleAttackInput();
        HandleDodgeInput();
        HandleSpecialInput();
    }
    
    private void HandleAttackInput()
    {
        // 通常攻撃
        if (Input.GetButtonDown(_attackButton) && Time.time - _lastNormalAttackTime > _attackCooldown)
        {
            _lastNormalAttackTime = Time.time;
            
            if (_isInJustDodge)
            {
                PerformCounterAttack();
            }
            else
            {
                PerformNormalAttack();
            }
        }
        
        // リズム攻撃
        if (Input.GetButtonDown(_rhythmAttackButton))
        {
            PerformRhythmAttack();
        }
    }
    
    private void HandleDodgeInput()
    {
        if (Input.GetButtonDown(_dodgeButton))
        {
            PerformDodge();
        }
    }
    
    private void HandleSpecialInput()
    {
        if (Input.GetButtonDown(_specialButton) && _specialItemCount > 0)
        {
            PerformSpecialAttack();
            _specialItemCount--;
        }
    }
    
    private void PerformNormalAttack()
    {
        // コンボ更新
        _comboCount = Mathf.Min(_comboCount + 1, _maxComboCount);
        _lastAttackTime = Time.time;
        
        // コンボ倍率計算（1.0～maxComboMultiplier）
        float comboMultiplier = 1.0f + (_maxComboMultiplier - 1.0f) * ((float)_comboCount / _maxComboCount);
        
        // ダメージ計算
        int damage = Mathf.RoundToInt(_normalAttackDamage * comboMultiplier);
        
        // 攻撃実行
        ExecuteAttack(damage, 0);
    }
    
    private void PerformRhythmAttack()
    {
        // リズムのタイミング評価
        float timingAccuracy = _rhythmManager.GetTimingAccuracy();
        
        // タイミング評価に基づく倍率
        float timingMultiplier = 1.0f;
        string timingResult = "Miss";
        
        if (timingAccuracy <= _perfectTimingWindow)
        {
            timingMultiplier = _perfectDamageMultiplier;
            timingResult = "Perfect";
            
            if (AudioController.Instance != null)
            {
                AudioController.Instance.PlaySE(_perfectSound);
            }
        }
        else if (timingAccuracy <= _goodTimingWindow)
        {
            timingMultiplier = _goodDamageMultiplier;
            timingResult = "Good";
            
            if (AudioController.Instance != null)
            {
                AudioController.Instance.PlaySE(_goodSound);
            }
        }
        
        Debug.Log($"リズム攻撃: {timingResult} (誤差: {timingAccuracy:F3}秒)");
        
        // コンボ更新
        if (timingResult != "Miss")
        {
            _comboCount = Mathf.Min(_comboCount + 1, _maxComboCount);
            _lastAttackTime = Time.time;
        }
        
        // コンボ倍率計算
        float comboMultiplier = 1.0f + (_maxComboMultiplier - 1.0f) * ((float)_comboCount / _maxComboCount);
        
        // ダメージ計算
        int damage = Mathf.RoundToInt(_rhythmAttackDamage * timingMultiplier * comboMultiplier);
        
        // 攻撃実行
        ExecuteAttack(damage, 1);
    }
    
    private void PerformCounterAttack()
    {
        // カウンター攻撃は常に最大コンボ扱い
        float comboMultiplier = _maxComboMultiplier;
        
        // ダメージ計算
        int damage = Mathf.RoundToInt(_counterAttackDamage * comboMultiplier);
        
        // カウンター状態解除
        _isInJustDodge = false;
        _justDodgeTime = -100f;
        
        // 攻撃実行
        ExecuteAttack(damage, 2);
    }
    
    private void PerformSpecialAttack()
    {
        // 特殊攻撃はコンボやタイミングの影響を受けない
        ExecuteAttack(_specialAttackDamage, 3);
    }
    
    private void PerformDodge()
    {
        // ジャスト回避判定（敵の攻撃タイミングとの差で判定）
        bool isJustDodge = _rhythmManager.IsInEnemyAttackTiming(_goodTimingWindow);
        
        if (isJustDodge)
        {
            _justDodgeTime = Time.time;
            Debug.Log("ジャスト回避成功！カウンターチャンス");
            
            // エフェクトや演出
        }
        
        // 回避アニメーション再生など
    }
    
    private void ExecuteAttack(int damage, int attackTypeIndex)
    {
        // 攻撃判定用のレイキャスト
        RaycastHit[] hits = Physics.SphereCastAll(
            _attackOrigin.position,
            _attackRange,
            _attackOrigin.forward,
            _attackRange,
            _enemyLayer
        );
        
        bool hitEnemy = false;
        
        foreach (var hit in hits)
        {
            var enemyHealth = hit.collider.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                bool killed = enemyHealth.TakeDamage(damage, hit.point);
                hitEnemy = true;
                
                if (killed)
                {
                    // 敵を倒した時の処理
                    Debug.Log("敵を倒した！");
                }
            }
        }
        
        // サウンド再生
        if (attackTypeIndex < _attackSounds.Length && _attackSounds[attackTypeIndex] != null && AudioController.Instance != null)
        {
            AudioController.Instance.PlaySE(_attackSounds[attackTypeIndex]);
        }
    }
    
    // アイテム取得
    public void AddSpecialItem()
    {
        _specialItemCount++;
    }
}
}