using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private List<GameObject> _cameras;
    [SerializeField] private Jump _jump;
    private IDisposable _subscription;
    
    private void Start()
    {
        _subscription = _jump.UseProp.Subscribe(ChangeCamera);
    }

    /// <summary>
    /// カメラを変更する
    /// </summary>
    private void ChangeCamera(UseProp propEnum)
    {
        if (propEnum == UseProp.Ground)
        {
            ActiveCamera(0);
        }
        else if (propEnum == UseProp.Sky)
        {
            ActiveCamera(1);
        }
    }

    /// <summary>
    /// カメラリストのオブジェクトのActive 非Activeを切り替える
    /// </summary>
    private void ActiveCamera(int index)
    {
        for (int i = 0; i < _cameras.Count; i++)
        {
            _cameras[i].SetActive(index == i);
        }
    }

    private void OnDestroy()
    {
        _subscription?.Dispose();
    }
}
