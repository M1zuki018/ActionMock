using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

public class CameraController2 : MonoBehaviour
{
    [SerializeField] private List<GameObject> _cameras;

    private void Start()
    {
        ActiveCamera(0);
    }
    
    /// <summary>
    /// カメラを変更する
    /// </summary>
    private void ChangeCamera(UseCamera cameraEnum)
    {
        if (cameraEnum == UseCamera.SideView)
        {
            ActiveCamera(0);
        }
        else if (cameraEnum == UseCamera.ThirdPerson)
        {
            ActiveCamera(1);
        }
        else
        {
            ActiveCamera(2);
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
}