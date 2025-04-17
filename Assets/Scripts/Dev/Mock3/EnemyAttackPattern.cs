using UnityEngine;

[System.Serializable]
public class EnemyAttackPattern
{
    [Tooltip("攻撃名称")] 
    public string attackName;
    
    [Tooltip("攻撃の基本ダメージ")] 
    public int baseDamage = 10;
    
    [Tooltip("攻撃の予備動作時間（秒）")] 
    public float telegraphTime = 0.5f;
    
    [Tooltip("4小節の譜面（16分音符単位）")]
    [Range(0, 1)] public float[] rhythmPattern = new float[64]; // 16分音符×4小節=64
    
    [Tooltip("攻撃SE")] 
    public AudioClip attackSound;
}