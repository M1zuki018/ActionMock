using UnityEngine;

/// <summary>
/// 敵を生成する
/// </summary>
public class EnemyGenerator : MonoBehaviour
{
    [SerializeField] private GameObject _enemyPrefab;

    public void Generate(Transform basePoint, int count)
    {
        for (int i = 0; i < count; i++)
        {
            // basePoint.xからマイナス方向に15までの範囲でランダム
            float randomX = Random.Range(basePoint.position.x - 45f, basePoint.position.x);
        
            // basePointの上下10の範囲でランダム
            float randomY = Random.Range(basePoint.position.y - 30f, basePoint.position.y + 30f);
            float randomZ = Random.Range(basePoint.position.z - 30f, basePoint.position.z + 30f);
        
            // ランダムな位置を設定
            var position = new Vector3(randomX, randomY, randomZ);
            
            var enemyObj = Instantiate(_enemyPrefab);
            enemyObj.transform.position = position;
        }
    }
}
