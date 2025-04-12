using UnityEngine;
using UniVRM10;

/// <summary>
/// NPCを操作するためのクラス
/// </summary>
public class NPCController : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer _skin;
    [SerializeField] private Animator _animator;
    [SerializeField] private Jump _jump;
    
    
    private void Start()
    {
        _jump.OnPerformance += HandlePerformance;
        
    }

    private void HandlePerformance()
    {
        _animator.SetTrigger("Clap");
        _skin.SetBlendShapeWeight(0, 100.0f);
        _skin.SetBlendShapeWeight(3, 90.0f);
        Debug.Log("BlendShape weight: " + _skin.GetBlendShapeWeight(0));
    }

    private void OnDestroy()
    {
        _jump.OnPerformance -= HandlePerformance;
    }
}