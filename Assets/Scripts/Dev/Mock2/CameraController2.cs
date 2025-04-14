using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UniRx;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CameraController2 : MonoBehaviour
{
    [Header("カメラ設定")]
    [SerializeField] private List<GameObject> _cameras;
    [SerializeField] private float _transitionDuration = 0.8f;
    [SerializeField] private Ease _transitionEase = Ease.InOutCubic;
    
    [Header("カメラエフェクト")]
    [SerializeField] private Volume _postProcessVolume; // URP Volume
    [SerializeField] private bool _useCameraShake = true;
    [SerializeField] private float _shakeIntensity = 0.5f;
    [SerializeField] private float _shakeDuration = 0.3f;
    
    // ポストプロセスエフェクト参照
    private ChromaticAberration _chromaticAberration;
    private Vignette _vignette;
    private DepthOfField _depthOfField;
    private ColorAdjustments _colorAdjustments;
    
    // 現在のカメラインデックス
    private int _currentCameraIndex = 0;
    private PlayerController _playerController;
    private IDisposable _playerTurnDisposable;
    
    // カメラトランジション中フラグ
    private bool _isTransitioning = false;
    
    // カメラ位置の揺れ制御用
    private Vector3[] _originalCameraPositions;
    private Quaternion[] _originalCameraRotations;
    
    private void Awake()
    {
        // プレイヤーコントローラーを取得
        _playerController = FindObjectOfType<PlayerController>();
        
        // 各カメラの初期位置と回転を保存
        InitializeCameras();
        
        // ポストプロセスボリュームの初期化
        InitializePostProcessing();
    }
    
    private void Start()
    {
        // 最初のカメラをアクティブに
        ActiveCamera(0);
        
        // プレイヤーターンの変更を監視
        if (_playerController != null)
        {
            _playerTurnDisposable = _playerController.IsEnemyTurn.Subscribe(isEnemyTurn =>
            {
                // ターン切り替え時にカメラエフェクトを適用
                OnTurnChanged(isEnemyTurn).Forget();
            });
        }
    }
    
    /// <summary>
    /// カメラの初期設定
    /// </summary>
    private void InitializeCameras()
    {
        // 各カメラの初期位置と回転を保存
        _originalCameraPositions = new Vector3[_cameras.Count];
        _originalCameraRotations = new Quaternion[_cameras.Count];
        
        for (int i = 0; i < _cameras.Count; i++)
        {
            if (_cameras[i] != null)
            {
                _originalCameraPositions[i] = _cameras[i].transform.position;
                _originalCameraRotations[i] = _cameras[i].transform.rotation;
                _cameras[i].SetActive(false); // 全カメラを一旦非アクティブに
            }
        }
    }
    
    /// <summary>
    /// ポストプロセッシングの初期化
    /// </summary>
    private void InitializePostProcessing()
    {
        if (_postProcessVolume != null)
        {
            // 各エフェクトを取得
            _postProcessVolume.profile.TryGet(out _chromaticAberration);
            _postProcessVolume.profile.TryGet(out _vignette);
            _postProcessVolume.profile.TryGet(out _depthOfField);
            _postProcessVolume.profile.TryGet(out _colorAdjustments);
            
            // 初期値を設定
            if (_chromaticAberration != null) _chromaticAberration.intensity.value = 0f;
            if (_vignette != null) _vignette.intensity.value = 0.2f;
            if (_depthOfField != null) _depthOfField.active = false;
        }
    }
    
    /// <summary>
    /// ターン切り替え時のエフェクト
    /// </summary>
    private async UniTask OnTurnChanged(bool isEnemyTurn)
    {
        // カメラ切り替え
        if (isEnemyTurn)
        {
            // 敵のターンではサイドビューカメラに切り替え
            ChangeCamera(UseCamera.SideView);
        }
        else
        {
            // プレイヤーのターンでは三人称視点カメラに切り替え
            ChangeCamera(UseCamera.ThirdPerson);
        }
        
        // カメラシェイク
        if (_useCameraShake)
        {
            ShakeCamera(_shakeDuration, _shakeIntensity).Forget();
        }
        
        // ポストプロセスエフェクト
        if (_chromaticAberration != null)
        {
            // 色収差エフェクト
            DOTween.To(() => _chromaticAberration.intensity.value, 
                x => _chromaticAberration.intensity.value = x, 
                0.8f, 0.3f).SetEase(Ease.OutQuad);
            
            await UniTask.Delay(300);
            
            DOTween.To(() => _chromaticAberration.intensity.value, 
                x => _chromaticAberration.intensity.value = x, 
                isEnemyTurn ? 0.3f : 0f, 0.8f).SetEase(Ease.InOutQuad);
        }
        
        // ビネットエフェクト
        if (_vignette != null)
        {
            DOTween.To(() => _vignette.intensity.value, 
                x => _vignette.intensity.value = x, 
                isEnemyTurn ? 0.4f : 0.2f, 0.8f).SetEase(Ease.InOutQuad);
        }
        
        // 深度エフェクト（敵ターン時に背景をぼかす）
        if (_depthOfField != null)
        {
            _depthOfField.active = isEnemyTurn;
            if (isEnemyTurn)
            {
                DOTween.To(() => _depthOfField.focusDistance.value, 
                    x => _depthOfField.focusDistance.value = x, 
                    10f, 0.5f);
            }
        }
    }
    
    /// <summary>
    /// カメラを変更する
    /// </summary>
    public void ChangeCamera(UseCamera cameraEnum)
    {
        // すでにトランジション中なら無視
        if (_isTransitioning) return;
        
        int targetIndex;
        
        if (cameraEnum == UseCamera.SideView)
        {
            targetIndex = 0;
        }
        else if (cameraEnum == UseCamera.ThirdPerson)
        {
            targetIndex = 1;
        }
        else // FirstPerson
        {
            targetIndex = 2;
        }
        
        // 同じカメラなら何もしない
        if (targetIndex == _currentCameraIndex) return;
        
        TransitionToCamera(targetIndex).Forget();
    }
    
    /// <summary>
    /// スムーズなカメラトランジションを行う
    /// </summary>
    private async UniTask TransitionToCamera(int targetIndex)
    {
        _isTransitioning = true;
        
        // 現在のアクティブカメラのカメラコンポーネントを取得
        Camera currentCamera = _cameras[_currentCameraIndex].GetComponentInChildren<Camera>();
        Camera targetCamera = _cameras[targetIndex].GetComponentInChildren<Camera>();
        
        if (currentCamera != null && targetCamera != null)
        {
            // 一時的なレンダーテクスチャを作成
            RenderTexture renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
            currentCamera.targetTexture = renderTexture;
            
            // 現在のカメラでレンダリング
            currentCamera.Render();
            currentCamera.targetTexture = null;
            
            // トランジション用のカメラ切り替え
            _cameras[_currentCameraIndex].SetActive(false);
            _cameras[targetIndex].SetActive(true);
            
            // トランジションエフェクトの実行（フェード、ワイプなど）
            // 例：フェードエフェクト
            Material transitionMaterial = new Material(Shader.Find("Unlit/Transparent"));
            transitionMaterial.mainTexture = renderTexture;
            
            GameObject transitionPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            transitionPlane.transform.position = targetCamera.transform.position + targetCamera.transform.forward;
            transitionPlane.transform.rotation = targetCamera.transform.rotation;
            transitionPlane.transform.localScale = new Vector3(1.9f, 1f, 1f);
            transitionPlane.GetComponent<Renderer>().material = transitionMaterial;
            
            // トランジションプレーンをフェードアウト
            transitionPlane.GetComponent<Renderer>().material.DOFade(0, _transitionDuration)
                .SetEase(_transitionEase)
                .OnComplete(() => {
                    Destroy(transitionPlane);
                    Destroy(renderTexture);
                });
            
            // トランジション中に目標カメラの動きを演出
            targetCamera.transform.DOLocalMoveY(0.2f, _transitionDuration * 0.5f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => {
                    targetCamera.transform.DOLocalMoveY(0f, _transitionDuration * 0.5f)
                        .SetEase(Ease.InOutQuad);
                });
        }
        else
        {
            // 単純なアクティブ切り替え
            _cameras[_currentCameraIndex].SetActive(false);
            _cameras[targetIndex].SetActive(true);
        }
        
        // インデックスを更新
        _currentCameraIndex = targetIndex;
        
        // トランジション完了まで待機
        await UniTask.Delay((int)(_transitionDuration * 1000));
        _isTransitioning = false;
    }
    
    /// <summary>
    /// カメラのアクティブ状態を切り替える（シンプルバージョン）
    /// </summary>
    private void ActiveCamera(int index)
    {
        for (int i = 0; i < _cameras.Count; i++)
        {
            _cameras[i].SetActive(index == i);
        }
        _currentCameraIndex = index;
    }
    
    /// <summary>
    /// カメラシェイクエフェクト
    /// </summary>
    public async UniTask ShakeCamera(float duration, float intensity)
    {
        if (!_useCameraShake) return;
        
        Camera currentCamera = _cameras[_currentCameraIndex].GetComponentInChildren<Camera>();
        if (currentCamera == null) return;
        
        Vector3 originalPosition = currentCamera.transform.localPosition;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            // ランダムな揺れを計算
            float x = UnityEngine.Random.Range(-1f, 1f) * intensity;
            float y = UnityEngine.Random.Range(-1f, 1f) * intensity;
            
            // カメラ位置を揺らす
            currentCamera.transform.localPosition = new Vector3(
                originalPosition.x + x,
                originalPosition.y + y,
                originalPosition.z
            );
            
            elapsed += Time.deltaTime;
            await UniTask.Yield();
        }
        
        // 元の位置に戻す
        currentCamera.transform.localPosition = originalPosition;
    }
    
    /// <summary>
    /// FOVエフェクト
    /// </summary>
    public async UniTask ChangeFOV(float targetFOV, float duration)
    {
        Camera currentCamera = _cameras[_currentCameraIndex].GetComponentInChildren<Camera>();
        if (currentCamera == null) return;
        
        float originalFOV = currentCamera.fieldOfView;
        
        // FOVを変更
        DOTween.To(() => currentCamera.fieldOfView, 
            x => currentCamera.fieldOfView = x, 
            targetFOV, duration).SetEase(Ease.OutQuad);
        
        await UniTask.Delay((int)(duration * 1000));
        
        // 元のFOVに戻す（徐々に）
        DOTween.To(() => currentCamera.fieldOfView, 
            x => currentCamera.fieldOfView = x, 
            originalFOV, duration).SetEase(Ease.InOutQuad);
    }
    
    /// <summary>
    /// 追跡カメラエフェクト
    /// </summary>
    public void SetCameraTargetOffset(Vector3 offset, float duration)
    {
        Camera currentCamera = _cameras[_currentCameraIndex].GetComponentInChildren<Camera>();
        if (currentCamera == null) return;
        
        // カメラのオフセットを徐々に変更
        DOTween.To(() => currentCamera.transform.localPosition, 
            x => currentCamera.transform.localPosition = x, 
            offset, duration).SetEase(Ease.InOutQuad);
    }
    
    private void OnDestroy()
    {
        _playerTurnDisposable?.Dispose();
        DOTween.Kill(this);
    }
}