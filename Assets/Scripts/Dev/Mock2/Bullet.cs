using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class Bullet : MonoBehaviour
{
    [SerializeField] private float _speed = 20;
    private LockOn _lockOn;
    private Transform _target;

    public void Setup(LockOn lockOn, Transform target)
    {
        _lockOn = lockOn;
        _target = target;
        
        transform.LookAt(_target.position);
    }

    private void Update()
    {
        if (_target != null)
        {
            Vector3 direction = (_target.position - transform.position).normalized;
            transform.position += direction * _speed * Time.deltaTime;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Target") // ターゲットに衝突したら
        {
            _lockOn.SearchTarget(); // ターゲット更新
            Destroy(gameObject); // 自分を消す
        }
    }
}
