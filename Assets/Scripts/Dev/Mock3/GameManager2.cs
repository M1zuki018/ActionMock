using UniRx;
using UnityEngine;

namespace Mock3
{
    /// <summary>
    /// モック3のGameManager
    /// </summary>
    public class GameManager2 : MonoBehaviour
    {
        public ReactiveProperty<int> Score = new ReactiveProperty<int>(0);
        public ReactiveProperty<int> ComboCount = new ReactiveProperty<int>(0);

        private void Awake()
        {
            Score.Value = 0;
            ComboCount.Value = 0;
        }
    }
}