using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// プレイヤーが敵をロックオンして弾を撃つシステム
/// </summary>
public class LockOn : MonoBehaviour
{
    [SerializeField] private GameManager2 _gameManager2;
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private Transform _lockOnIcon;
    [SerializeField] private GameObject _bulletPrefab;
    [SerializeField] private Transform _mazzle;
    [SerializeField] private AudioClip _shotSount;
    [SerializeField] private AudioSource _seSource;
    private List<Transform> _allTargets = new List<Transform>();
    private Transform _currentTarget; // 現在のターゲット
    private int _targetIndex = 0;
    private Camera _camera;

    private void Start()
    {
        _camera = Camera.main;
        
        SetTarget();
    }

    private void SetTarget()
    {
        // すべての Target タグを持つオブジェクトを取得し、距離でソートして _allTargets に格納
        Vector3 playerPosition = transform.position;
    
        _allTargets = GameObject.FindGameObjectsWithTag("Target")
            .Select(go => go.transform)
            .OrderBy(t => Vector3.Distance(t.position, playerPosition))
            .ToList();
        
        _currentTarget = _allTargets[_targetIndex];
    }

    private void Update()
    {
        Vector3 screenPosition = _camera.WorldToScreenPoint(_currentTarget.position);
        _lockOnIcon.transform.position = screenPosition; // ターゲットアイコンを現在のターゲットに合わせる
        
        if (Input.GetMouseButtonDown(0) && !_playerController.IsEnemyTurn.Value)
        {
            _seSource.PlayOneShot(_shotSount); // 弾を撃つSE再生
            TargetBroken();
        }
    }

    /// <summary>
    /// ターゲット破壊
    /// </summary>
    private void TargetBroken()
    {
        // 銃弾を生成
        var bullet = Instantiate(_bulletPrefab, _mazzle.position, Quaternion.identity);
        var component = bullet.GetComponent<Bullet>();
        component.Setup(this, _currentTarget);
    }
    
    /// <summary>
    /// 次のターゲットを探す
    /// </summary>
    public void SearchTarget()
    {
        _gameManager2.Score.Value += 100;
        
        return;
        
        // 一次的にコメントアウト中
        _targetIndex++;
        if (_targetIndex < _allTargets.Count)
        {
            _currentTarget = _allTargets[_targetIndex];
        }
        else
        {
            _lockOnIcon.gameObject.SetActive(false); // ターゲットがいなくなったらアイコンを非表示にする
        }
    }
}
