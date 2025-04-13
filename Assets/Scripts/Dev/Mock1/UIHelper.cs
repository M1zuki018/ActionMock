using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UIHelper : MonoBehaviour
{
    [SerializeField] private GameObject _texts;
    [SerializeField] private Image _redScreen;

    private void Start()
    {
        HideText();
        _redScreen.color = new Color(255, 0,0,0);
    }
    
    public void ShowText() => _texts.SetActive(true);
    public void HideText() => _texts.SetActive(false);

    /// <summary>
    /// 赤いスクリーンを一瞬出す
    /// </summary>
    public void RedFlash()
    {
        _redScreen.DOFade(0.6f, 0.1f).OnComplete(() => _redScreen.DOFade(0f, 0.3f));
    }
}
