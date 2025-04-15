using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 音楽再生を操作するためのメソッド
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioController : MonoBehaviour
{
    public static AudioController Instance;
    
    [SerializeField, Comment("再生開始秒数")] private float _startTime;
    [SerializeField] private AudioSource _seSource;
    private AudioSource _audioSource;
    private float _defaultVolume;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        _defaultVolume = _audioSource.volume; // 初期音量を保存
        
        _audioSource.volume = 0;
        _audioSource.time = _startTime;
        _audioSource.Play();

        _audioSource.DOFade(_defaultVolume, 0.5f); // フェードイン
    }

    public void PlaySE(AudioClip clip)
    {
        _seSource.PlayOneShot(clip);
    }
}