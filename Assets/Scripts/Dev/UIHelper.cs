using UnityEngine;

public class UIHelper : MonoBehaviour
{
    [SerializeField] private GameObject _texts;

    public void ShowText() => _texts.SetActive(true);
    public void HideText() => _texts.SetActive(false);
}
